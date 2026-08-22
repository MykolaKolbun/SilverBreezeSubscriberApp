using ParkingSubscription.Domain.Common;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// A parking subscription tariff the user can buy (ТЗ §2.3, §10.5). Exact plan
/// structure is an open question; this covers fixed-duration plans.
/// </summary>
public class SubscriptionPlan : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Price in minor units (e.g. cents/kopiykas).</summary>
    public long PriceMinor { get; set; }
    public string Currency { get; set; } = "UAH";

    /// <summary>Validity length of the card issued for this plan, in days.</summary>
    public int DurationDays { get; set; }

    public bool IsActive { get; set; } = true;
}
