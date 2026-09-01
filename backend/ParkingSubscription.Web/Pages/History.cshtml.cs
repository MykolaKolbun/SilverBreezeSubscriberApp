using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

// Payment history + fiscal receipts, mirroring the mobile HistoryScreen.
public class HistoryModel(ApiClient api) : PageModel
{
    public List<PaymentDto> Payments { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!api.IsLoggedIn) return RedirectToPage("/Login");
        try
        {
            Payments = (await api.GetPaymentsAsync())
                .OrderByDescending(p => p.UpdatedAt)
                .ToList();
        }
        catch (ApiException ex) { Error = ex.Message; }
        return Page();
    }

    // Receipts need a Bearer token, so they are proxied through these handlers.
    // The document may not exist yet (e.g. the fiscal provider is a stub) — 404, not 500.
    public async Task<IActionResult> OnGetReceiptPngAsync(Guid paymentId)
    {
        if (!api.IsLoggedIn) return Unauthorized();
        try { return File(await api.GetReceiptPngAsync(paymentId), "image/png"); }
        catch (ApiException) { return NotFound(); }
    }

    public async Task<IActionResult> OnGetReceiptPdfAsync(Guid paymentId)
    {
        if (!api.IsLoggedIn) return Unauthorized();
        try { return File(await api.GetReceiptPdfAsync(paymentId), "application/pdf", $"receipt-{paymentId:N}.pdf"); }
        catch (ApiException) { return NotFound(); }
    }
}
