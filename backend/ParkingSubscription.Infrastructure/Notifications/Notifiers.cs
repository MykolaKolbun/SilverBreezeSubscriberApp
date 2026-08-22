using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;

namespace ParkingSubscription.Infrastructure.Notifications;

/// <summary>Stub email sender that logs the message (ТЗ §3, §9). Swap for SMTP/ESP.</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation("EMAIL -> {To} | {Subject} | {Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}

/// <summary>Stub push sender that logs the notification (ТЗ §9). Swap for FCM/APNs.</summary>
public sealed class LoggingPushSender(ILogger<LoggingPushSender> logger) : IPushSender
{
    public Task SendAsync(Guid userId, string title, string body, CancellationToken ct = default)
    {
        logger.LogInformation("PUSH -> user {UserId} | {Title} | {Body}", userId, title, body);
        return Task.CompletedTask;
    }
}
