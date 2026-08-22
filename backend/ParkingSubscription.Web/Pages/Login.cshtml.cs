using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

public class LoginModel(ApiClient api) : PageModel
{
    public string? Email { get; private set; }
    public string? Error { get; private set; }
    public bool Confirmed { get; private set; }

    public void OnGet(bool confirmed = false) => Confirmed = confirmed;

    public async Task<IActionResult> OnPostAsync(string email, string password)
    {
        try
        {
            await api.LoginAsync(email, password);
            return RedirectToPage("/Index");
        }
        catch (ApiException ex)
        {
            Email = email;
            Error = ex.Message;
            return Page();
        }
    }
}
