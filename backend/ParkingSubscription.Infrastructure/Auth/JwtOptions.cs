namespace ParkingSubscription.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ParkingSubscription";
    public string Audience { get; set; } = "ParkingSubscription";

    /// <summary>Symmetric signing key. MUST be overridden via configuration/secret in production.</summary>
    public string SigningKey { get; set; } = "dev-only-signing-key-change-me-please-32bytes-min";

    public int AccessTokenMinutes { get; set; } = 30;
}
