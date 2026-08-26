using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;
using ParkingSubscription.Infrastructure.Persistence;

namespace ParkingSubscription.AdminPanel.Pages;

public sealed class ClientEditModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

    // Editable user data
    [BindProperty] public string? FirstName { get; set; }
    [BindProperty] public string? Surname { get; set; }
    [BindProperty] public string? Mobile { get; set; }
    [BindProperty] public bool PassageLp { get; set; }
    [BindProperty] public bool CheckLp { get; set; }
    [BindProperty] public bool MatchEntryPlate { get; set; }

    // Read-only context
    public string? Email { get; private set; }
    public bool IsBlocked { get; private set; }
    public bool IsSuspended { get; private set; }
    public bool HasActiveSubscription { get; private set; }
    public IReadOnlyList<Vehicle> Vehicles { get; private set; } = [];
    public string? Saved { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!await LoadAsync(ct)) return RedirectToPage("/Clients");
        return Page();
    }

    private async Task<bool> LoadAsync(CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == Id && !u.IsDeleted, ct);
        if (user is null) return false;

        FirstName = user.FirstName;
        Surname = user.Surname;
        Mobile = user.Mobile;
        PassageLp = user.PassageLp;
        CheckLp = user.CheckLp;
        MatchEntryPlate = user.MatchEntryPlate;
        IsBlocked = user.IsBlocked;
        IsSuspended = user.IsSuspended;

        Email = await db.AppAccounts.Where(a => a.UserId == Id).Select(a => a.Email).FirstOrDefaultAsync(ct)
                ?? user.Email;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        HasActiveSubscription = await db.ParkingCards.AnyAsync(
            c => c.UserId == Id && c.Status == CardStatus.Active && !c.IsDeleted && c.EndDate >= today, ct);
        Vehicles = await db.Vehicles.AsNoTracking()
            .Where(v => v.UserId == Id && !v.IsDeleted).OrderBy(v => v.CreatedAt).ToListAsync(ct);
        return true;
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == Id && !u.IsDeleted, ct);
        if (user is null) return RedirectToPage("/Clients");

        var first = Trim(FirstName);
        var surname = Trim(Surname);
        var mobile = Trim(Mobile);

        user.FirstName = first;
        user.Surname = surname;
        user.Mobile = mobile;
        user.PassageLp = PassageLp;
        user.CheckLp = CheckLp;
        user.MatchEntryPlate = MatchEntryPlate;
        user.Touch();
        Enqueue(EntityKind.User, user.Id, PropagationOperation.Update);

        // Client and user are the same person (1:1) — keep the paired Customer in sync.
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == user.CustomerId, ct);
        if (customer is not null)
        {
            customer.FirstName = first;
            customer.Surname = surname;
            customer.Mobile = mobile;
            customer.Touch();
            Enqueue(EntityKind.Customer, customer.Id, PropagationOperation.Update);
        }

        await db.SaveChangesAsync(ct);
        Saved = "user";
        await LoadAsync(ct);
        return Page();
    }

    public Task<IActionResult> OnPostBlockAsync(CancellationToken ct) => SetStatusAsync(block: true, ct);
    public Task<IActionResult> OnPostUnblockAsync(CancellationToken ct) => SetStatusAsync(block: false, ct);
    public Task<IActionResult> OnPostSuspendAsync(CancellationToken ct) => SetSuspendAsync(suspend: true, ct);
    public Task<IActionResult> OnPostResumeAsync(CancellationToken ct) => SetSuspendAsync(suspend: false, ct);

    private async Task<IActionResult> SetStatusAsync(bool block, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == Id && !u.IsDeleted, ct);
        if (user is null) return RedirectToPage("/Clients");
        var op = block ? PropagationOperation.Block : PropagationOperation.Unblock;

        user.IsBlocked = block;
        user.Touch();
        Enqueue(EntityKind.User, user.Id, op);

        // Same person — block/unblock the paired Customer too (cascades to its cards in sweb).
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == user.CustomerId, ct);
        if (customer is not null)
        {
            customer.IsBlocked = block;
            customer.Touch();
            Enqueue(EntityKind.Customer, customer.Id, op);
        }

        await db.SaveChangesAsync(ct);
        Saved = block ? "blocked" : "unblocked";
        await LoadAsync(ct);
        return Page();
    }

    private async Task<IActionResult> SetSuspendAsync(bool suspend, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == Id && !u.IsDeleted, ct);
        if (user is null) return RedirectToPage("/Clients");
        user.IsSuspended = suspend;
        user.Touch();
        // Suspend is a User-level concept in sweb — no CustomerSuspend, so User only.
        Enqueue(EntityKind.User, user.Id, suspend ? PropagationOperation.Suspend : PropagationOperation.Resume);
        await db.SaveChangesAsync(ct);
        Saved = suspend ? "suspended" : "resumed";
        await LoadAsync(ct);
        return Page();
    }

    // Enqueue an outbox message so the API's OutboxPropagationService syncs the change to sweb.
    private void Enqueue(EntityKind kind, Guid entityId, PropagationOperation op) =>
        db.OutboxMessages.Add(new OutboxMessage { EntityKind = kind, EntityId = entityId, Operation = op });

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
