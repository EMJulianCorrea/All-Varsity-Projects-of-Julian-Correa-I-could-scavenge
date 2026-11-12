using Azure;
using Azure.Data.Tables;
using CoffeeNChill.Gateway.Models;
using Microsoft.AspNetCore.Identity;

namespace CoffeeNChill.Gateway.Services;

/// <summary>
/// All access to the Users table lives here so controllers stay thin.
/// Passwords are hashed with ASP.NET's PasswordHasher (PBKDF2 with a random salt per user).
/// </summary>
public class UserStore(TableClient users, IPasswordHasher<CoffeeNChillUser> hasher)
{
    public static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Finds a user by email. The role is the PartitionKey and the caller does not know it at
    /// login, so each of the three role partitions is checked (point lookups, so this is cheap).
    /// </summary>
    public async Task<CoffeeNChillUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var key = NormaliseEmail(email);
        foreach (var role in Roles.All)
        {
            var found = await users.GetEntityIfExistsAsync<CoffeeNChillUser>(
                role, key, cancellationToken: cancellationToken);
            if (found.HasValue) return found.Value;
        }
        return null;
    }

    /// <summary>Creates a user in the given role. Returns null when the email is already registered.</summary>
    public async Task<CoffeeNChillUser?> CreateAsync(
        string role, string email, string fullName, string password, CancellationToken cancellationToken = default)
    {
        // One email = one account, whatever its role.
        if (await FindByEmailAsync(email, cancellationToken) is not null) return null;

        var user = new CoffeeNChillUser
        {
            PartitionKey = role,
            RowKey = NormaliseEmail(email),
            FullName = fullName.Trim(),
            CreatedDate = DateTimeOffset.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, password);

        try
        {
            // Atomic insert: two simultaneous registrations cannot both succeed.
            await users.AddEntityAsync(user, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            return null;
        }
        return user;
    }

    public bool VerifyPassword(CoffeeNChillUser user, string password) =>
        hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
