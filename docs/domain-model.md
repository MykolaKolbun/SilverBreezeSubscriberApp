# Domain Model

All entities derive from `Entity` (`Domain/Common/Entity.cs`):

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `Guid` | Generated client-side (`Guid.NewGuid()`) |
| `CreatedAt` | `DateTimeOffset` | Set on construction (UTC) |
| `UpdatedAt` | `DateTimeOffset` | Bumped via `Touch()`; drives pagination ordering |
| `IsDeleted` | `bool` | Soft-delete flag |

## Entity relationships

```
Customer 1 ────── * User 1 ────── * ParkingCard * ────── 1 SubscriptionPlan
                    │
                    └──────────── * ValueCard

AppAccount 1 ────── 1 User        (login account linked to a parking user)
Payment    * ────── 1 User
Payment    * ────── 1 SubscriptionPlan
Payment    0..1 ─── 1 ParkingCard (set once the payment succeeds)

OutboxMessage / AuditLogEntry     (reference any entity via EntityKind + EntityId)
```

## Entities

### Customer
A parking-system client. In the B2C flow a `Customer` is created **1:1** with a single `User` at registration, but the model supports **1:many** for future B2B use.

| Field | Type | Notes |
|-------|------|-------|
| `ExternalContactId` | `string?` | Correlation id in an external CRM/contact system (searchable) |
| `Name`, `Surname`, `FirstName`, `Email` | `string?` | Contact details |
| `IsBlocked` | `bool` | Block flag |
| `Users` | `ICollection<User>` | Owned users |

### User
Owner of parking cards and value cards. Belongs to exactly one `Customer`.

| Field | Type | Notes |
|-------|------|-------|
| `CustomerId` | `Guid` | Parent customer |
| `ExternalContactId` | `string?` | External correlation id |
| `Name`, `Surname`, `FirstName`, `Email` | `string?` | Contact details |
| `IsBlocked` | `bool` | Block flag |
| `IsSuspended` | `bool` | Suspend flag |
| `AnonymizationState` | `AnonymizationState` | Deferred-anonymization state |
| `ParkingCards`, `ValueCards` | collections | Owned cards |

### AppAccount
Application login account (email + password), linked 1:1 to a `User`.

| Field | Type | Notes |
|-------|------|-------|
| `Email` | `string` | Unique, max 320 chars, normalized to lowercase |
| `PasswordHash` | `string` | BCrypt hash |
| `EmailConfirmed` | `bool` | Set after email confirmation |
| `EmailConfirmationToken` | `string?` | Emailed at registration |
| `PasswordResetToken` / `PasswordResetTokenExpiresAt` | | Forgot-password flow |
| `RefreshTokenHash` / `RefreshTokenExpiresAt` | | Current session's refresh token (stored hashed) |
| `UserId` | `Guid` | Linked parking user |

