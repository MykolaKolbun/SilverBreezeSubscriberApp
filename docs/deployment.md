# Deployment

The backend runs on a Raspberry Pi (ARM64) as Docker containers, fronted by a Cloudflare
Tunnel. The mobile APK is built locally.

## Stack (`deploy/docker-compose.yml`)

| Service | Host port | Notes |
|---------|-----------|-------|
| `api` | 8085 → 8080 | ASP.NET Core Web API |
| `apk-share` | 8086 → 80 | Nginx static share for the APK |
| `admin` | 8087 → 8080 | Razor Pages admin panel |
| `web` | 8088 → 8080 | Public server-rendered Web client (Razor Pages) |

There is **no `postgres` service** — the app shares the **EVCharging** PostgreSQL instance.

The `web` client has **no database**: it calls the API over the internal Docker network
(`Api__BaseUrl=http://api:8080`), so there is no CORS and no gateway config of its own. Its
image is `ghcr.io/mykolakolbun/parkingsubscription-web` (built by CI alongside api/admin).

## Database — shared EVCharging PostgreSQL

- EVCharging DB container: `evchargingapi-db-1` (image `postgres16-pgcron`), network alias
  **`db`** on external Docker network **`evchargingapi_default`**, superuser `evuser`.
- SilverBreeze DB: database **`SWeb_DB`** owned by role **`parking`** (password = `.env`
  `DB_PASSWORD`), provisioned once with `deploy/create-db.sql`. Tables are created/updated by
  **EF Core migrations at API startup** — the API is what migrates the schema.
- `api` and `admin` join `evchargingapi_default` and connect with `Host=${DB_HOST:-db}`.
- In **pgAdmin**, `SWeb_DB` appears under the existing EVCharge server (port 5433) — no tunnel.

> Gotcha: a leftover `docker-compose.override.yml` (once used to loopback-publish a port on
> the old, now-removed `postgres` service) breaks `docker compose up` ("service postgres has
> neither an image nor a build context") — do not recreate it.

## Secrets & configuration

`.env` on the Pi (never committed) carries only the **bootstrap** secrets:
`DB_PASSWORD`, `JWT_SIGNING_KEY`, `ADMIN_PASSWORD` (initial). Everything else — iPay,
Checkbox and SKIDATA credentials — is configured in the **AdminPanel → Налаштування** and
stored **encrypted in the DB** (shared Data Protection keys, volume `dataprotection_keys`
mounted into both `api` and `admin`).

`Auth__ExposeDevTokens=true` (test phase) returns the OTP dev code in the API response — set
to `false` before production once real email delivery is wired in.

## CI/CD (`.github/workflows/deploy.yml`)

On push to `main`: build + push GHCR images for `api` and `admin`, `scp` the compose stack to
the Pi, upsert the bootstrap secrets into `.env` (only when set), and `docker compose pull &&
up -d`. SSH to the Pi goes through `cloudflared access ssh`.

## Cloudflare Tunnel

Ingress hostnames route to the container ports (e.g. `sweb…` → api :8085, `sweb-admin…` →
admin :8087, and a new hostname for the Web client → `http://localhost:8088`). The catch-all
`- service: http_status:404` must stay **last** in `config.yml`, and Universal SSL only covers
one subdomain level (use single-level hostnames like `sweb-admin.alternatiview.com.ua`).

> **New Web client hostname (manual, Pi-side):** the tunnel `config.yml` lives on the Pi, not
> in this repo. To expose the Web client, add an ingress rule **above** the catch-all, e.g.
> ```yaml
>   - hostname: sweb-app.alternatiview.com.ua
>     service: http://localhost:8088
> ```
> then `cloudflared tunnel ... ` reload (or restart the cloudflared service) and add the DNS
> route for that hostname. Until then the client is reachable on the LAN at `http://<pi>:8088`.
