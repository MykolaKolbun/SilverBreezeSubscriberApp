using System.Text.Json;
using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Entities;

namespace ParkingSubscription.Infrastructure.Wallet;

/// <summary>
/// Stub Wallet pass service (ТЗ §7). Produces a placeholder .pkpass-like payload
/// and a Google Wallet save link. Replace with real Apple PassKit signing and the
/// Google Wallet API. <see cref="PushPassUpdateAsync"/> is where Apple web-service
/// push / Google pass update would be triggered on card status change.
/// </summary>
public sealed class WalletPassServiceStub(ILogger<WalletPassServiceStub> logger) : IWalletPassService
{
    public WalletPass BuildApplePass(ParkingCard card)
    {
        // Placeholder pass.json payload; a real implementation signs a .pkpass bundle.
        var passJson = JsonSerializer.SerializeToUtf8Bytes(new
        {
            formatVersion = 1,
            passTypeIdentifier = "pass.com.example.parking",
            serialNumber = card.Id.ToString("N"),
            description = "Parking subscription",
            barcode = new { format = "PKBarcodeFormatQR", message = card.QrPayload, messageEncoding = "iso-8859-1" },
            status = card.Status.ToString(),
            validUntil = card.EndDate.ToString("yyyy-MM-dd")
        }, new JsonSerializerOptions { WriteIndented = true });

        return new WalletPass(passJson, "application/vnd.apple.pkpass", $"parking-{card.Id:N}.pkpass");
    }

    public string BuildGoogleWalletLink(ParkingCard card) =>
        $"https://pay.google.com/gp/v/save/stub-{card.Id:N}";

    public Task PushPassUpdateAsync(ParkingCard card, CancellationToken ct = default)
    {
        logger.LogInformation("Wallet pass update pushed for card {CardId} (status {Status})",
            card.Id, card.Status);
        return Task.CompletedTask;
    }
}