### ParkingCard
A contract parking subscription. A user may hold only **one active card per overlapping period** (see [Business Rules](business-rules.md#one-active-card-per-period)).

| Field | Type | Notes |
|-------|------|-------|
| `UserId` | `Guid` | Owner |
| `ExternalCardId` | `string?` | Id in Parking.Logic |
| `SubscriptionPlanId` | `Guid?` | Purchased tariff |
| `StartDate` / `EndDate` | `DateOnly` | Validity period |
| `Status` | `CardStatus` | Active / Blocked / Suspended / Deleted |
| `AnonymizationState` | `AnonymizationState` | Deferred-anonymization state |
| `QrPayload` | `string` | Encoded into the QR code (the card id, `"N"` format), max 256 chars |

Behavior:
- `IsActiveOn(date)` → `true` when `Status == Active`, not deleted, and `StartDate ≤ date ≤ EndDate`.
- `OverlapsPeriod(start, end)` → `true` when `StartDate ≤ end && start ≤ EndDate`.

### ValueCard
A top-up / stored-value card. Currently a CRUD skeleton; the full operation set is an open question in the spec.

| Field | Type | Notes |
|-------|------|-------|
| `UserId` | `Guid` | Owner |
| `ExternalCardId` | `string?` | Id in Parking.Logic |
| `BalanceMinor` | `long` | Balance in **minor units** (e.g. kopiykas) to avoid float rounding |
| `Currency` | `string` | Default `"UAH"`, max 3 chars |
| `Status` | `CardStatus` | Lifecycle status |

### SubscriptionPlan
A parking subscription tariff a user can buy.

| Field | Type | Notes |
|-------|------|-------|
| `Code` | `string` | Unique code (e.g. `MONTHLY`) |
| `Name` | `string` | Display name |
| `PriceMinor` | `long` | Price in minor units |
| `Currency` | `string` | Default `"UAH"`, max 3 chars |
| `DurationDays` | `int` | Card validity length in days |
| `IsActive` | `bool` | Only active plans are offered/purchasable |

Seeded plans (`DbSeeder`): `MONTHLY` (90000 / 30d), `QUARTERLY` (240000 / 90d), `ANNUAL` (850000 / 365d), all UAH.

### Payment
A payment for a parking subscription plus its fiscalization state. The parking card is activated only after a successful payment.

| Field | Type | Notes |
|-------|------|-------|
| `UserId` | `Guid` | Payer |
| `SubscriptionPlanId` | `Guid` | Plan being purchased |
| `ParkingCardId` | `Guid?` | Card activated on success; null until then |
| `AmountMinor` / `Currency` | | Copied from the plan at initiation |
| `Status` | `PaymentStatus` | Pending / Succeeded / Declined / TimedOut / Refunded |
| `ProviderPaymentId` | `string?` | Reference from the payment provider (webhook correlation) |
| `FiscalReceiptId` | `string?` | Receipt id from the fiscal provider after success |
| `FailureReason` | `string?` | Populated on failure |

### OutboxMessage
Transactional-outbox record for asynchronously propagating state changes to Parking.Logic, with delivery tracking and retries.

| Field | Type | Notes |
|-------|------|-------|
| `EntityKind` | `EntityKind` | Customer / User / ParkingCard / ValueCard |
| `EntityId` | `Guid` | Target entity |
| `Operation` | `PropagationOperation` | Create / Update / Delete / Block / … |
| `PayloadJson` | `string?` | Optional JSON snapshot |
| `Status` | `OutboxStatus` | Pending / Delivered / Failed |
| `Attempts` | `int` | Delivery attempts (max 5 → Failed) |
| `LastError` | `string?` | Last delivery error |
| `DeliveredAt` | `DateTimeOffset?` | Set on delivery |

### AuditLogEntry
Audit record for sensitive operations (block/unblock/suspend/resume/delete/anonymize).

| Field | Type | Notes |
|-------|------|-------|
| `EntityKind` / `EntityId` / `Operation` | | What happened |
| `Actor` | `string` | Who did it (account id / `"system"`) |
| `Details` | `string?` | Optional JSON detail |

## Enums

| Enum | Values |
|------|--------|
| `CardStatus` | `Active`, `Blocked`, `Suspended`, `Deleted` |
| `AnonymizationState` | `None`, `ReadyForAnonymization`, `Anonymized` |
| `PaymentStatus` | `Pending`, `Succeeded`, `Declined`, `TimedOut`, `Refunded` |
| `OutboxStatus` | `Pending`, `Delivered`, `Failed` |
| `EntityKind` | `Customer`, `User`, `ParkingCard`, `ValueCard` |
| `PropagationOperation` | `Create`, `Update`, `Delete`, `Block`, `Unblock`, `Suspend`, `Resume`, `Anonymize` |

> Enums are serialized as **strings** in JSON (configured in `Program.cs` via `JsonStringEnumConverter`).

## Persistence notes

Indexes and constraints defined in `AppDbContext.OnModelCreating`:

- `AppAccount.Email` — **unique**; `RefreshTokenHash` indexed.
- `SubscriptionPlan.Code` — **unique**.
- `ExternalContactId` / `ExternalCardId`, `UpdatedAt`, and `CustomerId` indexed on the relevant entities for search and pagination.
- `ParkingCard` has a composite `(UserId, Status)` index supporting the active-card overlap check.
- `OutboxMessage.Status` and `CreatedAt` indexed for the drain query.
- Money is always stored in **minor units** as `long`.
