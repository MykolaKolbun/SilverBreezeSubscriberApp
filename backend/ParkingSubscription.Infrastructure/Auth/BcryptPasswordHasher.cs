using ParkingSubscription.Application.Abstractions;

namespace ParkingSubscription.Infrastructure.Auth;

/// <summary>BCrypt password hashing per ТЗ §9 (bcrypt/argon2).</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
