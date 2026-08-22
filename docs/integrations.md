# Integrations & Ports

All external dependencies are expressed as **ports** — interfaces declared in `Application/Abstractions/Ports.cs` (plus `IAppDbContext`). Concrete implementations live in Infrastructure. Today every external integration is a **stub** that logs its action and returns synthetic data, so the domain logic runs end-to-end without real credentials. Swapping in a real client means implementing the same interface and re-registering it in `Infrastructure/DependencyInjection.cs` — no changes to Application or Domain.

## Port → implementation map

| Port (interface) | Purpose | Current implementation | Lifetime |
|------------------|---------|------------------------|----------|
| `IAppDbContext` | Persistence (DbSets + SaveChanges) | `AppDbContext` (EF Core) | Scoped |
| `IClock` | Abstracted current time | `SystemClock` | Singleton |
| `IPasswordHasher` | Password hash/verify | `BcryptPasswordHasher` | Singleton |
| `IJwtTokenService` | Issue/validate JWT + refresh tokens | `JwtTokenService` | Singleton |
| `IParkingLogicClient` | Propagate changes to Parking.Logic | `ParkingLogicClientStub` | Scoped |
| `IPaymentProvider` | Create/refund payments | `PaymentProviderStub` | Scoped |
| `IFiscalProvider` | Fiscalize receipts | `FiscalProviderStub` | Scoped |
| `IWalletPassService` | Apple/Google wallet passes | `WalletPassServiceStub` | Scoped |
| `IQrCodeGenerator` | QR PNG generation | `QrCodeGenerator` (QRCoder) | Singleton |
| `IEmailSender` | Transactional email | `LoggingEmailSender` | Scoped |
| `IPushSender` | Push notifications | `LoggingPushSender` | Scoped |

## Real vs. stubbed

**Real implementations** (production-ready as-is):
- `SystemClock`, `BcryptPasswordHasher`, `JwtTokenService`, `QrCodeGenerator`, and the EF Core `AppDbContext`.

**Stubs** (replace before production):
- `ParkingLogicClientStub` — logs the propagation and returns a synthetic external id (`PL-<Kind>-<id>`). Replace with a real HTTP client once the Parking.Logic spec is available.
- `PaymentProviderStub` — returns a synthetic `PaymentIntent`; real settlement is driven by the webhook. Swap for Stripe / LiqPay / Fondy.
- `FiscalProviderStub` — returns a synthetic fiscal receipt id/URL. Swap for the real fiscalization provider.
- `WalletPassServiceStub` — emits a placeholder `pass.json`-style payload and a stub Google Wallet link; `PushPassUpdateAsync` just logs. Replace with Apple PassKit signing and the Google Wallet API.
- `LoggingEmailSender` / `LoggingPushSender` — log the message. Swap for an SMTP/ESP and FCM/APNs.

## Port contracts

```csharp
// Parking.Logic facade — returns external ids; never throws so local state stays canonical.
Task<string> PropagateAsync(EntityKind kind, Guid entityId,
                            PropagationOperation op, string? payloadJson, CancellationToken ct);

// Payment provider
Task<PaymentIntent> CreatePaymentAsync(long amountMinor, string currency, string reference, CancellationToken ct);
Task RefundAsync(string providerPaymentId, CancellationToken ct);

// Fiscalization
Task<FiscalReceipt> FiscalizeAsync(Payment payment, CancellationToken ct);

// Wallet passes
WalletPass BuildApplePass(ParkingCard card);
string BuildGoogleWalletLink(ParkingCard card);
Task PushPassUpdateAsync(ParkingCard card, CancellationToken ct);

// QR
byte[] GeneratePng(string payload);

// Notifications
Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);          // email
Task SendAsync(Guid userId, string title, string body, CancellationToken ct);               // push
```

Supporting records: `PaymentIntent(ProviderPaymentId, ClientSecret)`, `FiscalReceipt(ReceiptId, Url)`, `WalletPass(Content, ContentType, FileName)`, `AccessTokens(AccessToken, RefreshToken, AccessTokenExpiresAt)`.

## How to replace a stub

1. Implement the interface in Infrastructure (e.g. `StripePaymentProvider : IPaymentProvider`).
2. Register it in `Infrastructure/DependencyInjection.cs`, replacing the stub line:
   ```csharp
   services.AddScoped<IPaymentProvider, StripePaymentProvider>();
   ```
3. Add any required config (keys, endpoints) to `appsettings` and bind an options class.
4. For the payment webhook specifically, add **provider signature verification** in `PaymentsController.Webhook` (currently `[AllowAnonymous]`).

Because Application and Domain depend only on the interface, no other code changes.
