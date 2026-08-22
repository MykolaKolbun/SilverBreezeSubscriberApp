namespace ParkingSubscription.Application.Auth;

public sealed record RegisterRequest(string Email, string Password, string? FirstName, string? Surname);
public sealed record ConfirmEmailRequest(string Email, string Token);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    Guid UserId,
    Guid CustomerId);
