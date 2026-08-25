namespace ParkingSubscription.Application.Auth;

public sealed record RegisterRequest(string Email, string Password, string? FirstName, string? Surname);
public sealed record ConfirmEmailRequest(string Email, string Token);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

// Passwordless phone login (OTP).
public sealed record RequestPhoneCodeRequest(string Phone);
public sealed record PhoneCodeResult(string Phone, string? DevCode);
public sealed record VerifyPhoneCodeRequest(string Phone, string Code);

public sealed record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    Guid UserId,
    Guid CustomerId);
