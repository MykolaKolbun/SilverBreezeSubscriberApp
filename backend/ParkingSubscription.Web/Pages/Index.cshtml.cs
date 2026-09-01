using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ParkingSubscription.Web.Pages;

public class IndexModel(ApiClient api) : PageModel
{
    public bool LoggedIn => api.IsLoggedIn;
    public List<PlanDto> Plans { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        if (!LoggedIn)
            return;
        try
        {
            Plans = await api.GetPlansAsync();
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
    }

}
