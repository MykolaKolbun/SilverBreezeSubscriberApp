namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// The rendered fiscal receipt image (Checkbox PNG) captured once after fiscalization
/// and stored so it is served from our DB — no per-view call to the fiscal provider.
/// Keyed by the payment it belongs to.
/// </summary>
public class FiscalReceiptBlob
{
    public Guid PaymentId { get; set; }
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = "image/png";
    public DateTimeOffset CreatedAt { get; set; }
}
