using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// A car park a parking card is valid for, with its entry permission (sweb CarPark).
/// Owned by <see cref="ParkingCard"/> — stored in its own table, no independent identity.
/// </summary>
public class CardCarPark
{
    public int CarParkNumber { get; set; }
    public CarParkEntryType EntryType { get; set; }
}
