using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Application.Facade;

public interface ICustomerService
{
    Task<CustomerDto> CreateAsync(CreateCustomerRequest req, CancellationToken ct = default);
    Task<PagedResult<CustomerDto>> SearchAsync(string? externalContactId, string? searchTerm, string? pagingToken, CancellationToken ct = default);
    Task<PagedResult<CustomerDto>> GetChangesAsync(string? pagingToken, CancellationToken ct = default);
    Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest req, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetUsersAsync(Guid id, string? pagingToken, CancellationToken ct = default);
    Task BlockAsync(Guid id, CancellationToken ct = default);
    Task UnblockAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Customer facade over Parking.Logic (ТЗ §4.1, §5).</summary>
public sealed class CustomerService(IAppDbContext db, ChangePropagator propagator, IClock clock) : ICustomerService
{
    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest req, CancellationToken ct = default)
    {
        var customer = new Customer
        {
            ExternalContactId = req.ExternalContactId,
            Name = req.Name,
            Surname = req.Surname,
            FirstName = req.FirstName,
            Email = req.Email
        };
        db.Customers.Add(customer);
        propagator.Enqueue(EntityKind.Customer, customer.Id, PropagationOperation.Create);
        await db.SaveChangesAsync(ct);
        return customer.ToDto();
    }

    public async Task<PagedResult<CustomerDto>> SearchAsync(string? externalContactId, string? searchTerm, string? pagingToken, CancellationToken ct = default)
    {
        var query = db.Customers.AsNoTracking().Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(externalContactId))
            query = query.Where(c => c.ExternalContactId == externalContactId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(c =>
                (c.Name != null && EF.Functions.Like(c.Name, $"%{term}%")) ||
                (c.Surname != null && EF.Functions.Like(c.Surname, $"%{term}%")) ||
                (c.FirstName != null && EF.Functions.Like(c.FirstName, $"%{term}%")) ||
                (c.Email != null && EF.Functions.Like(c.Email, $"%{term}%")));
        }

        return await query.ToPagedAsync(pagingToken, Mapping.ToDto, ct);
    }

    public Task<PagedResult<CustomerDto>> GetChangesAsync(string? pagingToken, CancellationToken ct = default) =>
        db.Customers.AsNoTracking().ToPagedAsync(pagingToken, Mapping.ToDto, ct);

    public async Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default) =>
        (await LoadAsync(id, ct)).ToDto();

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest req, CancellationToken ct = default)
    {
        var customer = await LoadAsync(id, ct);
        customer.Name = req.Name ?? customer.Name;
        customer.Surname = req.Surname ?? customer.Surname;
        customer.FirstName = req.FirstName ?? customer.FirstName;
        customer.Email = req.Email ?? customer.Email;
        customer.Touch();

        // Propagate only when the customer has cards (ТЗ §4.1).
        if (await CustomerHasCardsAsync(id, ct))
            propagator.Enqueue(EntityKind.Customer, id, PropagationOperation.Update);

        await db.SaveChangesAsync(ct);
        return customer.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await LoadAsync(id, ct);
        var today = clock.Today;

        // Cascade: mark customer + all users deleted, set EndDate=today on parking cards (ТЗ §5).
        customer.IsDeleted = true;
        customer.Touch();

        var users = await db.Users.Where(u => u.CustomerId == id).ToListAsync(ct);
        foreach (var user in users)
        {
            user.IsDeleted = true;
            user.Touch();
        }

        var userIds = users.Select(u => u.Id).ToList();
        var cards = await db.ParkingCards.Where(c => userIds.Contains(c.UserId)).ToListAsync(ct);
        foreach (var card in cards)
        {
            card.EndDate = today;
            card.Status = CardStatus.Deleted;
            card.IsDeleted = true;
            card.Touch();
        }

        propagator.Enqueue(EntityKind.Customer, id, PropagationOperation.Delete);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(Guid id, string? pagingToken, CancellationToken ct = default)
    {
        _ = await LoadAsync(id, ct);
        return await db.Users.AsNoTracking()
            .Where(u => u.CustomerId == id && !u.IsDeleted)
            .ToPagedAsync(pagingToken, Mapping.ToDto, ct);
    }

    public Task BlockAsync(Guid id, CancellationToken ct = default) => SetBlockedAsync(id, true, ct);
    public Task UnblockAsync(Guid id, CancellationToken ct = default) => SetBlockedAsync(id, false, ct);

    private async Task SetBlockedAsync(Guid id, bool blocked, CancellationToken ct)
    {
        var customer = await LoadAsync(id, ct);
        customer.IsBlocked = blocked;
        customer.Touch();
        propagator.Enqueue(EntityKind.Customer, id,
            blocked ? PropagationOperation.Block : PropagationOperation.Unblock);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Customer> LoadAsync(Guid id, CancellationToken ct) =>
        await db.Customers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct)
        ?? throw new NotFoundException($"Customer {id} not found.");

    private async Task<bool> CustomerHasCardsAsync(Guid customerId, CancellationToken ct)
    {
        var userIds = db.Users.Where(u => u.CustomerId == customerId).Select(u => u.Id);
        return await db.ParkingCards.AnyAsync(c => userIds.Contains(c.UserId), ct)
            || await db.ValueCards.AnyAsync(c => userIds.Contains(c.UserId), ct);
    }
}
