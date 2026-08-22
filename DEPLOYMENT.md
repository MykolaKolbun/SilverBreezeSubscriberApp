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
- `.github/workflows/health-check.yml` — periodic uptime check

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

## One-time Pi setup

```bash
ssh pi@<pi-host>

# Docker (includes Compose v2 plugin)
curl -sSL https://get.docker.com | sh
sudo usermod -aG docker $USER          # re-login afterwards

mkdir -p ~/projects/subscribeapp/apk
cd ~/projects/subscribeapp
# Copy deploy/docker-compose.yml, init.sql, nginx-apk.conf here
# (CI does this automatically; or scp them once manually).
cp .env.example .env && nano .env      # fill DB_PASSWORD + JWT_SIGNING_KEY
```

## GitHub Secrets

| Secret                | Purpose                                                      |
|-----------------------|-------------------------------------------------------------|
| `PI_HOST`             | Pi IP/hostname                                               |
| `PI_USER`             | SSH user (e.g. `pi`)                                         |
| `PI_SSH_PRIVATE_KEY`  | SSH private key (no passphrase)                             |
| `DB_PASSWORD`         | PostgreSQL password                                         |
| `JWT_SIGNING_KEY`     | Long random string (`openssl rand -base64 48`)             |
| `GHCR_PULL_TOKEN`     | PAT with `read:packages` so the Pi can pull the image       |
| `HEALTHCHECK_URL`     | (optional) public `/health` URL for the health-check workflow |

> If you make the GHCR package **public**, the Pi doesn't need `GHCR_PULL_TOKEN`
> and you can drop the `docker login` line from `deploy.yml`.

## Deploy

Push to `master`/`main` (or run the workflow manually). CI runs tests, builds the
`linux/arm64` image, pushes it to GHCR, copies the compose files to the Pi, writes
`.env` from secrets, and runs `docker compose up -d`, then waits for `/health`.

Manual deploy on the Pi:

```bash
cd ~/projects/subscribeapp
docker compose pull
docker compose up -d
curl -f http://localhost:8081/health
```

## Sharing the APK

```bash
# copy your mobile build to the Pi share folder
scp app-release.apk pi@<pi-host>:~/projects/subscribeapp/apk/
```

- Direct download: `http://<pi-host>:8084/app-release.apk`
- Browse all files: `http://<pi-host>:8084/`

(`.apk` is served as `application/vnd.android.package-archive` with a download header.)

## Optional: public access via Cloudflare Tunnel

```bash
curl -L --output cloudflared.deb \
  https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-arm64.deb
sudo dpkg -i cloudflared.deb
cloudflared tunnel login
cloudflared tunnel create subscribeapp
cloudflared tunnel route dns subscribeapp api.example.com
cloudflared tunnel route dns subscribeapp apk.example.com

mkdir -p ~/.cloudflared
cat > ~/.cloudflared/config.yml <<'EOF'
tunnel: subscribeapp
credentials-file: /home/pi/.cloudflared/<tunnel-id>.json
ingress:
  - hostname: api.example.com
    service: http://localhost:8081
  - hostname: apk.example.com
    service: http://localhost:8084
  - service: http_status:404
EOF

sudo cloudflared service install
sudo systemctl start cloudflared
```

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
