using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Application.Facade;

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserRequest req, CancellationToken ct = default);
    Task<PagedResult<UserDto>> SearchAsync(string? externalContactId, string? searchTerm, string? pagingToken, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetChangesAsync(string? pagingToken, CancellationToken ct = default);
    Task<UserDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest req, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task AnonymizeAsync(Guid id, CancellationToken ct = default);
    Task BlockAsync(Guid id, CancellationToken ct = default);
    Task UnblockAsync(Guid id, CancellationToken ct = default);
    Task SuspendAsync(Guid id, CancellationToken ct = default);
    Task ResumeAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ParkingCardDto>> GetParkingCardsAsync(Guid id, string? pagingToken, CancellationToken ct = default);
    Task<PagedResult<ValueCardDto>> GetValueCardsAsync(Guid id, string? pagingToken, CancellationToken ct = default);
}

/// <summary>User facade over Parking.Logic (ТЗ §4.2, §5).</summary>
public sealed class UserService(IAppDbContext db, ChangePropagator propagator) : IUserService
{
    public async Task<UserDto> CreateAsync(CreateUserRequest req, CancellationToken ct = default)
    {
        var customerExists = await db.Customers.AnyAsync(c => c.Id == req.CustomerId && !c.IsDeleted, ct);
        if (!customerExists)
            throw new NotFoundException($"Customer {req.CustomerId} not found.");

        var user = new User
        {
            CustomerId = req.CustomerId,
            ExternalContactId = req.ExternalContactId,
            Name = req.Name,
            Surname = req.Surname,
            FirstName = req.FirstName,
            Email = req.Email
        };
        db.Users.Add(user);
        propagator.Enqueue(EntityKind.User, user.Id, PropagationOperation.Create);
        await db.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task<PagedResult<UserDto>> SearchAsync(string? externalContactId, string? searchTerm, string? pagingToken, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking().Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(externalContactId))
            query = query.Where(u => u.ExternalContactId == externalContactId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(u =>
                (u.Name != null && EF.Functions.Like(u.Name, $"%{term}%")) ||
                (u.Surname != null && EF.Functions.Like(u.Surname, $"%{term}%")) ||
                (u.FirstName != null && EF.Functions.Like(u.FirstName, $"%{term}%")) ||
                (u.Email != null && EF.Functions.Like(u.Email, $"%{term}%")));
        }

        return await query.ToPagedAsync(pagingToken, Mapping.ToDto, ct);
    }

    public Task<PagedResult<UserDto>> GetChangesAsync(string? pagingToken, CancellationToken ct = default) =>
        db.Users.AsNoTracking().ToPagedAsync(pagingToken, Mapping.ToDto, ct);

    public async Task<UserDto> GetAsync(Guid id, CancellationToken ct = default) =>
        (await LoadAsync(id, ct)).ToDto();

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest req, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        user.Name = req.Name ?? user.Name;
        user.Surname = req.Surname ?? user.Surname;
        user.FirstName = req.FirstName ?? user.FirstName;
        user.Email = req.Email ?? user.Email;
        user.Touch();

        if (await db.ParkingCards.AnyAsync(c => c.UserId == id, ct) ||
            await db.ValueCards.AnyAsync(c => c.UserId == id, ct))
        {
            propagator.Enqueue(EntityKind.User, id, PropagationOperation.Update);
        }

        await db.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct);
        user.IsDeleted = true;
        user.Touch();
        propagator.Enqueue(EntityKind.User, id, PropagationOperation.Delete);
        await db.SaveChangesAsync(ct);
    }

    public async Task AnonymizeAsync(Guid id, CancellationToken ct = default)
    {
        var user = await LoadAsync(id, ct, includeDeleted: true);
        // Deferred anonymization: mark, background worker overwrites data later (ТЗ §5).
        user.IsDeleted = true;
        user.AnonymizationState = AnonymizationState.ReadyForAnonymization;
        user.Touch();
        propagator.Enqueue(EntityKind.User, id, PropagationOperation.Anonymize);
        await db.SaveChangesAsync(ct);
    }

    public Task BlockAsync(Guid id, CancellationToken ct = default) =>
        SetFlagAsync(id, u => u.IsBlocked = true, PropagationOperation.Block, ct);
    public Task UnblockAsync(Guid id, CancellationToken ct = default) =>
        SetFlagAsync(id, u => u.IsBlocked = false, PropagationOperation.Unblock, ct);
    public Task SuspendAsync(Guid id, CancellationToken ct = default) =>
        SetFlagAsync(id, u => u.IsSuspended = true, PropagationOperation.Suspend, ct);
    public Task ResumeAsync(Guid id, CancellationToken ct = default) =>
        SetFlagAsync(id, u => u.IsSuspended = false, PropagationOperation.Resume, ct);

    public async Task<PagedResult<ParkingCardDto>> GetParkingCardsAsync(Guid id, string? pagingToken, CancellationToken ct = default)
    {
        _ = await LoadAsync(id, ct);
        return await db.ParkingCards.AsNoTracking()
            .Where(c => c.UserId == id && !c.IsDeleted)
            .ToPagedAsync(pagingToken, Mapping.ToDto, ct);
    }

    public async Task<PagedResult<ValueCardDto>> GetValueCardsAsync(Guid id, string? pagingToken, CancellationToken ct = default)
    {
        _ = await LoadAsync(id, ct);
        return await db.ValueCards.AsNoTracking()
            .Where(c => c.UserId == id && !c.IsDeleted)
            .ToPagedAsync(pagingToken, Mapping.ToDto, ct);
    }

    private async Task SetFlagAsync(Guid id, Action<User> mutate, PropagationOperation op, CancellationToken ct)
    {
        var user = await LoadAsync(id, ct);
        mutate(user);
        user.Touch();
        propagator.Enqueue(EntityKind.User, id, op);
        await db.SaveChangesAsync(ct);
    }

    private async Task<User> LoadAsync(Guid id, CancellationToken ct, bool includeDeleted = false) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == id && (includeDeleted || !u.IsDeleted), ct)
        ?? throw new NotFoundException($"User {id} not found.");
}
