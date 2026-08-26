using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Infrastructure.Persistence;

namespace ParkingSubscription.AdminPanel.Pages;

public sealed class PlansModel(AppDbContext db) : PageModel
{
    public IReadOnlyList<SubscriptionPlan> Plans { get; private set; } = [];
    public string? Saved { get; private set; }
    public string? Error { get; private set; }

    // New-plan form
    [BindProperty] public string? NewCode { get; set; }
    [BindProperty] public string? NewName { get; set; }
    [BindProperty] public decimal NewPrice { get; set; }
    [BindProperty] public int NewDurationDays { get; set; }
    [BindProperty] public string? NewArticleId { get; set; }
    [BindProperty] public bool NewActive { get; set; } = true;

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    private async Task LoadAsync(CancellationToken ct) =>
        Plans = await db.SubscriptionPlans.AsNoTracking()
            .Where(p => !p.IsDeleted).OrderBy(p => p.PriceMinor).ToListAsync(ct);

    public async Task<IActionResult> OnPostAddAsync(CancellationToken ct)
    {
        var code = NewCode?.Trim();
        var name = NewName?.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            Error = "Код і назва обовʼязкові.";
        else if (NewDurationDays <= 0)
            Error = "Тривалість має бути більшою за 0.";
        else if (await db.SubscriptionPlans.AnyAsync(p => p.Code == code && !p.IsDeleted, ct))
            Error = $"Тариф з кодом '{code}' уже існує.";
        else
        {
            db.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Code = code,
                Name = name,
                PriceMinor = ToMinor(NewPrice),
                Currency = "UAH",
                DurationDays = NewDurationDays,
                ArticleId = Trim(NewArticleId),
                IsActive = NewActive
            });
            await db.SaveChangesAsync(ct);
            Saved = "added";
        }
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(
        Guid id, string? name, decimal price, int durationDays, string? articleId, bool isActive, CancellationToken ct)
    {
        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (plan is not null)
        {
            if (!string.IsNullOrWhiteSpace(name)) plan.Name = name.Trim();
            plan.PriceMinor = ToMinor(price);
            if (durationDays > 0) plan.DurationDays = durationDays;
            plan.ArticleId = Trim(articleId);
            plan.IsActive = isActive;
            plan.Touch();
            await db.SaveChangesAsync(ct);
            Saved = "saved";
        }
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
    {
        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is not null)
        {
            // Soft-delete: keeps referential integrity for cards already issued on this plan.
            plan.IsDeleted = true;
            plan.IsActive = false;
            plan.Touch();
            await db.SaveChangesAsync(ct);
            Saved = "deleted";
        }
        await LoadAsync(ct);
        return Page();
    }

    public static decimal PriceUah(long minor) => minor / 100m;
    private static long ToMinor(decimal uah) => (long)Math.Round(uah * 100m, MidpointRounding.AwayFromZero);
    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
