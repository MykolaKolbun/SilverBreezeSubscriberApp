using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Application.Facade;

// ---- Customer (ТЗ §4.1) ----
public sealed record CreateCustomerRequest(
    string? ExternalContactId, string? Name, string? Surname, string? FirstName, string? Email);

public sealed record UpdateCustomerRequest(
    string? Name, string? Surname, string? FirstName, string? Email);

public sealed record CustomerDto(
    Guid Id, string? ExternalContactId, string? Name, string? Surname, string? FirstName,
    string? Email, bool IsBlocked, bool IsDeleted, DateTimeOffset UpdatedAt);

// ---- User (ТЗ §4.2) ----
public sealed record CreateUserRequest(
    Guid CustomerId, string? ExternalContactId, string? Name, string? Surname, string? FirstName, string? Email);

public sealed record UpdateUserRequest(
    string? Name, string? Surname, string? FirstName, string? Email, string? Mobile = null);

public sealed record UserDto(
    Guid Id, Guid CustomerId, string? ExternalContactId, string? Name, string? Surname, string? FirstName,
    string? Email, string? Mobile, bool IsBlocked, bool IsSuspended, AnonymizationState AnonymizationState,
    bool IsDeleted, DateTimeOffset UpdatedAt);

// ---- Parking card (ТЗ §4.3) ----
public sealed record CreateParkingCardRequest(
    Guid UserId, Guid? SubscriptionPlanId, DateOnly StartDate, DateOnly EndDate, string? ExternalCardId);

public sealed record UpdateParkingCardRequest(DateOnly? StartDate, DateOnly? EndDate, string? ExternalCardId);

public sealed record ParkingCardDto(
    Guid Id, Guid UserId, string? ExternalCardId, Guid? SubscriptionPlanId,
    DateOnly StartDate, DateOnly EndDate, CardStatus Status, AnonymizationState AnonymizationState,
    string QrPayload, bool IsDeleted, DateTimeOffset UpdatedAt);

// ---- Value card (ТЗ §4.2, §10.2) ----
public sealed record CreateValueCardRequest(Guid UserId, long BalanceMinor, string Currency, string? ExternalCardId);
public sealed record ValueCardDto(
    Guid Id, Guid UserId, string? ExternalCardId, long BalanceMinor, string Currency,
    CardStatus Status, bool IsDeleted, DateTimeOffset UpdatedAt);

// ---- Vehicle (license plate for parking entry) ----
public sealed record CreateVehicleRequest(Guid UserId, string PlateNumber, string? Country, string? Make, string? Model);
public sealed record UpdateVehicleRequest(string? PlateNumber, string? Country, string? Make, string? Model);
public sealed record VehicleDto(
    Guid Id, Guid UserId, string PlateNumber, string Country, string? Make, string? Model,
    bool IsDeleted, DateTimeOffset UpdatedAt);
