# SilverBreeze — Project Overview

Current-state snapshot of the SilverBreeze parking-subscription platform (as of 2026-08-27).

## What it is

A parking-subscription platform for the **SilverBreeze** shopping center in Kyiv
(вул. Павла Тичини 1В). Customers buy contract parking passes in a mobile app;
passes are pushed to the **SKIDATA sweb(R)** parking system so the barrier admits
the car by QR and/or license plate. Staff manage clients, tariffs and integration
settings in an admin panel.

## Components

| Component | Stack | Purpose |
|-----------|-------|---------|
| `ParkingSubscription.Api` | .NET 10, ASP.NET Core Web API | Public/mobile REST API, auth, payments, fiscalization, SKIDATA push |
| `ParkingSubscription.AdminPanel` | .NET 10, Razor Pages | Staff back-office (clients, tariffs, settings) |
| `ParkingSubscription.Application` | .NET 10 class lib | Services (facade), DTOs, ports, business rules |
| `ParkingSubscription.Infrastructure` | EF Core 10 | DbContext + migrations, provider stubs, background workers |
| `ParkingSubscription.Domain` | .NET 10 class lib | Entities, enums, invariants |
| `mobile/` | React Native / Expo SDK 57 | Customer app (passes, QR, vehicles, profile, payments) |

## Key capabilities (current)

- **Passwordless auth** — email OTP; long-lived sessions.
- **Buy a pass** — pick a tariff, pay via **iPay** hosted page, card activates after
  server-confirmed payment, receipt **fiscalized via Checkbox Online** (PNG + PDF).
- **Offline-first mobile** — cards, vehicles and profile cached locally; on launch the
  app pushes local changes to the backend then pulls server state.
- **SKIDATA sweb integration** — an outbox propagates Customer/User/ParkingCard changes
  to the parking system (create in order Customer→User→Card, delete in reverse).
- **Admin panel** — manage clients (name/phone, LP-entry policy, per-subscription
  block/suspend), tariffs (incl. sweb `ArticleId`), and gateway/integration settings.

## Documentation index

| Document | Covers |
|----------|--------|
| [Parking integration](parking-integration.md) | SKIDATA sweb adapter, outbox, entity mapping, config, ordering |
| [Admin panel](admin-panel.md) | Clients, client edit, subscriptions, tariffs, settings |
| [Mobile app](mobile-app.md) | Auth, offline sync, profile, vehicles, buy flow, gating |
| [Deployment](deployment.md) | Docker topology, shared Postgres, CI/CD, secrets |
| [Architecture](architecture.md) | Clean-architecture layering (backend) |
| [Domain model](domain-model.md) | Entities, enums, relationships |
| [Business rules & flows](business-rules.md) | One-active-card, outbox, payment flow |
| [Configuration & running](configuration.md) | Build, run, test, DB providers, migrations |

> Some of the older backend docs (architecture / domain-model / business-rules /
> integrations) predate the SKIDATA work and describe Parking.Logic as a stub — the
> integration-, admin- and mobile-specific docs above reflect the current state.

## Repository facts

- Two working copies exist: the **canonical** repo `D:\My Programming\SubscribeApp`
  (edited + pushed) and a **build copy** `D:\My Programming\SWeb Solutions\SubscribeApp`
  (used to build the APK; kept in sync with `git pull`).
- Central Package Management: `backend/Directory.Packages.props`.
- Mobile is built locally (`npm run build:apk` in `mobile/`); the app footer shows the
  version, build number (commit count) and short SHA so the installed build is identifiable.
