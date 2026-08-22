# Configuration & Running

## Prerequisites

- .NET 10 SDK
- (Production only) A PostgreSQL instance

## Build & run

```bash
cd backend
dotnet build
dotnet run --project ParkingSubscription.Api
```

On startup the app:
1. Applies EF Core migrations (`db.Database.MigrateAsync()`).
2. Seeds baseline subscription plans if none exist (`DbSeeder`).
3. Starts the two background workers (outbox propagation, anonymization).

By default a SQLite file `parking.db` is created in the Api project folder.

Development URLs (see `Properties/launchSettings.json`): `http://localhost:5024` (and `https://localhost:7216`). Override with `ASPNETCORE_URLS`, e.g. `ASPNETCORE_URLS=http://localhost:5080`.

### API documentation (Development only)
- OpenAPI document: `http://localhost:<port>/openapi/v1.json`
- Scalar UI: `http://localhost:<port>/scalar/v1`

## Tests

```bash
cd backend
dotnet test
```

Integration tests spin up the full app on an isolated SQLite database in a temp folder and exercise: the end-to-end register → confirm → login → buy → fiscalize → QR flow, the one-active-card-per-period rule, block auditing, and authorization enforcement. Pagination has dedicated unit tests.

## Configuration reference

Config lives in `appsettings.json` (base/dev), `appsettings.Development.json`, and `appsettings.Production.json`. Key sections:

| Key | Meaning | Default (dev) |
|-----|---------|---------------|
| `Database:Provider` | `Sqlite` or `Postgres` | `Sqlite` |
| `ConnectionStrings:Default` | Connection string | `Data Source=parking.db` |
| `Jwt:Issuer` | JWT issuer | `ParkingSubscription` |
| `Jwt:Audience` | JWT audience | `ParkingSubscription` |
| `Jwt:SigningKey` | HMAC signing key (≥ 32 bytes) | dev placeholder — **replace in prod** |
| `Jwt:AccessTokenMinutes` | Access-token lifetime | `30` |
| `Auth:ExposeDevTokens` | Return confirm/reset tokens in responses | `true` — **must be `false` in prod** |
| `Auth:RefreshTokenDays` | Refresh-token lifetime | `14` |
| `Auth:ResetTokenHours` | Password-reset token lifetime | `2` |
| `Serilog:*` | Log levels | Info; ASP.NET Core & EF Core at Warning |

### Production checklist
- Set `Database:Provider=Postgres` and a real `ConnectionStrings:Default`.
- Replace `Jwt:SigningKey` with a securely stored secret.
- Set `Auth:ExposeDevTokens=false`.
- Provide real implementations for the stubbed ports (see [Integrations](integrations.md)) and add payment-webhook signature verification.

## Database providers & migrations

The provider is chosen at startup from `Database:Provider`:
- `Sqlite` → `UseSqlite` (with a `DateTimeOffset` → UTC-ticks value converter, since SQLite cannot order by `DateTimeOffset`).
- `Postgres` → `UseNpgsql`.

> ⚠️ The bundled `InitialCreate` migration was generated for **SQLite**. For PostgreSQL, generate a separate migration set:
> ```bash
> dotnet ef migrations add InitialCreate --project ParkingSubscription.Infrastructure   # with Database:Provider=Postgres
> ```

## Package management

The solution uses **Central Package Management**: all versions are declared in `backend/Directory.Packages.props`, and individual `.csproj` files reference packages **without** a `Version` attribute. To change a version, edit only that file.

### Security pins
Several transitive dependencies are pinned to patched versions in `Directory.Packages.props`:
- `SQLitePCLRaw.bundle_e_sqlite3 3.0.3` — overrides the deprecated/vulnerable `SQLitePCLRaw.lib.e_sqlite3 2.1.11` (GHSA-2m69-gcr7-jv3q).
- `Microsoft.OpenApi 2.7.5` — patched (GHSA-v5pm-xwqc-g5wc); stays on the 2.x line for source-generator compatibility with `AspNetCore.OpenApi 10.0.9`.
- `System.Security.Cryptography.Xml 10.0.9` — non-vulnerable pin of a JWT-stack transitive dependency.

The build is expected to be free of `NU1903` (known-vulnerability) warnings.

## Key technologies

| Concern | Library |
|---------|---------|
| Web framework | ASP.NET Core (controllers) |
| ORM | EF Core 10 (`Sqlite` / `Npgsql`) |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`, `BCrypt.Net-Next` |
| QR codes | `QRCoder` |
| Logging | `Serilog.AspNetCore` |
| API docs | `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` |
| Testing | `xUnit`, `Microsoft.AspNetCore.Mvc.Testing`, `coverlet` |
