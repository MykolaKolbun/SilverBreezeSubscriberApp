namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// One-time passwordless login code (ТЗ §3). Keyed by the login identifier
/// (normalized email), one active code per identifier (replaced on each request).
/// The code is stored HASHED; only its hash is kept.
/// </summary>
public class LoginOtp
{
    /// <summary>Login identifier — normalized email (primary key).</summary>
    public string Identifier { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Failed verification attempts against the current code.</summary>
    public int Attempts { get; set; }

    /// <summary>When the current code was last sent (for resend rate-limiting).</summary>
    public DateTimeOffset LastSentAt { get; set; }
}
