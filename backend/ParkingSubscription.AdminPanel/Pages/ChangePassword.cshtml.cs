using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.AdminPanel.Pages;

public sealed class ChangePasswordModel(AdminPasswordStore passwords) : PageModel
{
    [BindProperty] public string Current { get; set; } = "";
    [BindProperty] public string NewPassword { get; set; } = "";
    [BindProperty] public string Confirm { get; set; } = "";

    public string? Message { get; private set; }
    public string? Error { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!await passwords.VerifyAsync(Current, ct))
            Error = "Поточний пароль невірний.";
        else if (NewPassword.Length < 6)
            Error = "Новий пароль закороткий (мінімум 6 символів).";
        else if (NewPassword != Confirm)
            Error = "Паролі не збігаються.";
        else
        {
            await passwords.ChangeAsync(NewPassword, ct);
            Message = "Пароль змінено.";
            Current = NewPassword = Confirm = "";
        }
        return Page();
    }
}
