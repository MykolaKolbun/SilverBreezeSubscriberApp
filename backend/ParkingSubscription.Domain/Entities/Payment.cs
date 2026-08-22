using ParkingSubscription.Domain.Common;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// A payment for a parking subscription, plus its fiscalization state (ТЗ §6).
/// The parking card is activated only after a successful payment.
/// </summary>
public class Payment : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }

    /// <summary>Card activated on success; null until then.</summary>
    public Guid? ParkingCardId { get; set; }
    public ParkingCard? ParkingCard { get; set; }

    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "UAH";

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Reference returned by the payment provider (ТЗ §6 webhook correlation).</summary>
    public string? ProviderPaymentId { get; set; }

    /// <summary>Receipt id returned by the fiscal provider after success (ТЗ §6).</summary>
    public string? FiscalReceiptId { get; set; }

    public string? FailureReason { get; set; }
}
