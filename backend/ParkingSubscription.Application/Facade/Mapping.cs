using ParkingSubscription.Domain.Entities;

namespace ParkingSubscription.Application.Facade;

/// <summary>Entity → DTO projections shared across facade services.</summary>
public static class Mapping
{
    public static CustomerDto ToDto(this Customer c) => new(
        c.Id, c.ExternalContactId, c.Name, c.Surname, c.FirstName, c.Email,
        c.IsBlocked, c.IsDeleted, c.UpdatedAt);

    public static UserDto ToDto(this User u) => new(
        u.Id, u.CustomerId, u.ExternalContactId, u.Name, u.Surname, u.FirstName, u.Email,
        u.IsBlocked, u.IsSuspended, u.AnonymizationState, u.IsDeleted, u.UpdatedAt);

    public static ParkingCardDto ToDto(this ParkingCard p) => new(
        p.Id, p.UserId, p.ExternalCardId, p.SubscriptionPlanId, p.StartDate, p.EndDate,
        p.Status, p.AnonymizationState, p.QrPayload, p.IsDeleted, p.UpdatedAt);

    public static ValueCardDto ToDto(this ValueCard v) => new(
        v.Id, v.UserId, v.ExternalCardId, v.BalanceMinor, v.Currency, v.Status, v.IsDeleted, v.UpdatedAt);

    public static VehicleDto ToDto(this Vehicle v) => new(
        v.Id, v.UserId, v.PlateNumber, v.Country, v.Make, v.Model, v.IsDeleted, v.UpdatedAt);
}
