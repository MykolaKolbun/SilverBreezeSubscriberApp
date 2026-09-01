using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

// Profile: editable contact details + vehicles, mirroring the mobile ProfileScreen.
public class ProfileModel(ApiClient api) : PageModel
{
    public const int MaxVehicles = 3;

    [BindProperty] public string? FirstName { get; set; }
    [BindProperty] public string? Surname { get; set; }
    [BindProperty] public string? Mobile { get; set; }
    public string? Email { get; private set; }

    public List<ApiVehicle> Vehicles { get; private set; } = [];
    [BindProperty] public string? Plate { get; set; }
    [BindProperty] public string? Make { get; set; }
    [BindProperty] public string? CarModel { get; set; }

    public bool Incomplete { get; private set; }
    public string? Saved { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!api.IsLoggedIn) return RedirectToPage("/Login");
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostContactAsync()
    {
        if (!api.IsLoggedIn) return RedirectToPage("/Login");
        try
        {
            await api.UpdateUserAsync(FirstName?.Trim(), Surname?.Trim(), Mobile?.Trim());
            Saved = "contact";
        }
        catch (ApiException ex) { Error = ex.Message; }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddVehicleAsync()
    {
        if (!api.IsLoggedIn) return RedirectToPage("/Login");
        if (string.IsNullOrWhiteSpace(Plate))
        {
            Error = "Вкажіть номер авто.";
            await LoadAsync();
            return Page();
        }
        try
        {
            await api.CreateVehicleAsync(Plate.Trim().ToUpperInvariant(), Make?.Trim(), CarModel?.Trim());
            Saved = "vehicle";
            Plate = Make = CarModel = null;
        }
        catch (ApiException ex) { Error = ex.Message; }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteVehicleAsync(Guid vehicleId)
    {
        if (!api.IsLoggedIn) return RedirectToPage("/Login");
        try { await api.DeleteVehicleAsync(vehicleId); }
        catch (ApiException ex) { Error = ex.Message; }
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        var user = await api.GetUserAsync();
        Email = user.Email;
        FirstName = user.FirstName;
        Surname = user.Surname;
        Mobile = user.Mobile;
        Vehicles = await api.GetVehiclesAsync();
        Incomplete = string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.Surname);
    }
}
