using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParkingSubscription.Api.Subscribe;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;
using ParkingSubscription.Infrastructure.Persistence;
// Both namespaces define ParkingCard/ValueCard; here the bare names mean our entities.
using ParkingCard = ParkingSubscription.Domain.Entities.ParkingCard;
using ValueCard = ParkingSubscription.Domain.Entities.ValueCard;

namespace ParkingSubscription.Api.ParkingLogic;

/// <summary>
/// Real <see cref="IParkingLogicClient"/> backed by the SKIDATA sweb(R) Subscribe API
/// (the generated <see cref="Client"/>). Drained by the outbox: each message is
/// translated into the matching sweb call for its (kind, operation), and the UUID
/// sweb assigns on create is stored back on our entity so later update/block/etc.
/// can target it. Runs on the same scoped <see cref="AppDbContext"/> as the outbox
/// worker, so those id writes are committed together with the delivery status.
/// </summary>
public sealed class SkidataParkingLogicClient(
    AppDbContext db,
    Client client,
    IClock clock,
    IOptions<SkidataOptions> options,
    ILogger<SkidataParkingLogicClient> logger) : IParkingLogicClient
{
    private readonly SkidataOptions _opt = options.Value;

    public async Task<string> PropagateAsync(
        EntityKind kind, Guid entityId, PropagationOperation op, string? payloadJson, CancellationToken ct = default)
    {
        return kind switch
        {
            EntityKind.Customer => await HandleCustomerAsync(entityId, op, ct),
            EntityKind.User => await HandleUserAsync(entityId, op, ct),
            EntityKind.ParkingCard => await HandleParkingCardAsync(entityId, op, ct),
            EntityKind.ValueCard => await HandleValueCardAsync(entityId, op, ct),
            _ => throw new NotSupportedException($"Unknown entity kind {kind}")
        };
    }

    // ---- Customer ----------------------------------------------------------

    private async Task<string> HandleCustomerAsync(Guid id, PropagationOperation op, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException($"Customer {id} not found for propagation");

        var today = ToDate(clock.Today);
        switch (op)
        {
            case PropagationOperation.Create:
            case PropagationOperation.Update:
                if (customer.SkidataCustomerId is Guid updId)
                    await client.UpdateCustomerAsync(updId, MapCustomer(customer), ct);
                else
                    await EnsureCustomerAsync(customer, ct);
                break;
            case PropagationOperation.Block:
                await client.BlockCustomerAsync(await EnsureCustomerAsync(customer, ct), new BlockCustomer { StartDate = today }, ct);
                break;
            case PropagationOperation.Unblock:
                if (customer.SkidataCustomerId is Guid unId) await client.UnblockCustomerAsync(unId, ct);
                break;
            case PropagationOperation.Anonymize:
                if (customer.SkidataCustomerId is Guid anId) await client.AnonymizeCustomerAsync(anId, ct);
                break;
            case PropagationOperation.Delete:
                if (customer.SkidataCustomerId is Guid delId) await client.DeleteCustomerAsync(delId, ct);
                break;
            default:
                logger.LogWarning("Unsupported customer operation {Op}", op);
                break;
        }
        return RemoteRef(EntityKind.Customer, customer.SkidataCustomerId);
    }

    /// <summary>Returns the sweb customer UUID, creating the customer there if needed.</summary>
    private async Task<Guid> EnsureCustomerAsync(Customer customer, CancellationToken ct)
    {
        if (customer.SkidataCustomerId is Guid existing) return existing;

        var created = await client.CreateCustomerAsync(customer.Id, MapCustomer(customer), ct);
        customer.SkidataCustomerId = created.CustomerId;
        customer.Touch();
        logger.LogInformation("SKIDATA customer created {LocalId} -> {RemoteId}", customer.Id, created.CustomerId);
        return created.CustomerId;
    }

    private CreateCustomer MapCustomer(Customer c) => new()
    {
        Surname = NonEmpty(c.Surname ?? c.Name, "Customer"),
        Firstname = c.FirstName,
        Email = c.Email,
        ExternalContactId = c.ExternalContactId ?? c.Id.ToString("N")
    };

    // ---- User --------------------------------------------------------------

    private async Task<string> HandleUserAsync(Guid id, PropagationOperation op, CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.Customer).FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new InvalidOperationException($"User {id} not found for propagation");

        var today = ToDate(clock.Today);
        switch (op)
        {
            case PropagationOperation.Create:
            case PropagationOperation.Update:
                if (user.SkidataUserId is Guid updId)
                    await client.UpdateUserAsync(updId, await MapUserAsync(user, ct), ct);
                else
                    await EnsureUserAsync(user, ct);
                break;
            case PropagationOperation.Block:
                await client.BlockUserAsync(await EnsureUserAsync(user, ct), new BlockUser { StartDate = today }, ct);
                break;
            case PropagationOperation.Unblock:
                if (user.SkidataUserId is Guid unId) await client.UnblockUserAsync(unId, ct);
                break;
            case PropagationOperation.Suspend:
                await client.SuspendUserAsync(await EnsureUserAsync(user, ct),
                    new SuspendUser { StartDate = today, EndDate = today }, ct);
                break;
            case PropagationOperation.Resume:
                if (user.SkidataUserId is Guid reId) await client.ResumeUserAsync(reId, ct);
                break;
            case PropagationOperation.Anonymize:
                if (user.SkidataUserId is Guid anId) await client.AnonymizeUserAsync(anId, ct);
                break;
            case PropagationOperation.Delete:
                if (user.SkidataUserId is Guid delId) await client.DeleteUserAsync(delId, ct);
                break;
            default:
                logger.LogWarning("Unsupported user operation {Op}", op);
                break;
        }
        return RemoteRef(EntityKind.User, user.SkidataUserId);
    }

    /// <summary>Returns the sweb user UUID, creating the user (and its customer) there if needed.</summary>
    private async Task<Guid> EnsureUserAsync(User user, CancellationToken ct)
    {
        if (user.SkidataUserId is Guid existing) return existing;

        var created = await client.CreateUserAsync(user.Id, await MapUserAsync(user, ct), ct);
        user.SkidataUserId = created.UserId;
        user.Touch();
        logger.LogInformation("SKIDATA user created {LocalId} -> {RemoteId}", user.Id, created.UserId);
        return created.UserId;
    }

    private async Task<CreateUser> MapUserAsync(User user, CancellationToken ct)
    {
        var customer = user.Customer ?? await db.Customers.FirstOrDefaultAsync(c => c.Id == user.CustomerId, ct)
            ?? throw new InvalidOperationException($"Customer {user.CustomerId} of user {user.Id} not found");
        var customerRemoteId = await EnsureCustomerAsync(customer, ct);

        var dto = new CreateUser
        {
            Surname = NonEmpty(user.Surname ?? user.Name, "Parker"),
            Firstname = user.FirstName,
            Email = user.Email,
            ExternalContactId = user.ExternalContactId ?? user.Id.ToString("N")
        };
        if (_opt.CustomerLinkField.Equals("group", StringComparison.OrdinalIgnoreCase))
            dto.GroupCustomerId = customerRemoteId;
        else
            dto.B2bCustomerId = customerRemoteId;
        return dto;
    }

    // ---- Parking card ------------------------------------------------------

    private async Task<string> HandleParkingCardAsync(Guid id, PropagationOperation op, CancellationToken ct)
    {
        var facility = RequireFacility();
        var card = await db.ParkingCards.Include(c => c.User).ThenInclude(u => u!.Customer)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException($"ParkingCard {id} not found for propagation");

        var today = ToDate(clock.Today);
        switch (op)
        {
            case PropagationOperation.Create:
                if (card.SkidataCardId is null) await EnsureParkingCardAsync(facility, card, ct);
                break;
            case PropagationOperation.Update:
                if (card.SkidataCardId is Guid updId)
                    await client.UpdateParkingCardAsync(facility, updId, new UpdateParkingCard
                    {
                        ValidFrom = ToDate(card.StartDate),
                        ValidTo = ToDate(card.EndDate),
                        PrimaryId = QrIdentification(card)
                    }, ct);
                else
                    await EnsureParkingCardAsync(facility, card, ct);
                break;
            case PropagationOperation.Block:
                await client.BlockParkingCardAsync(facility, await EnsureParkingCardAsync(facility, card, ct),
                    new BlockParkingCard { StartDate = today }, ct);
                break;
            case PropagationOperation.Unblock:
                if (card.SkidataCardId is Guid unId) await client.UnblockParkingCardAsync(facility, unId, ct);
                break;
            case PropagationOperation.Suspend:
                await client.SuspendParkingCardAsync(facility, await EnsureParkingCardAsync(facility, card, ct),
                    new SuspendParkingCard { StartDate = ToDate(card.StartDate), EndDate = ToDate(card.EndDate) }, ct);
                break;
            case PropagationOperation.Resume:
                if (card.SkidataCardId is Guid reId) await client.ResumeParkingCardAsync(facility, reId, ct);
                break;
            case PropagationOperation.Anonymize:
                if (card.SkidataCardId is Guid anId) await client.AnonymizeParkingCardAsync(facility, anId, ct);
                break;
            case PropagationOperation.Delete:
                if (card.SkidataCardId is Guid delId) await client.DeleteParkingCardAsync(facility, delId, ct);
                break;
            default:
                logger.LogWarning("Unsupported parking card operation {Op}", op);
                break;
        }
        return RemoteRef(EntityKind.ParkingCard, card.SkidataCardId);
    }

    private async Task<Guid> EnsureParkingCardAsync(string facility, ParkingCard card, CancellationToken ct)
    {
        if (card.SkidataCardId is Guid existing) return existing;

        var user = card.User ?? await db.Users.Include(u => u.Customer).FirstOrDefaultAsync(u => u.Id == card.UserId, ct)
            ?? throw new InvalidOperationException($"User {card.UserId} of card {card.Id} not found");
        var userRemoteId = await EnsureUserAsync(user, ct);

        var body = new CreateParkingCard
        {
            UserId = userRemoteId,
            ValidFrom = ToDate(card.StartDate),
            ValidTo = ToDate(card.EndDate),
            ExternalCardId = card.ExternalCardId ?? card.Id.ToString("N"),
            PrimaryId = QrIdentification(card)
        };
        if (_opt.ParkingProductId is Guid pid) body.ProductId = pid;

        var created = await client.CreateParkingCardAsync(facility, card.Id, body, ct);
        card.SkidataCardId = created.ParkingCardId;
        card.Touch();
        logger.LogInformation("SKIDATA parking card created {LocalId} -> {RemoteId}", card.Id, created.ParkingCardId);
        return created.ParkingCardId;
    }

    private Identification QrIdentification(ParkingCard card) => new()
    {
        Type = _opt.QrIdentificationType,
        SubType = _opt.QrIdentificationSubType,
        Value = card.QrPayload
    };

    // ---- Value card --------------------------------------------------------

    private async Task<string> HandleValueCardAsync(Guid id, PropagationOperation op, CancellationToken ct)
    {
        var facility = RequireFacility();
        var card = await db.ValueCards.Include(c => c.User).ThenInclude(u => u!.Customer)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException($"ValueCard {id} not found for propagation");

        var today = ToDate(clock.Today);
        switch (op)
        {
            case PropagationOperation.Create:
            case PropagationOperation.Update:
                if (card.SkidataCardId is null) await EnsureValueCardAsync(facility, card, ct);
                break;
            case PropagationOperation.Block:
                await client.BlockValueCardAsync(facility, await EnsureValueCardAsync(facility, card, ct),
                    new BlockValueCard { StartDate = today }, ct);
                break;
            case PropagationOperation.Unblock:
                if (card.SkidataCardId is Guid unId) await client.UnblockValueCardAsync(facility, unId, ct);
                break;
            case PropagationOperation.Anonymize:
                if (card.SkidataCardId is Guid anId) await client.AnonymizeValueCardAsync(facility, anId, ct);
                break;
            case PropagationOperation.Delete:
                if (card.SkidataCardId is Guid delId) await client.DeleteValueCardAsync(facility, delId, ct);
                break;
            default:
                logger.LogWarning("Unsupported value card operation {Op}", op);
                break;
        }
        return RemoteRef(EntityKind.ValueCard, card.SkidataCardId);
    }

    private async Task<Guid> EnsureValueCardAsync(string facility, ValueCard card, CancellationToken ct)
    {
        if (card.SkidataCardId is Guid existing) return existing;

        var user = card.User ?? await db.Users.Include(u => u.Customer).FirstOrDefaultAsync(u => u.Id == card.UserId, ct)
            ?? throw new InvalidOperationException($"User {card.UserId} of value card {card.Id} not found");
        var userRemoteId = await EnsureUserAsync(user, ct);

        var body = new CreateValueCard
        {
            UserId = userRemoteId,
            ExternalCardId = card.ExternalCardId ?? card.Id.ToString("N")
        };
        if (_opt.ValueProductId is Guid pid) body.ProductId = pid;

        var created = await client.CreateValueCardAsync(facility, card.Id, body, ct);
        card.SkidataCardId = created.ValueCardId;
        card.Touch();
        logger.LogInformation("SKIDATA value card created {LocalId} -> {RemoteId}", card.Id, created.ValueCardId);
        return created.ValueCardId;
    }

    // ---- Helpers -----------------------------------------------------------

    private string RequireFacility() =>
        _opt.FacilityNumber ?? throw new InvalidOperationException("ParkingLogic:Skidata:FacilityNumber is not configured");

    private static DateTimeOffset ToDate(DateOnly d) =>
        new(d.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private static string NonEmpty(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string RemoteRef(EntityKind kind, Guid? remoteId) =>
        remoteId is Guid g ? $"skidata:{kind}:{g:N}" : $"skidata:{kind}:pending";
}
