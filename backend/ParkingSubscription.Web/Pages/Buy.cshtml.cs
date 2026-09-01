using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

// Confirm a plan and pick the start date before paying. A new subscription may not
// start earlier than the day after the latest active card ends (no overlap) — the
// same stacking floor the API enforces server-side.
public class BuyModel(ApiClient api) : PageModel
{
    public Guid PlanId { get; private set; }
    public PlanDto? Plan { get; private set; }
    public DateOnly MinStart { get; private set; }
    [BindProperty] public DateOnly StartDate { get; set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid planId)
    {
        if (!api.IsLoggedIn)
            return RedirectToPage("/Login");

        var setup = await LoadAsync(planId);
        if (setup is not null)
            return setup;

        StartDate = MinStart;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid planId)
    {
        if (!api.IsLoggedIn)
            return RedirectToPage("/Login");

        var setup = await LoadAsync(planId);
        if (setup is not null)
            return setup;

        // Never let the browser undercut the no-overlap floor.
        if (StartDate < MinStart)
            StartDate = MinStart;

        try
        {
            var payment = await api.InitiatePaymentAsync(planId, StartDate);
            return Redirect(payment.RedirectUrl);
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
            return Page();
        }
    }

    // Loads the plan and computes the earliest allowed start; returns a redirect only
    // when the plan is missing, otherwise null (caller continues rendering).
    private async Task<IActionResult?> LoadAsync(Guid planId)
    {
        PlanId = planId;

        // Gate: a complete profile (name + surname) is required before buying.
        var user = await api.GetUserAsync();
        if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.Surname))
            return RedirectToPage("/Profile");

        var plans = await api.GetPlansAsync();
        Plan = plans.FirstOrDefault(p => p.Id == planId);
        if (Plan is null)
            return RedirectToPage("/Index");

        var today = DateOnly.FromDateTime(DateTime.Now);
        var cards = (await api.GetMyCardsAsync()).Items;
        var latestActiveEnd = cards
            .Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .Select(c => (DateOnly?)c.EndDate)
            .DefaultIfEmpty(null)
            .Max();
        MinStart = latestActiveEnd is DateOnly le && le >= today ? le.AddDays(1) : today;
        return null;
    }
}
