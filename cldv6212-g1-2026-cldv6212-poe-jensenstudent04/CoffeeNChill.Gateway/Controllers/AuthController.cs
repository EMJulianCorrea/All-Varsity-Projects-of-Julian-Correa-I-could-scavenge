using System.Security.Claims;
using CoffeeNChill.Gateway.Dtos;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>Registration, login and identity. Users live in the Azure Table "Users".</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(UserStore users, JwtTokenService tokens) : ControllerBase
{
    /// <summary>Create a Customer account.</summary>
    /// <remarks>Public registration never grants Staff or Admin; those are created by an Admin via register-staff.</remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed (bad email, password shorter than 12 characters, ...).</response>
    /// <response code="409">This email is already registered.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await users.CreateAsync(Roles.Customer, request.Email, request.FullName, request.Password, cancellationToken);
        if (user is null)
            return Problem(detail: "This email is already registered.", statusCode: StatusCodes.Status409Conflict);

        return StatusCode(StatusCodes.Status201Created, new UserResponse(user.RowKey, user.FullName, user.PartitionKey));
    }

    /// <summary>Create a Staff or Admin account (Admin only).</summary>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is not an Admin.</response>
    /// <response code="409">This email is already registered.</response>
    [HttpPost("register-staff")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterStaff(RegisterStaffRequest request, CancellationToken cancellationToken)
    {
        var user = await users.CreateAsync(request.Role, request.Email, request.FullName, request.Password, cancellationToken);
        if (user is null)
            return Problem(detail: "This email is already registered.", statusCode: StatusCodes.Status409Conflict);

        return StatusCode(StatusCodes.Status201Created, new UserResponse(user.RowKey, user.FullName, user.PartitionKey));
    }

    /// <summary>Log in and receive a JWT.</summary>
    /// <remarks>Paste the returned <c>accessToken</c> into Swagger's Authorize button (token only, no "Bearer").</remarks>
    /// <response code="200">Login succeeded.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Unknown email or wrong password (deliberately not distinguished).</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || !users.VerifyPassword(user, request.Password))
            return Unauthorized();

        return Ok(tokens.CreateToken(user));
    }

    /// <summary>Show who the current token belongs to.</summary>
    /// <response code="200">The claims in the caller's token.</response>
    /// <response code="401">Missing or invalid token.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me() => Ok(new CurrentUserResponse(
        User.Identity?.Name,
        User.FindFirst(ClaimTypes.Email)?.Value,
        User.FindFirst(ClaimTypes.Role)?.Value));
}
