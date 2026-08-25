using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSubscription.Application.Auth;

namespace ParkingSubscription.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Register with email + password. Provisions Customer + User 1:1 (ТЗ §3).</summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResult>> Register(RegisterRequest req, CancellationToken ct) =>
        Ok(await auth.RegisterAsync(req, ct));

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest req, CancellationToken ct)
    {
        await auth.ConfirmEmailAsync(req, ct);
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(LoginRequest req, CancellationToken ct) =>
        Ok(await auth.LoginAsync(req, ct));

    /// <summary>Request an email OTP for passwordless login (ТЗ §3).</summary>
    [HttpPost("email/request-code")]
    public async Task<IActionResult> RequestEmailCode(RequestEmailCodeRequest req, CancellationToken ct)
    {
        var result = await auth.RequestEmailCodeAsync(req, ct);
        // devCode is populated only when Auth:ExposeDevTokens is on (email is stubbed).
        return Ok(new { result.Email, devCode = result.DevCode });
    }

    /// <summary>Verify the OTP; provisions the account on first login and returns JWTs.</summary>
    [HttpPost("email/verify")]
    public async Task<ActionResult<AuthResult>> VerifyEmailCode(VerifyEmailCodeRequest req, CancellationToken ct) =>
        Ok(await auth.VerifyEmailCodeAsync(req, ct));

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResult>> Refresh(RefreshRequest req, CancellationToken ct) =>
        Ok(await auth.RefreshAsync(req, ct));

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req, CancellationToken ct)
    {
        var devToken = await auth.ForgotPasswordAsync(req, ct);
        // Always 200 to avoid leaking whether the email exists; devToken populated only in dev.
        return Ok(new { message = "If the email exists, a reset code has been sent.", devToken });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest req, CancellationToken ct)
    {
        await auth.ResetPasswordAsync(req, ct);
        return NoContent();
    }
}
