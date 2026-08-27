# Admin Panel

`ParkingSubscription.AdminPanel` — Razor Pages back-office for staff. SilverBreeze
branding (light theme, blue `#009BDD`, dark sidebar, Roboto / Roboto Condensed).

## Auth

Single admin password (default `Passw0rd`, seeded to `AdminConfig` on first use, then
changeable in-app). Cookie auth; the header shows an admin avatar dropdown
(Змінити пароль / Вийти). All pages require auth except `/Login`.

## Pages

### Клієнти (`/Clients`)
Table of users: ID, name, email, phone, active-subscription badge. Rows link to the
client-edit page.

### Редагування клієнта (`/ClientEdit/{id}`)
Layout: left card **Дані користувача**, right column with two separate cards
(**Абонементи**, **Автомобілі**).

- **Дані користувача** — edit FirstName, Surname, Mobile (Email is read-only — it is the
  login). Saving keeps the paired `Customer` in sync (client and user are the same person,
  1:1) and enqueues `User Update` + `Customer Update` to sweb.
- **Політика в'їзду за номером** — `passageLP` / `checkLP` / `matchEntryPlate`. Editable only
  when the client has ≥1 vehicle; the backend forces the flags `false` without a vehicle.
- **Абонементи** — lists the user's parking cards (tariff, period, status). Block / suspend /
  resume / unblock act **per subscription** (sweb ParkingCard operations) and update the
  card status + enqueue the matching op.
- **Автомобілі** — read-only list of the user's vehicles (plate / make / model); vehicles are
  added in the mobile app.

### Тарифи (`/Plans`)
Manage `SubscriptionPlan` rows: list with inline edit (name, price ₴, days, **Article ID**,
active) + add + delete. Delete is soft (`IsDeleted`) so already-issued cards keep their
reference; soft-deleted plans disappear from the app. `ArticleId` is the sweb product UUID
used as the card's `productId`.

### Налаштування (`/Settings`)
Encrypted gateway/integration config (secrets never displayed; blank = keep):

- **Паркінг** — venue capacity (max concurrent active subscriptions, stored in `AdminConfig`).
- **Фіскальний провайдер (Checkbox Online)** — BaseUrl, TaxCode, PIN, License key.
- **Оплата (iPay)** — Merchant ID, BaseUrl, Sign Key.
- **Паркінг-інтеграція (SKIDATA)** — Enabled, Base URL, Login, Password, Facility Number.

Secrets are encrypted with **ASP.NET Data Protection**; the AdminPanel and Api share the
same keys (volume `/keys`, ApplicationName `SilverBreeze`) so the Api can decrypt what the
panel saves.

### Звіти (`/Reports`)
Placeholder.

## sweb propagation from the panel

The AdminPanel writes `OutboxMessage` rows directly (it shares the DB with the Api); the
Api's `OutboxPropagationService` drains them and pushes to sweb. So admin edits, per-card
block/suspend, and profile changes all reach the parking system without a direct call.
