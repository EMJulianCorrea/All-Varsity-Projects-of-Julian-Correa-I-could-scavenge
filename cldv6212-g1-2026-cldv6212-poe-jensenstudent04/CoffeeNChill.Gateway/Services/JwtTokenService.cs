using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CoffeeNChill.Gateway.Dtos;
using CoffeeNChill.Gateway.Models;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChill.Gateway.Services;

/// <summary>Creates the signed JWT returned by login. Claims: Name, Email and Role.</summary>
public class JwtTokenService(SymmetricSecurityKey signingKey, string issuer, string audience)
{
    public LoginResponse CreateToken(CoffeeNChillUser user)
    {
        // Short-lived: once it expires the user signs in again.
        var expires = DateTime.UtcNow.AddMinutes(30);

        // Claims are readable by anyone holding the token, so never put secrets in them.
        var token = new JwtSecurityToken(issuer, audience,
            claims:
            [
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.RowKey),
                new Claim(ClaimTypes.Role, user.PartitionKey)
            ],
            expires: expires,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.PartitionKey);
    }
}
