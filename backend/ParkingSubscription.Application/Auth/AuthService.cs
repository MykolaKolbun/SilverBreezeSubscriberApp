using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Domain.Entities;

namespace ParkingSubscription.Application.Auth;

public sealed record RegisterResult(Guid UserId, Guid CustomerId, string Email, string? DevConfirmationToken);

/// <summary>Configuration for the auth module.</summary>
public sealed class AuthOptions
{
    /// <summary>
    /// When true, confirmation/reset tokens are returned in API responses to ease
    /// local testing (emails are stubbed). MUST be false in production.
    /// </summary>
    public bool ExposeDevTokens { get; set; } = true;
    public int RefreshTokenDays { get; set; } = 14;
    public int ResetTokenHours { get; set; } = 2;
}

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(RegisterRequest req, CancellationToken ct = default);
    Task ConfirmEmailAsync(ConfirmEmailRequest req, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest req, CancellationToken ct = default);
    Task<AuthResult> RefreshAsync(RefreshRequest req, CancellationToken ct = default);
    Task<string?> ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct = default);
    /// <summary>Sends an email OTP for passwordless login (ТЗ §3).</summary>
    Task<EmailCodeResult> RequestEmailCodeAsync(RequestEmailCodeRequest req, CancellationToken ct = default);
    /// <summary>Verifies the OTP, provisioning a Customer+User+account on first login, and issues JWTs.</summary>
    Task<AuthResult> VerifyEmailCodeAsync(VerifyEmailCodeRequest req, CancellationToken ct = default);
}

