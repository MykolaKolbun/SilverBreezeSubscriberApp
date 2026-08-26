using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Application.Facade;

public interface IVehicleService
{
    Task<VehicleDto> CreateAsync(CreateVehicleRequest req, CancellationToken ct = default);
    Task<VehicleDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<VehicleDto>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleRequest req, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// CRUD for a user's vehicles (license plates). A change enqueues a User Update so the
/// outbox re-syncs the owner (and its <c>licensePlates</c>) to the parking system.
/// </summary>
public sealed class VehicleService(IAppDbContext db, ChangePropagator propagator) : IVehicleService
{
    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest req, CancellationToken ct = default)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == req.UserId && !u.IsDeleted, ct);
        if (!userExists)
            throw new NotFoundException($"User {req.UserId} not found.");

        var plate = Normalize(req.PlateNumber);
        if (string.IsNullOrEmpty(plate))
            throw new ValidationException("Plate number is required.");

        var vehicle = new Vehicle
        {
            UserId = req.UserId,
            PlateNumber = plate,
            Country = NormalizeCountry(req.Country),
            Make = Trim(req.Make),
            Model = Trim(req.Model)
        };
        db.Vehicles.Add(vehicle);
        propagator.Enqueue(EntityKind.User, vehicle.UserId, PropagationOperation.Update);
        await db.SaveChangesAsync(ct);
        return vehicle.ToDto();
    }

    public async Task<VehicleDto> GetAsync(Guid id, CancellationToken ct = default) =>
        (await LoadAsync(id, ct)).ToDto();

    public async Task<IReadOnlyList<VehicleDto>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await db.Vehicles.AsNoTracking()
            .Where(v => v.UserId == userId && !v.IsDeleted)
            .OrderBy(v => v.CreatedAt)
            .Select(v => v.ToDto())
            .ToListAsync(ct);

    public async Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleRequest req, CancellationToken ct = default)
    {
        var vehicle = await LoadAsync(id, ct);
        if (req.PlateNumber is not null)
        {
            var plate = Normalize(req.PlateNumber);
            if (string.IsNullOrEmpty(plate))
                throw new ValidationException("Plate number cannot be empty.");
            vehicle.PlateNumber = plate;
        }
        if (req.Country is not null) vehicle.Country = NormalizeCountry(req.Country);
        if (req.Make is not null) vehicle.Make = Trim(req.Make);
        if (req.Model is not null) vehicle.Model = Trim(req.Model);
        vehicle.Touch();
        propagator.Enqueue(EntityKind.User, vehicle.UserId, PropagationOperation.Update);
        await db.SaveChangesAsync(ct);
        return vehicle.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await LoadAsync(id, ct);
        vehicle.IsDeleted = true;
        vehicle.Touch();
        propagator.Enqueue(EntityKind.User, vehicle.UserId, PropagationOperation.Update);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Vehicle> LoadAsync(Guid id, CancellationToken ct) =>
        await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct)
        ?? throw new NotFoundException($"Vehicle {id} not found.");

    private static string Normalize(string? plate) => (plate ?? string.Empty).Trim();
    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string NormalizeCountry(string? c) =>
        string.IsNullOrWhiteSpace(c) ? "UA" : c.Trim().ToUpperInvariant();
}
