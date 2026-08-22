using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

public class ConfirmModel(ApiClient api) : PageModel
{
    public string? Email { get; private set; }
    public string? Token { get; private set; }
    public string? Error { get; private set; }

    public void OnGet(string? email, string? token)
    {
        Email = email;
        Token = token;
    }

    public async Task<IActionResult> OnPostAsync(string email, string token)
    {
        try
        {
            await api.ConfirmEmailAsync(email, token);
            return RedirectToPage("/Login", new { confirmed = true });
        }
        catch (ApiException ex)
        {
            Email = email;
            Token = token;
            Error = ex.Message;
            return Page();
        }
    }
}
