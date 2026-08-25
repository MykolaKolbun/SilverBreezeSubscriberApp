using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.AdminPanel.Pages;

public sealed class LoginModel(AdminPasswordStore passwords) : PageModel
{
    [BindProperty]
    public string Password { get; set; } = "";

    public string? Error { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (await passwords.VerifyAsync(Password, ct))
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, "Адміністратор") };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
            return RedirectToPage("/Clients");
        }

        Error = "Невірний пароль.";
        return Page();
    }
}
