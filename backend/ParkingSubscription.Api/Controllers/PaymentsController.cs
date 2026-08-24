using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSubscription.Application.Payments;

namespace ParkingSubscription.Api.Controllers;

[ApiController]
[Route("payments")]
public sealed class PaymentsController(
    IPaymentService payments,
    PaymentUrlOptions urls,
    ILogger<PaymentsController> logger) : ControllerBase
{
    /// <summary>Initiate a payment for a subscription plan (ТЗ §6). Returns the hosted-page URL.</summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<InitiatePaymentResult>> Initiate(InitiatePaymentRequest req, CancellationToken ct) =>
        Ok(await payments.InitiateAsync(req, ct));

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await payments.GetAsync(id, ct));

    /// <summary>Rendered fiscal receipt image (PNG) for one of the caller's payments.</summary>
    [HttpGet("{id:guid}/receipt")]
    [Authorize]
    public async Task<IActionResult> Receipt(Guid id, CancellationToken ct)
    {
        var uidClaim = User.FindFirst("uid")?.Value;
        if (!Guid.TryParse(uidClaim, out var userId))
            return Forbid();

        var image = await payments.GetReceiptImageAsync(id, userId, ct);
        if (image is null)
            return NotFound();
        return File(image.Content, image.ContentType);
    }

    [HttpPost("{id:guid}/refund")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> Refund(Guid id, CancellationToken ct) =>
        Ok(await payments.RefundAsync(id, ct));

    /// <summary>
    /// Provider return URLs (iPay good/bad). Two fixed paths so each can be whitelisted in the
    /// iPay merchant cabinet: <c>/payments/resolve/good</c> and <c>/payments/resolve/bad</c>.
    /// Confirms the payment server-side (the provider — not this redirect — is the source of
    /// truth), activates the card on success, then bounces the browser to the app deep link so
    /// the mobile WebBrowser session closes and the app polls the final status. Anonymous —
    /// the unguessable paymentId is the capability.
    /// </summary>
    [HttpGet("resolve/{outcome:regex(^(good|bad)$)}")]
    [AllowAnonymous]
    public async Task<IActionResult> Resolve(string outcome, [FromQuery] Guid paymentId, CancellationToken ct)
    {
        var good = string.Equals(outcome, "good", StringComparison.OrdinalIgnoreCase);
        string status;
        try
        {
            var dto = await payments.ResolveAsync(paymentId, good, ct);
            status = dto.Status;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payment resolve failed for {PaymentId} (outcome {Outcome})", paymentId, outcome);
            status = "Error";
        }

        var sep = urls.AppReturnUrl.Contains('?') ? '&' : '?';
        return Redirect($"{urls.AppReturnUrl}{sep}paymentId={paymentId}&status={status}");
    }

    /// <summary>
    /// Dev/stub webhook for async payment status (succeeded/declined/timeout), used by the
    /// Razor dev UI. The real iPay flow confirms server-side via <c>/payments/resolve</c>.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<ActionResult<PaymentDto>> Webhook(PaymentWebhookRequest req, CancellationToken ct) =>
        Ok(await payments.HandleWebhookAsync(req, ct));
}
