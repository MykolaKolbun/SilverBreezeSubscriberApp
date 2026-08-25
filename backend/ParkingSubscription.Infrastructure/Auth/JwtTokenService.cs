using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Domain.Entities;

namespace ParkingSubscription.Infrastructure.Auth;

/// <summary>JWT access + opaque refresh token issuing (ТЗ §3).</summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public AccessTokens Issue(AppAccount account)
    {
        var expires = clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("uid", account.UserId.ToString()),
        };
        if (!string.IsNullOrEmpty(account.Email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, account.Email));
        if (!string.IsNullOrEmpty(account.Phone))
            claims.Add(new Claim("phone", account.Phone));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: clock.UtcNow.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokens(accessToken, CreateRefreshToken(), expires);
    }

    public string CreateRefreshToken() => TokenGenerator.NewToken(48);

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
