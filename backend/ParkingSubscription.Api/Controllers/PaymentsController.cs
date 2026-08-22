using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSubscription.Application.Payments;

namespace ParkingSubscription.Api.Controllers;

[ApiController]
[Route("payments")]
public sealed class PaymentsController(IPaymentService payments) : ControllerBase
{
    /// <summary>Initiate a payment for a subscription plan (ТЗ §6).</summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<InitiatePaymentResult>> Initiate(InitiatePaymentRequest req, CancellationToken ct) =>
        Ok(await payments.InitiateAsync(req, ct));

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await payments.GetAsync(id, ct));

    [HttpPost("{id:guid}/refund")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> Refund(Guid id, CancellationToken ct) =>
        Ok(await payments.RefundAsync(id, ct));

    /// <summary>
    /// Provider webhook for async payment status (succeeded/declined/timeout).
    /// On success the parking card is activated and the receipt fiscalized (ТЗ §6).
    /// Anonymous: authenticated in production via provider signature verification.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<ActionResult<PaymentDto>> Webhook(PaymentWebhookRequest req, CancellationToken ct) =>
        Ok(await payments.HandleWebhookAsync(req, ct));
}
