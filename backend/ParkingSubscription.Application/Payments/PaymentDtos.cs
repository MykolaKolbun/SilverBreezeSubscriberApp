namespace ParkingSubscription.Application.Payments;

// Client is "web" for the server-rendered Web client (so resolve returns to a web page)
// or null for the mobile app (resolve bounces to the app deep link).
public sealed record InitiatePaymentRequest(Guid UserId, Guid SubscriptionPlanId, DateOnly? StartDate, string? Client = null);

public sealed record InitiatePaymentResult(
    Guid PaymentId, string ProviderPaymentId, string RedirectUrl, long AmountMinor, string Currency);

/// <summary>Async status update from the payment provider (ТЗ §6 webhook).</summary>
public sealed record PaymentWebhookRequest(string ProviderPaymentId, string Status);

public sealed record PaymentDto(
    Guid Id, Guid UserId, Guid SubscriptionPlanId, Guid? ParkingCardId,
    long AmountMinor, string Currency, string Status,
    string? FiscalReceiptId, string? FiscalReceiptUrl, string? FailureReason, DateTimeOffset UpdatedAt);
