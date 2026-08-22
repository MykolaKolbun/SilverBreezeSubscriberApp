namespace ParkingSubscription.Application.Common;

/// <summary>Base for domain/application errors mapped to HTTP ProblemDetails in the API.</summary>
public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
    public abstract string ErrorCode { get; }
}

/// <summary>Requested entity does not exist (HTTP 404).</summary>
public sealed class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => 404;
    public override string ErrorCode => "not_found";
}

/// <summary>Request is invalid (HTTP 400).</summary>
public sealed class ValidationException(string message) : AppException(message)
{
    public override int StatusCode => 400;
    public override string ErrorCode => "validation_error";
}

/// <summary>Business-rule conflict, e.g. an active card already exists (HTTP 409).</summary>
public sealed class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => 409;
    public override string ErrorCode => "conflict";
}

/// <summary>Authentication/credentials failure (HTTP 401).</summary>
public sealed class AuthException(string message) : AppException(message)
{
    public override int StatusCode => 401;
    public override string ErrorCode => "unauthorized";
}