/// <summary>
/// Email/password auth with JWT (ТЗ §3). Registration atomically provisions a
/// Customer + User 1:1 (B2C model) and links the login account to that User.
/// </summary>
public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService tokens,
    IEmailSender email,
    IClock clock,
    AuthOptions options,
    ILogger<AuthService> logger) : IAuthService
{
    private const int CodeExpiryMinutes = 5;
    private const int ResendCooldownSeconds = 60;
    private const int MaxOtpAttempts = 5;

    public async Task<RegisterResult> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var normalizedEmail = Normalize(req.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail) || !normalizedEmail.Contains('@'))
            throw new ValidationException("A valid email is required.");
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
            throw new ValidationException("Password must be at least 8 characters.");

        if (await db.AppAccounts.AnyAsync(a => a.Email == normalizedEmail, ct))
            throw new ConflictException("An account with this email already exists.");

        // B2C: create Customer 1:1 with User (ТЗ §3, §10.1).
        var customer = new Customer
        {
            Email = normalizedEmail,
            FirstName = req.FirstName,
            Surname = req.Surname
        };
        var user = new User
        {
            Customer = customer,
            Email = normalizedEmail,
            FirstName = req.FirstName,
            Surname = req.Surname
        };
        var token = TokenGenerator.NewToken();
        var account = new AppAccount
        {
            Email = normalizedEmail,
            PasswordHash = hasher.Hash(req.Password),
            EmailConfirmed = false,
            EmailConfirmationToken = token,
            User = user
        };

        db.Customers.Add(customer);
        db.Users.Add(user);
        db.AppAccounts.Add(account);
        await db.SaveChangesAsync(ct);

        await email.SendAsync(normalizedEmail, "Confirm your email",
            $"Your confirmation code is: {token}", ct);
        logger.LogInformation("Registered account {Email}; Customer {CustomerId}/User {UserId} created 1:1",
            normalizedEmail, customer.Id, user.Id);

        return new RegisterResult(user.Id, customer.Id, normalizedEmail,
            options.ExposeDevTokens ? token : null);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest req, CancellationToken ct = default)
    {
        var account = await db.AppAccounts
            .FirstOrDefaultAsync(a => a.Email == Normalize(req.Email), ct)
            ?? throw new NotFoundException("Account not found.");

        if (account.EmailConfirmed)
            return;
        if (string.IsNullOrEmpty(account.EmailConfirmationToken) || account.EmailConfirmationToken != req.Token)
            throw new ValidationException("Invalid confirmation token.");

        account.EmailConfirmed = true;
        account.EmailConfirmationToken = null;
        account.Touch();
        await db.SaveChangesAsync(ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var account = await db.AppAccounts.Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Email == Normalize(req.Email), ct);

        if (account is null || !hasher.Verify(req.Password, account.PasswordHash))
            throw new AuthException("Invalid email or password.");
        if (!account.EmailConfirmed)
            throw new AuthException("Email is not confirmed.");

        return await IssueAsync(account, ct);
    }

    public async Task<AuthResult> RefreshAsync(RefreshRequest req, CancellationToken ct = default)
    {
        var hash = tokens.HashRefreshToken(req.RefreshToken);
        var account = await db.AppAccounts.Include(a => a.User)
            .FirstOrDefaultAsync(a => a.RefreshTokenHash == hash, ct)
            ?? throw new AuthException("Invalid refresh token.");

        if (account.RefreshTokenExpiresAt is null || account.RefreshTokenExpiresAt < clock.UtcNow)
            throw new AuthException("Refresh token expired.");

        return await IssueAsync(account, ct);
    }

    public async Task<string?> ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct = default)
    {
        var account = await db.AppAccounts.FirstOrDefaultAsync(a => a.Email == Normalize(req.Email), ct);
        // Do not reveal whether the email exists.
        if (account is null)
            return null;

        var token = TokenGenerator.NewToken();
        account.PasswordResetToken = token;
        account.PasswordResetTokenExpiresAt = clock.UtcNow.AddHours(options.ResetTokenHours);
        account.Touch();
        await db.SaveChangesAsync(ct);

        await email.SendAsync(account.Email, "Reset your password",
            $"Your password reset code is: {token}", ct);
        return options.ExposeDevTokens ? token : null;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 8)
            throw new ValidationException("Password must be at least 8 characters.");

        var account = await db.AppAccounts.FirstOrDefaultAsync(a => a.Email == Normalize(req.Email), ct)
            ?? throw new NotFoundException("Account not found.");

        if (string.IsNullOrEmpty(account.PasswordResetToken) || account.PasswordResetToken != req.Token
            || account.PasswordResetTokenExpiresAt is null || account.PasswordResetTokenExpiresAt < clock.UtcNow)
            throw new ValidationException("Invalid or expired reset token.");

        account.PasswordHash = hasher.Hash(req.NewPassword);
        account.PasswordResetToken = null;
        account.PasswordResetTokenExpiresAt = null;
        account.RefreshTokenHash = null; // invalidate existing sessions
        account.Touch();
        await db.SaveChangesAsync(ct);
    }

    public async Task<EmailCodeResult> RequestEmailCodeAsync(RequestEmailCodeRequest req, CancellationToken ct = default)
    {
        var addr = Normalize(req.Email);
        if (string.IsNullOrWhiteSpace(addr) || !addr.Contains('@'))
            throw new ValidationException("A valid email is required.");
        var now = clock.UtcNow;

        var otp = await db.LoginOtps.FirstOrDefaultAsync(o => o.Identifier == addr, ct);
        if (otp is not null && now - otp.LastSentAt < TimeSpan.FromSeconds(ResendCooldownSeconds))
            throw new ValidationException("Зачекайте перед повторним надсиланням коду.");

        var code = GenerateCode();
        if (otp is null)
        {
            otp = new LoginOtp { Identifier = addr };
            db.LoginOtps.Add(otp);
        }
        otp.CodeHash = hasher.Hash(code);
        otp.ExpiresAt = now.AddMinutes(CodeExpiryMinutes);
        otp.Attempts = 0;
        otp.LastSentAt = now;
        await db.SaveChangesAsync(ct);

        await email.SendAsync(addr, "SilverBreeze — код для входу",
            $"Ваш код для входу: {code}\n\nКод дійсний {CodeExpiryMinutes} хв.", ct);
        logger.LogInformation("Email OTP requested for {Email}", addr);

        return new EmailCodeResult(addr, options.ExposeDevTokens ? code : null);
    }

    public async Task<AuthResult> VerifyEmailCodeAsync(VerifyEmailCodeRequest req, CancellationToken ct = default)
    {
        var addr = Normalize(req.Email);
        var now = clock.UtcNow;

        var otp = await db.LoginOtps.FirstOrDefaultAsync(o => o.Identifier == addr, ct)
            ?? throw new AuthException("Код не знайдено. Запросіть новий.");
        if (otp.ExpiresAt < now)
            throw new AuthException("Код прострочено. Запросіть новий.");
        if (otp.Attempts >= MaxOtpAttempts)
            throw new AuthException("Забагато спроб. Запросіть новий код.");
        if (!hasher.Verify(req.Code, otp.CodeHash))
        {
            otp.Attempts++;
            await db.SaveChangesAsync(ct);
            throw new AuthException("Невірний код.");
        }

        // Code is valid — consume it and find/provision the account for this email.
        db.LoginOtps.Remove(otp);

        var account = await db.AppAccounts.Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Email == addr, ct);
        if (account is null)
        {
            var customer = new Customer { Email = addr };
            var user = new User { Customer = customer, Email = addr };
            account = new AppAccount { Email = addr, EmailConfirmed = true, User = user };
            db.Customers.Add(customer);
            db.Users.Add(user);
            db.AppAccounts.Add(account);
            logger.LogInformation("Provisioned email account {Email}; Customer/User created 1:1", addr);
        }
        else
        {
            account.EmailConfirmed = true;
        }
        await db.SaveChangesAsync(ct);

        return await IssueAsync(account, ct);
    }

    private static string GenerateCode() =>
        System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private async Task<AuthResult> IssueAsync(AppAccount account, CancellationToken ct)
    {
        var issued = tokens.Issue(account);
        account.RefreshTokenHash = tokens.HashRefreshToken(issued.RefreshToken);
        account.RefreshTokenExpiresAt = clock.UtcNow.AddDays(options.RefreshTokenDays);
        account.Touch();
        await db.SaveChangesAsync(ct);

        return new AuthResult(issued.AccessToken, issued.RefreshToken, issued.AccessTokenExpiresAt,
            account.UserId, account.User!.CustomerId);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
