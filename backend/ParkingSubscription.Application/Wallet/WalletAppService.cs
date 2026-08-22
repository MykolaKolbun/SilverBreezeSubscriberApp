using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Domain.Entities;

namespace ParkingSubscription.Application.Wallet;

public interface IWalletAppService
{
    Task<byte[]> GetQrPngAsync(Guid cardId, CancellationToken ct = default);
    Task<WalletPass> GetApplePassAsync(Guid cardId, CancellationToken ct = default);
    Task<string> GetGoogleWalletLinkAsync(Guid cardId, CancellationToken ct = default);
}

/// <summary>Builds QR image and Wallet passes for a parking card (ТЗ §7).</summary>
public sealed class WalletAppService(
    IAppDbContext db,
    IQrCodeGenerator qr,
    IWalletPassService wallet) : IWalletAppService
{
    public async Task<byte[]> GetQrPngAsync(Guid cardId, CancellationToken ct = default)
    {
        var card = await LoadAsync(cardId, ct);
        return qr.GeneratePng(card.QrPayload);
    }

    public async Task<WalletPass> GetApplePassAsync(Guid cardId, CancellationToken ct = default) =>
        wallet.BuildApplePass(await LoadAsync(cardId, ct));

    public async Task<string> GetGoogleWalletLinkAsync(Guid cardId, CancellationToken ct = default) =>
        wallet.BuildGoogleWalletLink(await LoadAsync(cardId, ct));

    private async Task<ParkingCard> LoadAsync(Guid cardId, CancellationToken ct) =>
        await db.ParkingCards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cardId && !c.IsDeleted, ct)
        ?? throw new NotFoundException($"Parking card {cardId} not found.");
}
