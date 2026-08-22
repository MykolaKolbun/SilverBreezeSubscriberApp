using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Application.Facade;
using ParkingSubscription.Application.Wallet;

namespace ParkingSubscription.Api.Controllers;

[ApiController]
[Route("parking-cards")]
[Authorize]
public sealed class ParkingCardsController(IParkingCardService cards, IWalletAppService wallet) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ParkingCardDto>> Create(CreateParkingCardRequest req, CancellationToken ct)
    {
        var dto = await cards.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<ParkingCardDto>>> Search(
        [FromQuery] string? externalCardId, [FromQuery] string? searchTerm,
        [FromQuery] string? pagingToken, CancellationToken ct) =>
        Ok(await cards.SearchAsync(externalCardId, searchTerm, pagingToken, ct));

    [HttpGet("changes")]
    public async Task<ActionResult<PagedResult<ParkingCardDto>>> Changes([FromQuery] string? pagingToken, CancellationToken ct) =>
        Ok(await cards.GetChangesAsync(pagingToken, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ParkingCardDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await cards.GetAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ParkingCardDto>> Update(Guid id, UpdateParkingCardRequest req, CancellationToken ct) =>
        Ok(await cards.UpdateAsync(id, req, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await cards.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/anonymize")]
    public async Task<IActionResult> Anonymize(Guid id, CancellationToken ct)
    {
        await cards.AnonymizeAsync(id, ct);
        return Accepted();
    }

    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id, CancellationToken ct)
    {
        await cards.BlockAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/unblock")]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken ct)
    {
        await cards.UnblockAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
    {
        await cards.SuspendAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> Resume(Guid id, CancellationToken ct)
    {
        await cards.ResumeAsync(id, ct);
        return NoContent();
    }

    // ---- Wallet & QR (ТЗ §7) ----

    /// <summary>PNG QR code for the card.</summary>
    [HttpGet("{id:guid}/qr")]
    public async Task<IActionResult> Qr(Guid id, CancellationToken ct) =>
        File(await wallet.GetQrPngAsync(id, ct), "image/png");

    /// <summary>Apple Wallet (.pkpass) pass (stub payload).</summary>
    [HttpGet("{id:guid}/wallet/apple")]
    public async Task<IActionResult> ApplePass(Guid id, CancellationToken ct)
    {
        var pass = await wallet.GetApplePassAsync(id, ct);
        return File(pass.Content, pass.ContentType, pass.FileName);
    }

    /// <summary>Google Wallet save link (stub).</summary>
    [HttpGet("{id:guid}/wallet/google")]
    public async Task<IActionResult> GooglePass(Guid id, CancellationToken ct) =>
        Ok(new { saveUrl = await wallet.GetGoogleWalletLinkAsync(id, ct) });
}
