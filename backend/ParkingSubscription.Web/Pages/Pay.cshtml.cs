using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

// Payment-result landing. The provider (iPay) redirects the browser here via the API's
// resolve endpoint, which has already confirmed the payment and issued the fiscal receipt
// server-side. This page just reads and displays the authoritative payment status.
public class PayModel(ApiClient api) : PageModel
{
    public Guid PaymentId { get; private set; }
    public PaymentDto? Payment { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid paymentId)
    {
        if (!api.IsLoggedIn)
            return RedirectToPage("/Login");

        PaymentId = paymentId;
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
}
