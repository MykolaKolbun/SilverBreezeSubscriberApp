using ParkingSubscription.Domain.Common;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// Application login account (email + password). Linked 1:1 to a <see cref="User"/>
/// in the parking system (ТЗ §3).
/// </summary>
public class AppAccount : Entity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    /// <summary>Token e-mailed to the user to confirm their address (ТЗ §3).</summary>
    public string? EmailConfirmationToken { get; set; }

    /// <summary>Token e-mailed for the forgot-password flow (ТЗ §3).</summary>
    public string? PasswordResetToken { get; set; }
    public DateTimeOffset? PasswordResetTokenExpiresAt { get; set; }

    /// <summary>Opaque refresh token for the current session (ТЗ §3 JWT refresh).</summary>
    public string? RefreshTokenHash { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }
}
