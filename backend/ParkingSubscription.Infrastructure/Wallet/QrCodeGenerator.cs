using ParkingSubscription.Application.Abstractions;
using QRCoder;

namespace ParkingSubscription.Infrastructure.Wallet;

/// <summary>Generates a PNG QR code for the card payload (ТЗ §7) using QRCoder.</summary>
public sealed class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(20);
    }
}
