using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSubscription.Application.Facade;

namespace ParkingSubscription.Api.Controllers;

[ApiController]
[Route("vehicles")]
[Authorize]
public sealed class VehiclesController(IVehicleService vehicles) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create(CreateVehicleRequest req, CancellationToken ct)
    {
        var dto = await vehicles.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await vehicles.GetAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleDto>> Update(Guid id, UpdateVehicleRequest req, CancellationToken ct) =>
        Ok(await vehicles.UpdateAsync(id, req, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await vehicles.DeleteAsync(id, ct);
        return NoContent();
    }
}
