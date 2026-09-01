using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

// Passwordless login, mirroring the mobile app: email -> one-time code -> in.
public class LoginModel(ApiClient api, LocService L) : PageModel
{
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string? Code { get; set; }

    public bool CodeSent { get; private set; }
    public string? DevCode { get; private set; }
    public string? Error { get; private set; }

    public IActionResult OnGet()
    {
        if (api.IsLoggedIn)
            return RedirectToPage("/Cards");
        return Page();
    }

    // Step 1 — send a one-time code to the email.
    public async Task<IActionResult> OnPostRequestAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            Error = L["err.needEmail"];
            return Page();
        }
        try
        {
            var result = await api.RequestEmailCodeAsync(Email.Trim());
            CodeSent = true;
            DevCode = result.DevCode; // prefilled only during the test phase
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
        return Page();
    }

    // Step 2 — verify the code and sign in (auto-provisions the account on first login).
    public async Task<IActionResult> OnPostVerifyAsync()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            Error = L["err.needCode"];
            CodeSent = true;
            return Page();
        }
        try
        {
            await api.VerifyEmailCodeAsync(Email.Trim(), Code.Trim());
            // Prompt profile completion on first sign-in, like the app.
            var user = await api.GetUserAsync();
            if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.Surname))
                return RedirectToPage("/Profile");
            return RedirectToPage("/Cards");
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
            CodeSent = true;
            return Page();
        }
    }
}
