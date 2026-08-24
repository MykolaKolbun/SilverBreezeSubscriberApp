using Microsoft.AspNetCore.DataProtection;
using ParkingSubscription.Application.Abstractions;

namespace ParkingSubscription.Infrastructure.Security;

/// <summary>
/// Encrypts/decrypts secrets (e.g. the payment SignKey) at rest using ASP.NET Core
/// Data Protection. The protection keys are persisted OUTSIDE the database (a mounted
/// volume in production — see Program.cs), so a leaked DB dump does not expose secrets.
///
/// The purpose chain "SilverBreeze.Credentials.v1" isolates this protector; rotating the
/// chain name invalidates all existing ciphertext.
/// </summary>
public sealed class CredentialProtector : ICredentialProtector
{
    private readonly IDataProtector _protector;

    public CredentialProtector(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector("SilverBreeze.Credentials.v1");

    public string Protect(string plaintext) => _protector.Protect(plaintext);
    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
