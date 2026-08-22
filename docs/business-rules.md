# Business Rules & Flows

## Authentication & account provisioning

Registration is **B2C**: a single call creates a `Customer` + `User` **1:1** and an `AppAccount`, atomically, in one `SaveChangesAsync`. The data model still allows a customer to own many users for future B2B use.

- Email is normalized to lowercase and trimmed; it must contain `@`. Passwords must be ≥ 8 characters.
- Duplicate email → `409 Conflict`.
- Login requires a **confirmed** email and correct password (BCrypt verify), else `401`.
- **Tokens:** a JWT access token (default 30 min, claims: `sub` = account id, `uid` = user id, `email`, `jti`) plus an opaque refresh token. The refresh token is stored **hashed** (SHA-256) with an expiry (default 14 days). Refresh re-issues both.
- **Forgot/reset:** `forgot-password` always returns 200 (no account enumeration). A reset token is emailed with a short expiry (default 2 h). `reset-password` re-hashes the password and clears the stored refresh token, invalidating existing sessions.

> `Auth:ExposeDevTokens=true` returns confirmation/reset tokens in API responses to ease local testing (emails are stubbed). **Must be `false` in production.**

## One active card per period

Enforced in `ParkingCardService.EnsureNoOverlappingActiveCardAsync`. When creating a card — or updating its dates — the service rejects the operation if the same user already has a **non-deleted, `Active`** card whose period overlaps the requested `[startDate, endDate]`:

```
overlap  ⇔  existing.StartDate ≤ newEnd  AND  newStart ≤ existing.EndDate
```

A conflict → `409 Conflict` ("User already has an active parking card in this period."). On update, the card being edited is excluded from the check. `endDate` must be on or after `startDate`, else `400`.

## Cascade delete of a customer

`DELETE /customers/{id}` (`CustomerService.DeleteAsync`) performs a cascade in one unit of work:

1. Mark the `Customer` deleted.
2. Mark **all** its `User`s deleted.
3. For every parking card of those users: set `Status = Deleted`, `IsDeleted = true`, and `EndDate = today`.
4. Enqueue a `Delete` propagation for the customer.

## Sensitive operations: propagation + audit

State-changing operations flow through `ChangePropagator.Enqueue`, which — within the caller's transaction — writes:

- An **`OutboxMessage`** (always), for asynchronous propagation to Parking.Logic.
- An **`AuditLogEntry`** (only for audited operations: `Block`, `Unblock`, `Suspend`, `Resume`, `Delete`, `Anonymize`).

`ChangePropagator` never calls `SaveChanges` itself — the calling service commits everything atomically.

### Outbox propagation worker
`OutboxPropagationService` (hosted background service) runs every **5 s**:

1. Loads up to 50 `Pending` messages, oldest first.
2. Calls `IParkingLogicClient.PropagateAsync` for each.
3. On success → `Delivered` (+ `DeliveredAt`). On failure → increment `Attempts`, record `LastError`; after **5 attempts** → `Failed`.

This gives at-least-once delivery with bounded retries while keeping local state the source of truth.

### Some updates only propagate when relevant
`CustomerService.UpdateAsync` and `UserService.UpdateAsync` enqueue an `Update` propagation **only if** the entity actually has cards — there is nothing to sync to the parking system otherwise.

## Deferred anonymization (GDPR-like)

Anonymization is a two-phase, deferred process:

1. **Mark:** `POST /users/{id}/anonymize` (or `/parking-cards/{id}/anonymize`) sets `AnonymizationState = ReadyForAnonymization` (for users, also `IsDeleted = true`), enqueues an `Anonymize` propagation + audit entry, and returns **202 Accepted**.
2. **Apply:** `AnonymizationWorker` (hosted service, every **15 s**) finds entities in `ReadyForAnonymization` and overwrites PII:
   - **Users:** name/surname/firstName replaced with `anon…`, email → `anon-<rnd>@anonymized.invalid`, `ExternalContactId` cleared; state → `Anonymized`.
   - **Parking cards:** `ExternalCardId` cleared; state → `Anonymized`.

## Payment & activation flow

Orchestrated by `PaymentService`. The card is activated **only after a successful payment**.

```
Client                    API / PaymentService              Provider (stub)        Fiscal (stub)
  │  POST /payments  ─────────────►                              │                      │
  │                     create Payment(Pending)                  │                      │
  │                     CreatePaymentAsync ───────────────────►  │                      │
  │  ◄──── {paymentId, providerPaymentId, clientSecret} ─────────┤                      │
  │                                                              │                      │
  │  (client completes payment with the provider)               │                      │
  │                                                              │                      │
  │  POST /payments/webhook {providerPaymentId, "succeeded"} ─►  │                      │
  │                     find Payment by providerPaymentId        │                      │
  │                     [idempotent: skip if already terminal]   │                      │
  │                     create + activate ParkingCard            │                      │
  │                       (enforces one-active-card rule)        │                      │
  │                     FiscalizeAsync ───────────────────────────────────────────►    │
  │                     Payment.Status = Succeeded               │                      │
  │                     push "Payment successful"                │                      │
  │  ◄──── PaymentDto {status:Succeeded, parkingCardId, fiscalReceiptId} ───────────────┤
```

Details:
- **Initiate** copies `priceMinor`/`currency` from the plan onto the `Payment`, creates a provider intent, and stores the `ProviderPaymentId`. Requires a non-deleted user and an **active** plan (404 otherwise).
- **Card period** on success: `start = today`, `end = start + max(1, DurationDays) - 1`.
- **Webhook statuses:** `succeeded` → activate + fiscalize; `declined` → `Declined`; `timeout`/`timedout` → `TimedOut`; anything else → `400`. On non-success the user gets a "Payment failed" push.
- **Idempotency:** a webhook for a payment already in a terminal (non-`Pending`) state is logged and ignored.
- **Refund** (`POST /payments/{id}/refund`): only for `Succeeded` payments (else 409). Refunds via the provider, sets `Refunded`, and **deactivates the associated card** (soft-delete).

## Wallet & QR

For an activated card:
- `GET /{id}/qr` → PNG QR encoding `QrPayload` (the card id), via `IQrCodeGenerator` (QRCoder).
- `GET /{id}/wallet/apple` → `.pkpass` payload via `IWalletPassService.BuildApplePass` (stub).
- `GET /{id}/wallet/google` → a Google Wallet save link (stub).

Whenever a card's status changes (block/unblock/suspend/resume/delete), `IWalletPassService.PushPassUpdateAsync` is invoked to push an update to already-issued passes.

## Pagination

Defined in `Common/Pagination.cs` + `QueryablePaging.cs`:

- Fixed page size of **50** (`Paging.PageSize`).
- Results ordered by `(UpdatedAt desc, Id desc)` — newest changes first.
- The `pagingToken` is an **opaque** base64 string encoding an offset (`o:<n>`). Clients treat it as a black box and pass it back to fetch the next page.
- A response's `nextPagingToken` is `null` when there are no more pages. An unparseable token → `400 validation_error`.
- Offset paging is used because it is stable across SQLite and PostgreSQL and correct for this facade's read workload.

## Localization

The API supports **uk** and **en** via ASP.NET Core request localization; the default culture is **uk**.
