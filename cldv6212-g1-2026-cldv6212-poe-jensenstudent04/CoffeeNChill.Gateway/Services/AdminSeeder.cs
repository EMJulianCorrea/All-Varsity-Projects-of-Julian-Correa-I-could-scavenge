namespace CoffeeNChill.Gateway.Services;

/// <summary>
/// Public registration only ever creates Customers, so the very first Admin has to come from
/// configuration (SeedAdmin__Email / SeedAdmin__Password). That Admin can then create Staff and
/// further Admins through POST /api/auth/register-staff.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var email = config["SeedAdmin:Email"];
        var password = config["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SeedAdmin:Email / SeedAdmin:Password not set - no Admin account will be seeded.");
            return;
        }

        var store = services.GetRequiredService<UserStore>();
        var created = await store.CreateAsync(Roles.Admin, email, "System Admin", password);
        logger.LogInformation(created is null
            ? "Seed admin {Email} already exists."
            : "Seed admin {Email} created.", UserStore.NormaliseEmail(email));
    }
}
