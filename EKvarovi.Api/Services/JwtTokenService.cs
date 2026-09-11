using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EKvarovi.Api.Entities;
using EKvarovi.Api.Infrastructure.Options;
using EKvarovi.Api.Services.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EKvarovi.Api.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly SymmetricSecurityKey _key = new(Encoding.UTF8.GetBytes(options.Value.Key));

    public string GenerateToken(User user, IReadOnlyList<string> roleCodes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", $"{user.FirstName} {user.LastName}")
        };

        if (user.HomeLocationId is int locationId)
            claims.Add(new Claim("homeLocationId", locationId.ToString()));

        claims.AddRange(roleCodes.Select(code => new Claim(ClaimTypes.Role, code)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_options.ExpiryHours),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
