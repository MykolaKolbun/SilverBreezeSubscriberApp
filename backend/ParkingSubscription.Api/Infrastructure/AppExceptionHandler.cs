using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ParkingSubscription.Application.Common;

namespace ParkingSubscription.Api.Infrastructure;

/// <summary>
/// Maps <see cref="AppException"/> to RFC7807 ProblemDetails; everything else
/// becomes a 500 without leaking internals.
/// </summary>
public sealed class AppExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception exception, CancellationToken ct)
    {
        var (status, title, code) = exception switch
        {
            AppException app => (app.StatusCode, app.Message, app.ErrorCode),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "internal_error")
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception");

        ctx.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = code
            }
        });
    }
}
