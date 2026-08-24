using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;

namespace ParkingSubscription.Infrastructure.Payments;

/// <summary>
/// Stub payment provider (ТЗ §6, §10.3) — used in tests and dev (Payment:Provider != "iPay").
/// <see cref="CreatePaymentAsync"/> returns the success return-URL as the "redirect", and
/// <see cref="GetStatusAsync"/> always reports success, so the resolve flow completes without
/// any real gateway. The <c>/payments/webhook</c> path also remains for the Razor dev UI.
/// </summary>
public sealed class PaymentProviderStub(ILogger<PaymentProviderStub> logger) : IPaymentProvider
{
    public Task<PaymentIntent> CreatePaymentAsync(PaymentInitiation initiation, CancellationToken ct = default)
    {
        var providerId = $"pi_{Guid.NewGuid():N}";
        logger.LogInformation("Stub payment created {ProviderId} for {Amount} {Currency} (ref {Reference})",
            providerId, initiation.AmountMinor, initiation.Currency, initiation.Reference);
        // No hosted page — send the browser straight to the success return URL.
        return Task.FromResult(new PaymentIntent(providerId, initiation.SuccessUrl));
    }

    public Task<ProviderPaymentStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default) =>
        Task.FromResult(new ProviderPaymentStatusResult(ProviderPaymentStatus.Succeeded, 0));

    public Task RefundAsync(string providerPaymentId, CancellationToken ct = default)
    {
        logger.LogInformation("Stub refund issued for {ProviderId}", providerPaymentId);
        return Task.CompletedTask;
    }
}
