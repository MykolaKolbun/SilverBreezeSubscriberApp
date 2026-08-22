using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Application.Facade;

namespace ParkingSubscription.Api.Controllers;

[ApiController]
[Route("customers")]
[Authorize]
public sealed class CustomersController(ICustomerService customers) : ControllerBase
{
    /// <summary>Create a customer and related details (ТЗ §4.1).</summary>
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest req, CancellationToken ct)
    {
        var dto = await customers.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    /// <summary>Search by externalContactId and/or searchTerm (name/surname/firstname/email).</summary>
    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<CustomerDto>>> Search(
        [FromQuery] string? externalContactId, [FromQuery] string? searchTerm,
        [FromQuery] string? pagingToken, CancellationToken ct) =>
        Ok(await customers.SearchAsync(externalContactId, searchTerm, pagingToken, ct));

    /// <summary>Paginated changed customers, newest first, page size 50 (ТЗ §4.1, §9).</summary>
    [HttpGet("changes")]
    public async Task<ActionResult<PagedResult<CustomerDto>>> Changes(
        [FromQuery] string? pagingToken, CancellationToken ct) =>
        Ok(await customers.GetChangesAsync(pagingToken, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await customers.GetAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpdateCustomerRequest req, CancellationToken ct) =>
        Ok(await customers.UpdateAsync(id, req, ct));

    /// <summary>Mark deleted; cascade users and set EndDate=today on parking cards (ТЗ §5).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await customers.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/users")]
    public async Task<ActionResult<PagedResult<UserDto>>> Users(Guid id, [FromQuery] string? pagingToken, CancellationToken ct) =>
        Ok(await customers.GetUsersAsync(id, pagingToken, ct));

    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id, CancellationToken ct)
    {
        await customers.BlockAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/unblock")]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken ct)
    {
        await customers.UnblockAsync(id, ct);
        return NoContent();
    }
}
