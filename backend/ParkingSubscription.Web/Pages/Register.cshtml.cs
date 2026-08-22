using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

public class RegisterModel(ApiClient api) : PageModel
{
    public string? Email { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostAsync(string email, string password, string? firstName, string? surname)
    {
        try
        {
            var result = await api.RegisterAsync(email, password, firstName, surname);
            // In dev the API returns the confirmation token, so we can prefill the confirm form.
            return RedirectToPage("/Confirm", new { email = result.Email, token = result.DevConfirmationToken });
        }
        catch (ApiException ex)
        {
            Email = email;
            Error = ex.Message;
            return Page();
        }
    }
}
