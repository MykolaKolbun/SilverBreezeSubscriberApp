using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Application.Facade;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Application.Payments;

public interface IPaymentService
{
    Task<InitiatePaymentResult> InitiateAsync(InitiatePaymentRequest req, CancellationToken ct = default);
    Task<PaymentDto> HandleWebhookAsync(PaymentWebhookRequest req, CancellationToken ct = default);
    Task<PaymentDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PaymentDto> RefundAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Orchestrates payment + fiscalization (ТЗ §6). On a successful payment the
/// parking card is created/activated (enforcing the one-active-card rule via
/// <see cref="IParkingCardService"/>), the receipt is fiscalized, and the user
/// is notified.
/// </summary>
public sealed class PaymentService(
    IAppDbContext db,
    IPaymentProvider provider,
    IFiscalProvider fiscal,
    IParkingCardService parkingCards,
    IPushSender push,
    IClock clock,
    ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<InitiatePaymentResult> InitiateAsync(InitiatePaymentRequest req, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId && !u.IsDeleted, ct)
            ?? throw new NotFoundException($"User {req.UserId} not found.");

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == req.SubscriptionPlanId && p.IsActive, ct)
            ?? throw new NotFoundException($"Subscription plan {req.SubscriptionPlanId} not found.");

        var payment = new Payment
        {
            UserId = user.Id,
            SubscriptionPlanId = plan.Id,
            AmountMinor = plan.PriceMinor,
            Currency = plan.Currency,
            Status = PaymentStatus.Pending
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        var intent = await provider.CreatePaymentAsync(plan.PriceMinor, plan.Currency, payment.Id.ToString(), ct);
        payment.ProviderPaymentId = intent.ProviderPaymentId;
        payment.Touch();
        await db.SaveChangesAsync(ct);

        return new InitiatePaymentResult(payment.Id, intent.ProviderPaymentId, intent.ClientSecret, plan.PriceMinor, plan.Currency);
    }

    public async Task<PaymentDto> HandleWebhookAsync(PaymentWebhookRequest req, CancellationToken ct = default)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == req.ProviderPaymentId, ct)
            ?? throw new NotFoundException($"Payment for provider id {req.ProviderPaymentId} not found.");

        // Idempotency: ignore repeated terminal-state callbacks.
        if (payment.Status is not PaymentStatus.Pending)
        {
            logger.LogInformation("Ignoring webhook for payment {PaymentId} already in state {Status}", payment.Id, payment.Status);
            return ToDto(payment);
        }

        switch (req.Status.Trim().ToLowerInvariant())
        {
            case "succeeded":
                await OnSucceededAsync(payment, ct);
                break;
            case "declined":
                payment.Status = PaymentStatus.Declined;
                payment.FailureReason = "Declined by provider.";
                break;
            case "timeout":
            case "timedout":
                payment.Status = PaymentStatus.TimedOut;
                payment.FailureReason = "Payment timed out.";
                break;
            default:
                throw new ValidationException($"Unknown payment status '{req.Status}'.");
        }

        payment.Touch();
        await db.SaveChangesAsync(ct);

        if (payment.Status != PaymentStatus.Succeeded)
            await push.SendAsync(payment.UserId, "Payment failed", $"Your payment {payment.Id} was not completed.", ct);

        return ToDto(payment);
    }

    public async Task<PaymentDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException($"Payment {id} not found.");
        return ToDto(payment);
    }

    public async Task<PaymentDto> RefundAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException($"Payment {id} not found.");
        if (payment.Status != PaymentStatus.Succeeded)
            throw new ConflictException("Only succeeded payments can be refunded.");

        if (payment.ProviderPaymentId is not null)
            await provider.RefundAsync(payment.ProviderPaymentId, ct);

        payment.Status = PaymentStatus.Refunded;
        payment.Touch();

        // Deactivate the associated card on refund (ТЗ §6).
        if (payment.ParkingCardId is Guid cardId)
            await parkingCards.DeleteAsync(cardId, ct);

        await db.SaveChangesAsync(ct);
        await push.SendAsync(payment.UserId, "Refund processed", $"Payment {payment.Id} was refunded.", ct);
        return ToDto(payment);
    }

    private async Task OnSucceededAsync(Payment payment, CancellationToken ct)
    {
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Id == payment.SubscriptionPlanId, ct);

        // Create + activate the parking card (enforces one-active-card-per-period rule).
        var start = clock.Today;
        var end = start.AddDays(Math.Max(1, plan.DurationDays) - 1);
        var card = await parkingCards.CreateAsync(
            new CreateParkingCardRequest(payment.UserId, plan.Id, start, end, null), ct);

        payment.ParkingCardId = card.Id;
        payment.Status = PaymentStatus.Succeeded;

        // Fiscalize the receipt after successful payment (ТЗ §6).
        var receipt = await fiscal.FiscalizeAsync(payment, ct);
        payment.FiscalReceiptId = receipt.ReceiptId;

        await push.SendAsync(payment.UserId, "Payment successful",
            $"Your parking card is active until {end:yyyy-MM-dd}.", ct);
        logger.LogInformation("Payment {PaymentId} succeeded; card {CardId} activated, receipt {ReceiptId}",
            payment.Id, card.Id, receipt.ReceiptId);
    }

    private static PaymentDto ToDto(Payment p) => new(
        p.Id, p.UserId, p.SubscriptionPlanId, p.ParkingCardId, p.AmountMinor, p.Currency,
        p.Status.ToString(), p.FiscalReceiptId, p.FailureReason, p.UpdatedAt);
}
