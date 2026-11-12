using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.Dtos;

/// <summary>Body for <c>POST /api/auth/register</c>. Public sign-up always creates a Customer.</summary>
public class RegisterRequest
{
    private string _email = "";

    /// <summary>Email address; it becomes the user's unique key (stored lower-case).</summary>
    /// <example>customer@example.com</example>
    [Required, StringLength(254)]
    [RegularExpression(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$", ErrorMessage = "Email is not valid.")]
    public string Email { get => _email; set => _email = value?.Trim() ?? ""; }

    /// <summary>Password, 12-128 characters. It is hashed before it is stored.</summary>
    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = "";

    /// <summary>Display name, returned in the JWT Name claim.</summary>
    /// <example>Jane Customer</example>
    [Required, StringLength(100)]
    public string FullName { get; set; } = "";
}

/// <summary>Body for <c>POST /api/auth/register-staff</c> (Admin only).</summary>
public class RegisterStaffRequest : RegisterRequest
{
    /// <summary>Role to grant: <c>Staff</c> or <c>Admin</c>.</summary>
    /// <example>Staff</example>
    [Required, RegularExpression("^(Staff|Admin)$", ErrorMessage = "Role must be Staff or Admin.")]
    public string Role { get; set; } = "Staff";
}

/// <summary>Body for <c>POST /api/auth/login</c>.</summary>
public class LoginRequest
{
    private string _email = "";

    /// <summary>The account's email address.</summary>
    [Required, StringLength(254)]
    [RegularExpression(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$", ErrorMessage = "Email is not valid.")]
    public string Email { get => _email; set => _email = value?.Trim() ?? ""; }

    /// <summary>The account's password.</summary>
    [Required, StringLength(128)]
    public string Password { get; set; } = "";
}

/// <summary>A safe view of a user. The password hash is never returned.</summary>
/// <param name="Email">Normalised email address.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Role">Customer, Staff or Admin.</param>
public record UserResponse(string Email, string FullName, string Role);

/// <summary>Returned by login: send <c>AccessToken</c> as <c>Authorization: Bearer ...</c> on later calls.</summary>
/// <param name="AccessToken">The signed JWT.</param>
/// <param name="ExpiresAtUtc">When the token stops working (UTC).</param>
/// <param name="Role">The role carried by the token.</param>
public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string Role);

/// <summary>The identity read from the caller's token claims.</summary>
/// <param name="FullName">Name claim.</param>
/// <param name="Email">Email claim.</param>
/// <param name="Role">Role claim.</param>
public record CurrentUserResponse(string? FullName, string? Email, string? Role);
