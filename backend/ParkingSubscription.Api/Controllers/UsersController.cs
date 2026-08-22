using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Application.Facade;

namespace ParkingSubscription.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public sealed class UsersController(IUserService users) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest req, CancellationToken ct)
    {
        var dto = await users.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<UserDto>>> Search(
        [FromQuery] string? externalContactId, [FromQuery] string? searchTerm,
        [FromQuery] string? pagingToken, CancellationToken ct) =>
        Ok(await users.SearchAsync(externalContactId, searchTerm, pagingToken, ct));

    [HttpGet("changes")]
    public async Task<ActionResult<PagedResult<UserDto>>> Changes([FromQuery] string? pagingToken, CancellationToken ct) =>
        Ok(await users.GetChangesAsync(pagingToken, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await users.GetAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserRequest req, CancellationToken ct) =>
        Ok(await users.UpdateAsync(id, req, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await users.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Mark for deferred anonymization (ТЗ §4.2, §5).</summary>
    [HttpPost("{id:guid}/anonymize")]
    public async Task<IActionResult> Anonymize(Guid id, CancellationToken ct)
    {
        await users.AnonymizeAsync(id, ct);
        return Accepted();
    }

    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id, CancellationToken ct)
    {
        await users.BlockAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/unblock")]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken ct)
    {
        await users.UnblockAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
    {
        await users.SuspendAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> Resume(Guid id, CancellationToken ct)
    {
        await users.ResumeAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/parking-cards")]
    public async Task<ActionResult<PagedResult<ParkingCardDto>>> ParkingCards(Guid id, [FromQuery] string? pagingToken, CancellationToken ct) =>
        Ok(await users.GetParkingCardsAsync(id, pagingToken, ct));

    [HttpGet("{id:guid}/value-cards")]
    public async Task<ActionResult<PagedResult<ValueCardDto>>> ValueCards(Guid id, [FromQuery] string? pagingToken, CancellationToken ct) =>
        Ok(await users.GetValueCardsAsync(id, pagingToken, ct));
}
