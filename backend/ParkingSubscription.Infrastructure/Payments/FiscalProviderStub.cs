using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Entities;

namespace ParkingSubscription.Infrastructure.Payments;

/// <summary>Stub fiscalization provider (ТЗ §6). Returns a synthetic receipt.</summary>
public sealed class FiscalProviderStub(ILogger<FiscalProviderStub> logger) : IFiscalProvider
{
    public Task<FiscalReceipt> FiscalizeAsync(Payment payment, CancellationToken ct = default)
    {
        var receiptId = $"rcpt_{Guid.NewGuid():N}";
        logger.LogInformation("Stub fiscalization for payment {PaymentId}: receipt {ReceiptId}",
            payment.Id, receiptId);
        return Task.FromResult(new FiscalReceipt(receiptId, $"https://fiscal.example/receipts/{receiptId}"));
    }

    // The stub has no rendered receipt image.
    public Task<FiscalReceiptImage?> GetReceiptImageAsync(string receiptId, CancellationToken ct = default) =>
        Task.FromResult<FiscalReceiptImage?>(null);
}
