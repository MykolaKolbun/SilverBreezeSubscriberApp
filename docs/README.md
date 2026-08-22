# Parking Subscription — Backend Documentation

Backend (.NET 10 / ASP.NET Core) for a platform that sells **contract parking subscriptions** (parking cards) and manages top-up value cards. It handles registration/authentication, a facade over the future **Parking.Logic** external API, payment + fiscalization, QR and Wallet-pass generation, and notifications.

All external integrations (Parking.Logic, payment provider, fiscalization, wallet passes, email, push) are currently **stubs behind clean interfaces (ports)**, so real SDKs and keys can be plugged in later without changing the domain logic.

> The mobile/web client (React Native) is out of scope for this stage.

## Documentation index

| Document | What it covers |
|----------|----------------|
| [Architecture](architecture.md) | Clean-architecture layering, projects, dependency flow, cross-cutting concerns |
| [Domain Model](domain-model.md) | Entities, enums, relationships, invariants |
| [API Reference](api-reference.md) | Every HTTP endpoint, request/response DTOs, status codes |
| [Business Rules & Flows](business-rules.md) | One-active-card rule, cascade delete, outbox propagation, anonymization, payment flow |
| [Integrations & Ports](integrations.md) | The port interfaces and their current stub implementations |
| [Configuration & Running](configuration.md) | Build, run, test, config sections, database providers, migrations |
| [Design System](design-system.md) | Web-frontend design tokens (colors, spacing, typography, radius) and component classes |

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
