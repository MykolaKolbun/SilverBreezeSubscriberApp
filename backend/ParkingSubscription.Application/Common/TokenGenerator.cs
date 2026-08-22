using System.Security.Cryptography;

namespace ParkingSubscription.Application.Common;

/// <summary>Generates URL-safe random tokens for email confirmation / password reset.</summary>
public static class TokenGenerator
{
    public static string NewToken(int bytes = 32)
    {
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(buffer)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
