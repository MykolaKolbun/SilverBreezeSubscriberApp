using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;

namespace ParkingSubscription.Infrastructure.Payments;

/// <summary>
/// Stub payment provider (ТЗ §6, §10.3). Immediately returns a synthetic intent;
/// real settlement is driven by the webhook endpoint. Swap for Stripe/LiqPay/Fondy.
/// </summary>
public sealed class PaymentProviderStub(ILogger<PaymentProviderStub> logger) : IPaymentProvider
{
    public Task<PaymentIntent> CreatePaymentAsync(long amountMinor, string currency, string reference, CancellationToken ct = default)
    {
        var providerId = $"pi_{Guid.NewGuid():N}";
        logger.LogInformation("Stub payment created {ProviderId} for {Amount} {Currency} (ref {Reference})",
            providerId, amountMinor, currency, reference);
        return Task.FromResult(new PaymentIntent(providerId, $"secret_{Guid.NewGuid():N}"));
    }

    public Task RefundAsync(string providerPaymentId, CancellationToken ct = default)
    {
        logger.LogInformation("Stub refund issued for {ProviderId}", providerPaymentId);
        return Task.CompletedTask;
    }
}
