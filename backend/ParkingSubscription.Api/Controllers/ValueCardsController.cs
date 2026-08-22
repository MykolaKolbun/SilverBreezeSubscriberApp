using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Application.Facade;

namespace ParkingSubscription.Api.Controllers;

[ApiController]
[Route("value-cards")]
[Authorize]
public sealed class ValueCardsController(IValueCardService valueCards) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ValueCardDto>> Create(CreateValueCardRequest req, CancellationToken ct)
    {
        var dto = await valueCards.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ValueCardDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await valueCards.GetAsync(id, ct));

    [HttpGet("changes")]
    public async Task<ActionResult<PagedResult<ValueCardDto>>> Changes([FromQuery] string? pagingToken, CancellationToken ct) =>
        Ok(await valueCards.GetChangesAsync(pagingToken, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await valueCards.DeleteAsync(id, ct);
        return NoContent();
    }
}
