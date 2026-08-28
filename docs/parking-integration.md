# Parking Integration — SKIDATA sweb(R) Subscribe API

How SilverBreeze pushes subscription data to the **SKIDATA sweb(R)** parking system
so barriers admit cars by QR and/or license plate.

## Pieces

- **Generated client** — `backend/ParkingSubscription.Api/SWebAPI/`
  - `SubscribeApiClient.cs` — NSwag-generated HTTP client (`Client`), namespace
    `ParkingSubscription.Api.Subscribe`; basicAuth.
  - `Models/*.cs` — one file per DTO/enum (CreateCustomer, GetUser, LicensePlate,
    Identification, CarPark, …). `SWebAPI/.editorconfig` silences generated-code warnings.
  - `subscribe.yaml` — the sweb OpenAPI spec.
- **Adapter** — `Api/ParkingLogic/SkidataParkingLogicClient.cs` implements
  `IParkingLogicClient` and replaces the logging `ParkingLogicClientStub`.
- **Outbox** — `ChangePropagator.Enqueue(kind, id, op)` writes an `OutboxMessage`;
  `OutboxPropagationService` (Infrastructure background worker, 5 retries) drains it and
  calls `IParkingLogicClient.PropagateAsync`.

## Data model mapping (three base tables)

| Ours | sweb |
|------|------|
| `Customer` | `Customer` (Contact) |
| `User` (+ `Vehicle` rows) | `User` (Contact + `licensePlates[]`) |
| `ParkingCard` | Standard Parking Card (ContractParkerCard) |

Each entity stores the UUID sweb assigns on create: `Customer.SkidataCustomerId`,
`User.SkidataUserId`, `ParkingCard.SkidataCardId` — used to target later update/block/delete.

Field mappings pushed today:

- **Customer/User**: `Surname`, `FirstName`, `Email`, `Mobile`, `ExternalContactId`.
- **User** also: `licensePlates[]` ← the user's `Vehicle` rows
  (`Country`→country, `PlateNumber`→value, `"{Make} {Model}"`→vehicle); LP-entry policy
  flags (`PassageLp`/`CheckLp`/`MatchEntryPlate`) are sent nested under
  `user.parkingContract` (`passageLP`/`checkLP`/`matchEntryPlate`), per the sweb schema.
- **ParkingCard**: `userId`, `validFrom/validTo` (StartDate/EndDate), `externalCardId`,
  `primaryId` (QR as an `EXT` Identification from `QrPayload`), `productId`
  (from the plan's `ArticleId`), `singleNeutral`, `secondaryIds[]` (`CardIdentification`),
  `carParks[]` (`CardCarPark`, with `CarParkEntryType`→sweb `EntryType`).

Server-managed sweb read-only fields (`productName`, `transferStatus`, `productionReason`,
`blockDate`, `suspension`, `canceled`, `anonymized`) are **not** sent.

## Operation ordering (sweb rules)

- **Create** — the adapter lazily ensures the chain **Customer → User → Card**:
  `EnsureParkingCardAsync` → `EnsureUserAsync` → `EnsureCustomerAsync`. Our local Guid is
  passed as the `Idempotency-Key`, so retries never double-create.
- **Delete** — reverse order **Card → User → Customer**: deleting a user first deletes its
  cards in sweb, then the user; deleting a customer cascades through all its users' cards,
  then users, then the customer.

## `productId` from the plan (`ArticleId`)

`SubscriptionPlan.ArticleId` holds the sweb product UUID for that tariff. When a card is
created, `ResolveProductIdAsync` reads the plan's `ArticleId`, parses it to a Guid and sends
it as `productId` (falling back to a stored `card.ProductId`; logs a warning if absent).
Tariffs (and their `ArticleId`) are managed in the AdminPanel → Тарифи.

## Configuration — all in the AdminPanel (no env secrets)

`ParkingIntegrationConfig` (single row) is edited in **AdminPanel → Налаштування →
Паркінг-інтеграція**: `Enabled`, `BaseUrl`, `Username`/`Password` (encrypted with the shared
Data Protection keys), `FacilityNumber`. Non-editable defaults live on the entity
(`QrIdentificationType=EXT`, `CustomerLinkField=b2b`). The adapter reads this row at runtime,
so enabling/changing it needs no redeploy; while it is disabled or incomplete the adapter
**no-ops** (dev/tests unaffected).

### What the official sweb spec (v2.3) settles

- **Base URL is region-specific.** `sweb.skidata.com` is **staging**; production is
  `https://car.webhost.skidata.com/bei/DTASales/SubscribeApi` (EU Car),
  `car.au.skidata.com` (Asia-Pacific) or `hb.usa.skidata.com` (US). Set the correct
  region in the AdminPanel.
- **user→customer link.** `b2bCustomerId` links the user to an existing B2B customer
  contact (our 1:1 model) — the default. `groupCustomerId` links to a contact *group*.
- **QR / primary identification.** We send the mobile QR payload as the card's `primaryId`
  with type `EXT`; its `subType` is the **bar-code format SKIDATA configures for the product**
  (e.g. `15693`) and must match that config exactly — SKIDATA provides the value, set it in
  Settings. (`_SDCP` is only sweb's *auto-generated* subType when NO primaryId is sent, and is
  not a valid value for our provided identification.) While the subType is empty the adapter
  omits `primaryId` — the card is still created and license-plate entry works; QR recognition
  begins once the subType is filled in.
- **`productId` is optional** (spec §2.3.2 / §3.4.1.3.1): omit it and DTA assigns the
  contractor's default `CAR_CONTRACT_PARKING` product. Provide the per-plan `ArticleId`
  to pin an exact product — fetch the UUIDs via the **Inventory API** (`.../InventoryApi`,
  "fetch subscribe products inventory") or ask SKIDATA.
- **Dates are date-only** (`format: date`, e.g. `2022-03-07`) — the generated client's
  `DateFormatConverter` emits `yyyy-MM-dd`, matching the spec.
- **DTA retries onward delivery to Parking.Logic for ~14 days** after it accepts our call,
  independent of our outbox's 5 delivery retries to DTA.

> Still worth a quick confirmation with SKIDATA: the exact region host, the `FacilityNumber`,
> and whether their barrier scanners expect the `_SDCP` subType for the QR (vs a custom one).

## License-plate entry policy

On `User` (sweb UserParkingContract), editable in the AdminPanel only when the client has
≥1 vehicle:

- **passageLP** — allow entry by license plate.
- **checkLP** — verify the plate matches the profile (entry on another car is denied).
- **matchEntryPlate** — exit only on the car that entered.
