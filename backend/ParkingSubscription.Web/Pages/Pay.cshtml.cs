using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

public class PayModel(ApiClient api) : PageModel
{
    public Guid PaymentId { get; private set; }
    public string? Provider { get; private set; }
    public PaymentDto? Payment { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid paymentId, string? provider)
    {
        if (!api.IsLoggedIn)
            return RedirectToPage("/Login");

        PaymentId = paymentId;
        Provider = provider;
        try
        {
            Payment = await api.GetPaymentAsync(paymentId);
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
        return Page();
    }

    /// <summary>Dev-only: act as the payment provider and fire the webhook.</summary>
    public async Task<IActionResult> OnPostAsync(Guid paymentId, string provider, string outcome)
    {
        if (!api.IsLoggedIn)
            return RedirectToPage("/Login");

        try
        {
            await api.SimulateProviderCallbackAsync(provider, outcome);
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
        return await OnGetAsync(paymentId, provider);
    }
}
