namespace ParkingSubscription.Application.Payments;

/// <summary>
/// Venue-wide subscription limits (bound from the "Subscription" config section).
/// </summary>
public sealed class SubscriptionOptions
{
    public const string SectionName = "Subscription";

    /// <summary>
    /// Maximum number of concurrently active subscriptions across ALL users (parking
    /// capacity). A new purchase is rejected up front when this many active (current or
    /// upcoming, non-deleted) cards already exist.
    /// </summary>
    public int MaxActiveSubscriptions { get; set; } = 50;
}
