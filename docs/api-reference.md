# API Reference

Base URL in development: `http://localhost:5024` (see `Properties/launchSettings.json`).

- **Auth:** JWT bearer. Send `Authorization: Bearer <accessToken>` on all endpoints except `/auth/*` and the payment webhook.
- **Content type:** `application/json`. Enums are serialized as **strings**.
- **Errors:** RFC 7807 ProblemDetails with `status`, `title`, and `type` (a stable error code). See [Architecture → Error handling](architecture.md#error-handling).
- **Pagination:** list/`changes` endpoints return `{ items: [...], nextPagingToken: "..." | null }`. Pass the token back as `?pagingToken=` to fetch the next page. Page size is a fixed **50**. See [Business Rules → Pagination](business-rules.md#pagination).

Interactive docs are available in Development at `/scalar/v1` (Scalar UI) and `/openapi/v1.json` (OpenAPI document).

---

## Auth — `/auth` (anonymous)

| Method | Path | Body | Response |
|--------|------|------|----------|
| POST | `/auth/register` | `RegisterRequest` | `RegisterResult` |
| POST | `/auth/confirm-email` | `ConfirmEmailRequest` | 204 |
| POST | `/auth/login` | `LoginRequest` | `AuthResult` |
| POST | `/auth/refresh` | `RefreshRequest` | `AuthResult` |
| POST | `/auth/forgot-password` | `ForgotPasswordRequest` | 200 `{ message, devToken }` |
| POST | `/auth/reset-password` | `ResetPasswordRequest` | 204 |

**DTOs**

```
RegisterRequest      { email, password, firstName?, surname? }
RegisterResult       { userId, customerId, email, devConfirmationToken? }
ConfirmEmailRequest  { email, token }
LoginRequest         { email, password }
RefreshRequest       { refreshToken }
ForgotPasswordRequest{ email }
ResetPasswordRequest { email, token, newPassword }
AuthResult           { accessToken, refreshToken, accessTokenExpiresAt, userId, customerId }
```

Notes:
- `register` provisions a `Customer` + `User` 1:1 and an `AppAccount`, then emails a confirmation token. `devConfirmationToken` is populated **only** when `Auth:ExposeDevTokens=true`.
- Passwords must be at least **8 characters**; email must contain `@`. Duplicate email → 409.
- `login` requires a confirmed email; bad credentials or unconfirmed email → 401.
- `forgot-password` always returns 200 (does not reveal whether the email exists); `devToken` is populated only in dev.
- `reset-password` invalidates existing sessions by clearing the stored refresh token.

---

## Plans — `/plans` (authorized)

| Method | Path | Response |
|--------|------|----------|
| GET | `/plans` | `PlanDto[]` — active tariffs, cheapest first |

```
PlanDto { id, code, name, priceMinor, currency, durationDays }
```

---

## Customers — `/customers` (authorized)

| Method | Path | Body / Query | Response |
|--------|------|--------------|----------|
| POST | `/customers` | `CreateCustomerRequest` | 201 `CustomerDto` |
| GET | `/customers/search` | `?externalContactId&searchTerm&pagingToken` | `PagedResult<CustomerDto>` |
| GET | `/customers/changes` | `?pagingToken` | `PagedResult<CustomerDto>` |
| GET | `/customers/{id}` | | `CustomerDto` |
| PUT | `/customers/{id}` | `UpdateCustomerRequest` | `CustomerDto` |
| DELETE | `/customers/{id}` | | 204 (cascade delete) |
| GET | `/customers/{id}/users` | `?pagingToken` | `PagedResult<UserDto>` |
| POST | `/customers/{id}/block` | | 204 |
| POST | `/customers/{id}/unblock` | | 204 |

```
CreateCustomerRequest { externalContactId?, name?, surname?, firstName?, email? }
UpdateCustomerRequest { name?, surname?, firstName?, email? }
CustomerDto           { id, externalContactId, name, surname, firstName, email, isBlocked, isDeleted, updatedAt }
```

- `search` filters by exact `externalContactId` and/or a `LIKE` `searchTerm` over name/surname/firstName/email.
- `DELETE` cascades: marks the customer and all its users deleted, and sets `EndDate = today` + `Status = Deleted` on their parking cards.
- `update` propagates to Parking.Logic only if the customer has cards.

---

## Users — `/users` (authorized)

| Method | Path | Body / Query | Response |
|--------|------|--------------|----------|
| POST | `/users` | `CreateUserRequest` | 201 `UserDto` |
| GET | `/users/search` | `?externalContactId&searchTerm&pagingToken` | `PagedResult<UserDto>` |
| GET | `/users/changes` | `?pagingToken` | `PagedResult<UserDto>` |
| GET | `/users/{id}` | | `UserDto` |
| PUT | `/users/{id}` | `UpdateUserRequest` | `UserDto` |
| DELETE | `/users/{id}` | | 204 |
| POST | `/users/{id}/anonymize` | | 202 (deferred) |
| POST | `/users/{id}/block` | | 204 |
| POST | `/users/{id}/unblock` | | 204 |
| POST | `/users/{id}/suspend` | | 204 |
| POST | `/users/{id}/resume` | | 204 |
| GET | `/users/{id}/parking-cards` | `?pagingToken` | `PagedResult<ParkingCardDto>` |
| GET | `/users/{id}/value-cards` | `?pagingToken` | `PagedResult<ValueCardDto>` |

```
CreateUserRequest { customerId, externalContactId?, name?, surname?, firstName?, email? }
UpdateUserRequest { name?, surname?, firstName?, email? }
UserDto           { id, customerId, externalContactId, name, surname, firstName, email,
                    isBlocked, isSuspended, anonymizationState, isDeleted, updatedAt }
```

- Creating a user requires an existing, non-deleted `customerId` (404 otherwise).
- `anonymize` marks the user deleted + `ReadyForAnonymization`; a background worker overwrites the data later (202 Accepted).

---

## Parking cards — `/parking-cards` (authorized)

| Method | Path | Body / Query | Response |
|--------|------|--------------|----------|
| POST | `/parking-cards` | `CreateParkingCardRequest` | 201 `ParkingCardDto` |
| GET | `/parking-cards/search` | `?externalCardId&searchTerm&pagingToken` | `PagedResult<ParkingCardDto>` |
| GET | `/parking-cards/changes` | `?pagingToken` | `PagedResult<ParkingCardDto>` |
| GET | `/parking-cards/{id}` | | `ParkingCardDto` |
| PUT | `/parking-cards/{id}` | `UpdateParkingCardRequest` | `ParkingCardDto` |
| DELETE | `/parking-cards/{id}` | | 204 |
| POST | `/parking-cards/{id}/anonymize` | | 202 |
| POST | `/parking-cards/{id}/block` | | 204 |
| POST | `/parking-cards/{id}/unblock` | | 204 |
| POST | `/parking-cards/{id}/suspend` | | 204 |
| POST | `/parking-cards/{id}/resume` | | 204 |
| GET | `/parking-cards/{id}/qr` | | `image/png` |
| GET | `/parking-cards/{id}/wallet/apple` | | `application/vnd.apple.pkpass` |
| GET | `/parking-cards/{id}/wallet/google` | | `{ saveUrl }` |

```
CreateParkingCardRequest { userId, subscriptionPlanId?, startDate, endDate, externalCardId? }
UpdateParkingCardRequest { startDate?, endDate?, externalCardId? }
ParkingCardDto           { id, userId, externalCardId, subscriptionPlanId, startDate, endDate,
                           status, anonymizationState, qrPayload, isDeleted, updatedAt }
```

- Create/update enforce `endDate ≥ startDate` (400) and the **one-active-card-per-period** rule (409). See [Business Rules](business-rules.md#one-active-card-per-period).
- `delete` soft-deletes, sets `Status = Deleted` and `EndDate = today`, and pushes a wallet-pass update.
- Status changes (block/unblock/suspend/resume) push a wallet-pass update.

---

## Value cards — `/value-cards` (authorized)

| Method | Path | Body / Query | Response |
|--------|------|--------------|----------|
| POST | `/value-cards` | `CreateValueCardRequest` | 201 `ValueCardDto` |
| GET | `/value-cards/{id}` | | `ValueCardDto` |
| GET | `/value-cards/changes` | `?pagingToken` | `PagedResult<ValueCardDto>` |
| DELETE | `/value-cards/{id}` | | 204 |

```
CreateValueCardRequest { userId, balanceMinor, currency, externalCardId? }
ValueCardDto           { id, userId, externalCardId, balanceMinor, currency, status, isDeleted, updatedAt }
```

> This is a skeleton; the full value-card operation set is an open question in the spec.

---

## Payments — `/payments`

| Method | Path | Auth | Body | Response |
|--------|------|------|------|----------|
| POST | `/payments` | authorized | `InitiatePaymentRequest` | `InitiatePaymentResult` |
| GET | `/payments/{id}` | authorized | | `PaymentDto` |
| POST | `/payments/{id}/refund` | authorized | | `PaymentDto` |
| POST | `/payments/webhook` | **anonymous** | `PaymentWebhookRequest` | `PaymentDto` |

```
InitiatePaymentRequest { userId, subscriptionPlanId, startDate? }
InitiatePaymentResult  { paymentId, providerPaymentId, clientSecret, amountMinor, currency }
PaymentWebhookRequest  { providerPaymentId, status }   // status: "succeeded" | "declined" | "timeout"
PaymentDto             { id, userId, subscriptionPlanId, parkingCardId, amountMinor, currency,
                         status, fiscalReceiptId, failureReason, updatedAt }
```

- `webhook` is idempotent: repeated callbacks for a payment already in a terminal state are ignored.
- On `succeeded` the parking card is created/activated and the receipt fiscalized. See [the payment flow](business-rules.md#payment--activation-flow).
- `refund` only succeeds for `Succeeded` payments (409 otherwise); it refunds via the provider and deactivates the associated card.
- The webhook is anonymous here; in production it must be authenticated via **provider signature verification**.
