namespace ParkingSubscription.Application.Payments;

public sealed record InitiatePaymentRequest(Guid UserId, Guid SubscriptionPlanId, DateOnly? StartDate);

public sealed record InitiatePaymentResult(
    Guid PaymentId, string ProviderPaymentId, string ClientSecret, long AmountMinor, string Currency);

/// <summary>Async status update from the payment provider (ТЗ §6 webhook).</summary>
public sealed record PaymentWebhookRequest(string ProviderPaymentId, string Status);

public sealed record PaymentDto(
    Guid Id, Guid UserId, Guid SubscriptionPlanId, Guid? ParkingCardId,
    long AmountMinor, string Currency, string Status,
    string? FiscalReceiptId, string? FailureReason, DateTimeOffset UpdatedAt);
