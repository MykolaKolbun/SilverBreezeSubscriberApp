# Architecture

The backend follows **Clean Architecture**. Dependencies point inward: outer layers depend on inner ones, never the reverse. Business logic is isolated from frameworks and external services through interfaces ("ports").

```
┌───────────────────────────────────────────────────────────────┐
│  Api               Controllers, DI wiring, JWT auth, OpenAPI,   │
│                    global exception handler                     │
├───────────────────────────────────────────────────────────────┤
│  Infrastructure    EF Core DbContext + migrations, port stubs,  │
│                    JWT/BCrypt, background workers                │
├───────────────────────────────────────────────────────────────┤
│  Application       Services (use cases), DTOs, port interfaces, │
│                    pagination, exceptions, change propagation    │
├───────────────────────────────────────────────────────────────┤
│  Domain            Entities, enums, invariants (no dependencies)│
└───────────────────────────────────────────────────────────────┘
```

Reference direction: **Api → Application + Infrastructure → Application → Domain**. Infrastructure and Application both reference Domain; Api composes everything at startup.

## Projects

### `ParkingSubscription.Domain`
Pure business model with no external dependencies.

- **`Common/Entity.cs`** — base class for all persisted entities. Provides `Guid Id`, `CreatedAt`/`UpdatedAt` timestamps, an `IsDeleted` soft-delete flag, and `Touch()` to bump `UpdatedAt`.
- **`Entities/`** — `Customer`, `User`, `ParkingCard`, `ValueCard`, `AppAccount`, `Payment`, `SubscriptionPlan`, `OutboxMessage`, `AuditLogEntry`.
- **`Enums/Enums.cs`** — `CardStatus`, `AnonymizationState`, `PaymentStatus`, `OutboxStatus`, `EntityKind`, `PropagationOperation`.
- Some invariants live on the entities themselves, e.g. `ParkingCard.IsActiveOn(date)` and `ParkingCard.OverlapsPeriod(start, end)`.

See [Domain Model](domain-model.md) for details.

### `ParkingSubscription.Application`
Use cases and orchestration. Depends only on Domain.

- **`Abstractions/`** — the **ports**: `IAppDbContext`, `IClock`, `IPasswordHasher`, `IJwtTokenService`, `IParkingLogicClient`, `IPaymentProvider`, `IFiscalProvider`, `IWalletPassService`, `IQrCodeGenerator`, `IEmailSender`, `IPushSender`. These decouple business logic from concrete infrastructure.
- **`Auth/`** — `AuthService` (register, confirm, login, refresh, forgot/reset password) and its DTOs.
- **`Facade/`** — `CustomerService`, `UserService`, `ParkingCardService`, `ValueCardService`, `PlanService` plus shared `Mapping` (entity → DTO) and `FacadeDtos`.
- **`Payments/`** — `PaymentService` orchestrating payment → card activation → fiscalization.
- **`Wallet/`** — `WalletAppService` producing QR images and wallet passes.
- **`Common/`** — `ChangePropagator` (outbox + audit), pagination (`Paging`, `CursorPagination`, `QueryablePaging`), and the `AppException` hierarchy.

Services are registered in `Application/DependencyInjection.cs` via `AddApplication()`.

### `ParkingSubscription.Infrastructure`
Concrete implementations of the ports. Depends on Application (for the interfaces) and Domain.

- **`Persistence/`** — `AppDbContext` (implements `IAppDbContext`), EF migrations, `DbSeeder` (baseline subscription plans).
- **`Auth/`** — `BcryptPasswordHasher`, `JwtTokenService`, `JwtOptions`.
- **`Time/SystemClock.cs`** — real `IClock`.
- **`ParkingLogic/`**, **`Payments/`**, **`Wallet/`**, **`Notifications/`** — **stub** implementations of the external-integration ports (see [Integrations](integrations.md)).
- **`BackgroundServices/`** — `OutboxPropagationService` and `AnonymizationWorker` hosted services.

Everything is wired in `Infrastructure/DependencyInjection.cs` via `AddInfrastructure(configuration)`.

### `ParkingSubscription.Api`
The HTTP host and composition root.

- **`Controllers/`** — thin controllers delegating to Application services.
- **`Infrastructure/AppExceptionHandler.cs`** — maps `AppException` to RFC 7807 ProblemDetails.
- **`Program.cs`** — configures Serilog, controllers with enum-as-string JSON, ProblemDetails, OpenAPI/Scalar, JWT bearer auth, localization (uk/en), then applies migrations and seeds data at startup.

### `ParkingSubscription.Tests`
xUnit tests using `WebApplicationFactory` against an isolated SQLite database. Covers the end-to-end flow, the one-active-card rule, block auditing, authorization, and pagination.

## Cross-cutting concerns

### Error handling
Application code throws typed `AppException` subclasses; `AppExceptionHandler` translates each to a ProblemDetails response with a stable status code and error code:

| Exception | HTTP | `type` code |
|-----------|------|-------------|
| `ValidationException` | 400 | `validation_error` |
| `AuthException` | 401 | `unauthorized` |
| `NotFoundException` | 404 | `not_found` |
| `ConflictException` | 409 | `conflict` |
| (anything else) | 500 | `internal_error` |

Only 5xx responses log the full exception; 4xx are returned without leaking internals.

### Persistence abstraction
Services depend on `IAppDbContext` (a set of `DbSet<>` + `SaveChangesAsync`), not the concrete `AppDbContext`. This keeps the Application layer free of a hard EF Core dependency and makes services straightforward to test.

### Unit of work
Each service method builds up changes (including outbox and audit records via `ChangePropagator`) and commits them in a **single `SaveChangesAsync`**, so state changes and their propagation records are persisted atomically.

### Time
All time-dependent logic uses `IClock` (`SystemClock` in production, a fixed clock in tests) for deterministic behavior.

### Logging & audit
Serilog writes structured logs to the console. Sensitive operations (block/unblock/suspend/resume/delete/anonymize) additionally write an `AuditLogEntry` row via `ChangePropagator`.

### SQLite `DateTimeOffset` handling
SQLite cannot order by or compare `DateTimeOffset`. `AppDbContext.OnModelCreating` installs a value converter storing every `DateTimeOffset` as UTC ticks (`INTEGER`) when running on SQLite, so ordering by `UpdatedAt`/`CreatedAt` (used by pagination) works correctly.
