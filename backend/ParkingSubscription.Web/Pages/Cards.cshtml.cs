using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

public class CardsModel(ApiClient api) : PageModel
{
    public List<ParkingCardDto> Cards { get; private set; } = [];
    public string? Error { get; private set; }

    // Wallet passes are still stubs (no real Apple/Google integration). Keep the
    // buttons hidden until each provider is wired up; then flip the matching flag
    // (later: drive it from config) so its button appears on its own.
    public bool AppleWalletEnabled => false;
    public bool GoogleWalletEnabled => false;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!api.IsLoggedIn)
            return RedirectToPage("/Login");

        try
        {
            Cards = (await api.GetMyCardsAsync()).Items;
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
        return Page();
    }

    // The API requires a Bearer token, which an <img> tag can't send —
    // so the QR image and passes are proxied through these page handlers.

    public async Task<IActionResult> OnGetQrAsync(Guid cardId) =>
        api.IsLoggedIn ? File(await api.GetQrPngAsync(cardId), "image/png") : Unauthorized();

    public async Task<IActionResult> OnGetApplePassAsync(Guid cardId) =>
        api.IsLoggedIn
            ? File(await api.GetApplePassAsync(cardId), "application/vnd.apple.pkpass", $"parking-{cardId:N}.pkpass")
            : Unauthorized();

    public async Task<IActionResult> OnGetGoogleAsync(Guid cardId) =>
        api.IsLoggedIn ? Redirect(await api.GetGoogleWalletLinkAsync(cardId)) : Unauthorized();
}
