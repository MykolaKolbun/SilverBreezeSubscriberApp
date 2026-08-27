# SilverBreeze — Documentation

Platform that sells **contract parking subscriptions** for the SilverBreeze shopping center:
a .NET 10 backend (Web API + Razor Pages admin), a React Native / Expo mobile app, and a
real integration with the **SKIDATA sweb(R)** parking system.

**Start here:** [Project overview](project-overview.md) — current-state snapshot.

## Documentation index

### Current state
| Document | Covers |
|----------|--------|
| [Project overview](project-overview.md) | Components, capabilities, repo facts |
| [Parking integration](parking-integration.md) | SKIDATA sweb adapter, outbox, mapping, ordering, config |
| [Admin panel](admin-panel.md) | Clients, client edit, subscriptions, tariffs, settings |
| [Mobile app](mobile-app.md) | Auth, offline sync, profile, vehicles, buy flow, gating |
| [Deployment](deployment.md) | Docker topology, shared Postgres, CI/CD, secrets |

### Backend reference (some predates the SKIDATA work)
| Document | Covers |
|----------|--------|
| [Architecture](architecture.md) | Clean-architecture layering, projects, dependency flow |
| [Domain Model](domain-model.md) | Entities, enums, relationships, invariants |
| [API Reference](api-reference.md) | HTTP endpoints, request/response DTOs, status codes |
| [Business Rules & Flows](business-rules.md) | One-active-card rule, cascade delete, outbox, payment flow |
| [Integrations & Ports](integrations.md) | The port interfaces and their implementations |
| [Configuration & Running](configuration.md) | Build, run, test, config sections, DB providers, migrations |
| [Design System](design-system.md) | Design tokens and component classes |

## Quick facts

- **Stack:** .NET 10, ASP.NET Core Web API (controllers), EF Core 10, JWT auth, BCrypt, QRCoder, Serilog, OpenAPI + Scalar, xUnit.
- **Database:** SQLite for local development, PostgreSQL for production (switched via configuration).
- **Architecture:** Clean Architecture — Domain → Application → Infrastructure → Api.
- **Package management:** Central Package Management (`backend/Directory.Packages.props`).

## Solution layout

```
backend/
  ParkingSubscription.Domain          # Entities, enums, domain rules (no dependencies)
  ParkingSubscription.Application     # Services, DTOs, ports (integration interfaces), pagination
  ParkingSubscription.Infrastructure  # EF Core DbContext + migrations, port stubs, JWT, workers
  ParkingSubscription.Api             # Controllers, DI, auth, OpenAPI, error handling
  ParkingSubscription.Tests           # Unit + integration tests
  Directory.Packages.props            # Central package version management
```

## The end-to-end scenario

The system's flagship path — verified by an integration test — is:

**register → confirm email → login → pick a plan → initiate payment → provider webhook (succeeded) → parking card activated + receipt fiscalized → QR / Wallet pass issued.**

See [Business Rules & Flows](business-rules.md#payment--activation-flow) for the detailed sequence.

---

> **Note on `ТЗ §…` references:** the source code comments reference sections of the original Ukrainian technical specification ("технічне завдання"), abbreviated `ТЗ`. Those markers are preserved in the docs where they clarify *why* a rule exists.
