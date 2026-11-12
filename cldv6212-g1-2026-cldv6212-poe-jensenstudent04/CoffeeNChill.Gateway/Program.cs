using System.Reflection;
using System.Security.Claims;
using System.Text;
using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Gateway;
using CoffeeNChill.Gateway.Middleware;
using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- 1. Configuration --------------------------------------------------------------------
// Nothing secret lives in source control. Locally these come from appsettings.Development.json;
// on the VPS they arrive as environment variables (Storage__ConnectionString, Jwt__Key, ...)
// supplied by docker compose from the .env file.
var storageConnection = builder.Configuration["Storage:ConnectionString"]
    ?? throw new InvalidOperationException("Storage:ConnectionString is not configured (env var Storage__ConnectionString).");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured (env var Jwt__Key).");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");
var functionsBaseUrl = builder.Configuration["Functions:BaseUrl"]
    ?? throw new InvalidOperationException("Functions:BaseUrl is not configured (env var Functions__BaseUrl).");

// Issuer = who made the token, audience = who it is for. Creation and validation must agree.
const string issuer = "CoffeeNChillGateway";
const string audience = "CoffeeNChillClients";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

// ---- 2. Services -------------------------------------------------------------------------
var usersTable = new TableClient(storageConnection, "Users");
builder.Services.AddSingleton(usersTable);
builder.Services.AddSingleton<IPasswordHasher<CoffeeNChillUser>, PasswordHasher<CoffeeNChillUser>>();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton(new JwtTokenService(signingKey, issuer, audience));

// Typed HttpClient: the gateway's single door to the internal Azure Functions app.
builder.Services.AddHttpClient<FunctionsClient>(client =>
{
    client.BaseAddress = new Uri(functionsBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(60); // uploads can take a moment
});

builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep the claim names exactly as JwtTokenService wrote them
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = issuer,
            ValidateAudience = true, ValidAudience = audience,
            ValidateIssuerSigningKey = true, IssuerSigningKey = signingKey,
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role // makes [Authorize(Roles = "...")] work
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CoffeeNChill Gateway",
        Version = "v1",
        Description = "Public API gateway for CoffeeNChill. Handles JWT authentication and role-based access, " +
                      "then forwards to the internal Azure Functions app. Roles: Customer, Staff, Admin."
    });

    // XML doc comments (generated at build time) supply the descriptions and response schemas.
    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFile)) options.IncludeXmlComments(xmlFile);

    // Adds the Authorize button: paste the token only, Swagger adds "Bearer ".
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the accessToken returned by POST /api/auth/login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});

var app = builder.Build();

// ---- 3. Start-up: make sure the Users table exists, then seed the first Admin -----------
// In docker compose the gateway can start a moment before Azurite is ready, so retry briefly.
for (var attempt = 1; ; attempt++)
{
    try
    {
        await usersTable.CreateIfNotExistsAsync();
        break;
    }
    catch (Exception ex) when (ex is RequestFailedException or HttpRequestException or AggregateException && attempt < 10)
    {
        app.Logger.LogWarning("Table Storage not ready (attempt {Attempt}/10): {Message}", attempt, ex.Message);
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
await AdminSeeder.SeedAsync(app.Services, app.Configuration, app.Logger);

// ---- 4. Pipeline (order matters) ---------------------------------------------------------
app.UseMiddleware<GlobalExceptionMiddleware>(); // outermost: catches anything thrown below it

// Swagger stays on in every environment: the VPS demo URL is http://<ip>:5000/swagger
app.UseSwagger();
app.UseSwaggerUI(ui =>
{
    ui.SwaggerEndpoint("/swagger/v1/swagger.json", "CoffeeNChill Gateway v1");
    ui.RoutePrefix = "swagger";
});

app.UseAuthentication(); // who are you?
app.UseAuthorization();  // may you use this endpoint?

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous().ExcludeFromDescription();
app.MapControllers();

app.Run();
