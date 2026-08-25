namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// One-time SMS code for passwordless phone login (ТЗ §3). One row per phone number
/// (replaced on each request). The code is stored HASHED; only its hash is kept.
/// </summary>
public class PhoneOtp
{
    /// <summary>Phone in E.164 (primary key — one active OTP per number).</summary>
    public string Phone { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Failed verification attempts against the current code.</summary>
    public int Attempts { get; set; }

    /// <summary>When the current code was last sent (for resend rate-limiting).</summary>
    public DateTimeOffset LastSentAt { get; set; }
}
