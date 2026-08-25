namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// Single-row admin panel configuration. Holds the admin login password HASH so it can
/// be changed from the panel itself (seeded from the Admin:Password config on first use).
/// </summary>
public class AdminConfig
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}
