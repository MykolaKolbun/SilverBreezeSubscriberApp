namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// A secondary identification on a parking card (sweb Identification: type/subType/value,
/// e.g. an extra LP or EXT barcode). Owned by <see cref="ParkingCard"/>.
/// </summary>
public class CardIdentification
{
    public string Type { get; set; } = string.Empty;
    public string SubType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
