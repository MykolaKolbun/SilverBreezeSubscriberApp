using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSubscription.Application.Facade;

namespace ParkingSubscription.Api.Controllers;

[ApiController]
[Route("plans")]
[Authorize]
public sealed class PlansController(IPlanService plans) : ControllerBase
{
    /// <summary>List active subscription tariffs to choose from (ТЗ §2.3).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> Get(CancellationToken ct) =>
        Ok(await plans.GetActiveAsync(ct));
}
