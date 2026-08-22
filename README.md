# Parking Subscription — Backend

Backend (.NET 10) для платформи придбання абонементів на паркінг. Реалізує реєстрацію/авторизацію,
facade над майбутнім Parking.Logic API, оплату+фіскалізацію, генерацію QR та Wallet-passes,
нотифікації. Усі зовнішні інтеграції наразі — **стаби** за чистими інтерфейсами (порти), тож реальні
SDK/ключі підключаються пізніше без зміни доменної логіки.

Мобільний/web клієнт (React Native) — поза обсягом цього етапу. APK-збірка роздається через
Nginx-сервіс у Docker-стеку (див. [DEPLOYMENT.md](DEPLOYMENT.md)).

## Стек

- .NET 10, ASP.NET Core Web API (контролери)
- EF Core 10 — **SQLite** для локальної розробки, **PostgreSQL** для продакшену/Pi
- JWT (access + refresh), BCrypt для паролів
- QRCoder (QR), Serilog (лог/аудит), OpenAPI + Scalar (документація)
- xUnit + WebApplicationFactory (інтеграційні тести)
- Docker (multi-stage ARM64) + docker-compose для Raspberry Pi

## Структура

```
backend/
  Directory.Packages.props            # Central Package Management — усі версії тут
  ParkingSubscription.Domain          # сутності, enums, доменні правила
  ParkingSubscription.Application     # сервіси, DTO, порти, пагінація
  ParkingSubscription.Infrastructure  # EF Core DbContext + міграції, стаби портів, JWT, воркери
  ParkingSubscription.Api             # контролери, DI, auth, OpenAPI, /health, Dockerfile
  ParkingSubscription.Tests           # юніт + інтеграційні тести
deploy/                               # docker-compose стек для Raspberry Pi (Postgres + API + APK share)
.github/workflows/                    # CI: тест → build arm64 → push GHCR → deploy на Pi
```

## Запуск (локально)

```bash
cd backend
dotnet build
dotnet run --project ParkingSubscription.Api
```

За замовчуванням: SQLite-файл `parking.db` створюється в теці Api через `EnsureCreated`, сідяться
базові тарифи. У Development доступна документація:

- OpenAPI: `http://localhost:<port>/openapi/v1.json`
- Scalar UI: `http://localhost:<port>/scalar/v1`
- Health: `http://localhost:<port>/health`

Порт можна задати через `ASPNETCORE_URLS`, напр. `ASPNETCORE_URLS=http://localhost:5080`.

## Тести

```bash
cd backend
dotnet test
```

## База даних: SQLite (dev) vs PostgreSQL (prod)

- **Локально** — SQLite, схема створюється з моделі (`EnsureCreated`), без міграцій.
- **Прод/Pi** — PostgreSQL, застосовуються **EF Core міграції** при старті API. Міграції
  генеруються під Npgsql через design-time factory (див. [DEPLOYMENT.md](DEPLOYMENT.md)).

## Наскрізний сценарій (curl)

```bash
B=http://localhost:5080
# 1. register → повертає devConfirmationToken (лише коли Auth:ExposeDevTokens=true)
curl -X POST $B/auth/register -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Sup3rSecret!","firstName":"A","surname":"B"}'
# 2. confirm-email {email, token}
# 3. login {email, password} → accessToken
# 4. GET /plans (Bearer)
# 5. POST /payments {userId, subscriptionPlanId} → providerPaymentId
# 6. POST /payments/webhook {providerPaymentId, status:"succeeded"} → активація картки + фіскалізація
# 7. GET /parking-cards/{id}/qr → PNG; /wallet/apple → .pkpass-стаб
```

## Розгортання на Raspberry Pi

Повна інструкція — [DEPLOYMENT.md](DEPLOYMENT.md). Коротко: push у `master`/`main` запускає CI, яка
тестує, збирає ARM64-образ, пушить у GHCR і розгортає docker-compose стек на Pi
(PostgreSQL + API `:8081` + Nginx APK-share `:8084`).

## Керування версіями пакетів

**Central Package Management** — усі версії в [`backend/Directory.Packages.props`](backend/Directory.Packages.props);
`.csproj` містять `PackageReference` без `Version`.

## Відомі нотатки

- **Безпека залежностей:** транзитивні `SQLitePCLRaw.lib.e_sqlite3 2.1.11` (`GHSA-2m69-gcr7-jv3q`)
  та `Microsoft.OpenApi 2.0.0` (`GHSA-v5pm-xwqc-g5wc`) закриті пінами `SQLitePCLRaw.bundle_e_sqlite3 3.0.3`
  та `Microsoft.OpenApi 2.7.5`. Збірка без попереджень `NU1903`.
- Стаби зовнішніх сервісів логують дії; замінюються реальними клієнтами через ті самі інтерфейси
  (`IParkingLogicClient`, `IPaymentProvider`, `IFiscalProvider`, `IWalletPassService`, `IEmailSender`, `IPushSender`).

## Реалізовані модулі (за ТЗ)

- **Auth (§3):** email реєстрація (авто-створення Customer+User 1:1, B2C), підтвердження email, login,
  refresh, forgot/reset password, JWT.
- **Facade над Parking.Logic (§4):** Customer / User / ParkingCard / ValueCard — усі ендпоінти,
  пошук, `changes`, пагінація (`pagingToken`, сторінка = 50).
- **Бізнес-правила (§5):** одна активна parking card на період; каскадне видалення Customer;
  асинхронна пропагація через outbox-воркер; відкладена анонімізація фоновим воркером.
- **Оплата+фіскалізація (§6):** ініціація, webhook, refund, фіскальний чек.
- **Wallet+QR (§7):** QR PNG, Apple `.pkpass`-стаб, Google Wallet link, push-оновлення pass.
- **Нефункціональні (§9):** BCrypt, аудит, пагінація, локалізація (uk/en), нотифікації, health-check.
