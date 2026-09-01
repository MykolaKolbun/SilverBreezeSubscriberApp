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

    /// <summary>The validity window for a plan starting on a given date — the backend's
    /// authoritative date math, so clients display it instead of re-deriving it.</summary>
    [HttpGet("{id:guid}/period")]
    public async Task<ActionResult<PlanPeriodDto>> Period(Guid id, [FromQuery] DateOnly start, CancellationToken ct)
    {
        var period = await plans.GetPeriodAsync(id, start, ct);
        return period is null ? NotFound() : Ok(period);
    }
}
