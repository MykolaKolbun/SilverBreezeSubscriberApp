# Raspberry Pi Deployment — ParkingSubscription

Adapted from the reusable Pi template for **this** app. Differences from the
EVCharging template:

| Template service        | Here                                                    |
|-------------------------|---------------------------------------------------------|
| Blazor WASM admin (8083)| **Removed** — mobile is React Native (not built yet)    |
| Nginx APK/static (8084) | **Kept** — used to share the mobile `.apk`              |
| RabbitMQ                | **Removed** — app uses in-process background workers     |
| PostgreSQL 16 + pg_cron | **PostgreSQL 16** (no pg_cron; workers do the cleanup)  |
| API (8081)              | **API (8081)** — ASP.NET Core .NET 10, `/health`        |

```
Raspberry Pi (ARM64) ── Docker Compose
  ├─ postgres        (5433 → 5432)
  ├─ api             (8081 → 8080)   /health, Scalar only in Development
  └─ apk-share       (8084 → 80)     serves ./apk/*.apk
        ↓ (optional) Cloudflare Tunnel → public domain
```

## Files

- `backend/ParkingSubscription.Api/Dockerfile` — multi-stage ARM64 build
- `backend/.dockerignore`
- `deploy/docker-compose.yml` — the stack
- `deploy/.env.example` — copy to `.env` on the Pi
- `deploy/init.sql` — first-boot Postgres setup (UTC; no app tables — EF migrations create them)
- `deploy/nginx-apk.conf` — static/APK server config
- `deploy/apk/` — drop the mobile `.apk` here
- `.github/workflows/deploy.yml` — CI: test → build arm64 → push GHCR → deploy

## Database strategy (important)

- **Production / Pi = PostgreSQL** and applies committed **EF Core migrations**
  (`backend/ParkingSubscription.Infrastructure/Persistence/Migrations`) at API
  startup.
- **Local dev = SQLite**, created directly from the model with `EnsureCreated`
  (no migration set kept for SQLite).
- Migrations are generated against Npgsql via a design-time factory:
  ```bash
  cd backend
  dotnet ef migrations add <Name> \
    --project ParkingSubscription.Infrastructure \
    --startup-project ParkingSubscription.Api \
    -o Persistence/Migrations
  ```

## GitHub Secrets — reuse the EVCharging ones

`deploy.yml` uses the **same deploy mechanism and the same secrets** as the
EVCharging repos: it SSHes to the Pi through **Cloudflare Access** (`cloudflared
access ssh`) and decodes a **base64-encoded** private key. So set these to the
**exact same values** you already use for EVCharging (or, if they're
organization secrets, just add this repo to each secret's repository-access list —
then nothing needs re-entering):

| Secret                | Value (same as EVCharging)                                   |
|-----------------------|-------------------------------------------------------------|
| `PI_HOST`             | The Cloudflare Access SSH hostname (used as `--hostname`)    |
| `PI_USER`             | Pi username (`sorrow`)                                       |
| `PI_SSH_PRIVATE_KEY`  | **base64-encoded** private key (decoded on the runner)      |
| `DB_PASSWORD`         | PostgreSQL password (app-specific — pick a new one is fine)  |
| `JWT_SIGNING_KEY`     | Long random string (signs JWT access tokens)                |

> To base64-encode a key on Windows (their documented method):
> ```powershell
> [Convert]::ToBase64String([System.IO.File]::ReadAllBytes("$env:USERPROFILE\.ssh\id_ed25519"))
> ```

> **GHCR image pull:** the image is private. The Pi pulls it using the **one-time**
> `docker login ghcr.io -u mykolakolbun` you already did for EVCharging (stored in
> `~/.docker/config.json`) — no CI token needed.

## One-time Pi setup

The Pi already has Docker, the Cloudflare tunnel, and the GHCR login from your
EVCharging setup. This repo only needs its own folder (CI creates it and writes
`.env` automatically on first deploy, but you can pre-create it):

```bash
mkdir -p ~/SilverBreezeSubscriberApp/apk
```

Add Cloudflare tunnel ingress + DNS for the new services if you want them public
(see the Cloudflare section below): API `:8081`, APK share `:8084`.

## Deploy

Push to `main` (or run the workflow manually). CI builds the `linux/arm64` image,
pushes it to GHCR, connects to the Pi via `cloudflared access ssh`, copies
`docker-compose.yml` + `init.sql` + `nginx-apk.conf` into `~/SilverBreezeSubscriberApp`,
upserts `DB_PASSWORD` + `JWT_SIGNING_KEY` into the Pi's `.env`, and runs
`docker compose pull && down && up`.

Manual deploy on the Pi:

```bash
cd ~/SilverBreezeSubscriberApp
docker compose pull
docker compose up -d
curl -f http://localhost:8081/health
```

## Sharing the APK

```bash
# copy your mobile build to the Pi share folder
scp app-release.apk pi@<pi-host>:~/SilverBreezeSubscriberApp/apk/
```

- Direct download: `http://<pi-host>:8084/app-release.apk`
- Browse all files: `http://<pi-host>:8084/`

(`.apk` is served as `application/vnd.android.package-archive` with a download header.)

## Optional: public access via your existing Cloudflare Tunnel

You already run one tunnel for EVCharging — **add ingress rules to it**, don't
create a new tunnel. Edit the live config (per your EVCharging manual, the service
reads `/etc/cloudflared/config.yml`, not `~/.cloudflared/`):

```bash
sudo nano /etc/cloudflared/config.yml
```

Add two rules **before** the final `http_status:404` line (use one-level
subdomains so Cloudflare's free SSL covers them):

```yaml
  - hostname: sweb.alternatiview.com.ua        # the API
    service: http://localhost:8081
  - hostname: sweb-app.alternatiview.com.ua    # the APK download
    service: http://localhost:8084
```

Then restart and add matching CNAMEs (Target: `<tunnel-id>.cfargotunnel.com`,
Proxy: ON) in the Cloudflare dashboard:

```bash
sudo systemctl restart cloudflared
```

The mobile app would then use `https://sweb.alternatiview.com.ua` as its API base
URL, and testers download the APK from `https://sweb-app.alternatiview.com.ua`.

## Troubleshooting

| Issue                    | Fix                                                        |
|--------------------------|------------------------------------------------------------|
| Image won't pull on Pi   | `docker login ghcr.io` with a `read:packages` PAT          |
| API unhealthy            | `docker compose logs -f api` (usually DB connection/env)   |
| DB connection fails      | `docker compose logs postgres`; check `.env` password      |
| Port already in use      | Change the left side of the port map in `docker-compose.yml` |
| Migrations not applied   | API applies them at startup on Postgres; check API logs    |
| Out of disk space        | `docker system prune -a` (removes unused images)           |

## Useful commands

```bash
docker compose ps
docker compose logs -f api
docker compose restart api
docker system df
```
