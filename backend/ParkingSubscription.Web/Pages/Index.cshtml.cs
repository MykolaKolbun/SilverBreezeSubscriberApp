using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

public class IndexModel(ApiClient api) : PageModel
{
    public bool LoggedIn => api.IsLoggedIn;
    public List<PlanDto> Plans { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        if (!LoggedIn)
            return;
        try
        {
            Plans = await api.GetPlansAsync();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

    /// <summary>Buy: create the payment, then go to the payment page.</summary>
    public async Task<IActionResult> OnPostAsync(Guid planId)
    {
        if (!LoggedIn)
            return RedirectToPage("/Login");
        try
        {
            var payment = await api.InitiatePaymentAsync(planId);
            // Send the browser to the provider's hosted page (iPay). After payment the API's
            // resolve endpoint confirms + fiscalizes server-side and returns to /Pay.
            return Redirect(payment.RedirectUrl);
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }
}
