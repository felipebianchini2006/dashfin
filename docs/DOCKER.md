# Docker (dev) & Production Blueprint

## Dev (Docker Compose)

Services:
- Postgres (`postgres:16`)
- API (`src/Finance.Api`) with Hangfire server + dashboard
- Next.js (`web`) in dev mode

### 1) Configure env

```bash
cp .env.example .env
```

### 2) Start

```bash
docker compose up --build
```

URLs:
- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`
- Hangfire Dashboard: `http://localhost:5000/hangfire` (Basic auth)
- Web: `http://localhost:3000`

Hangfire dashboard credentials come from `.env`:
- `HANGFIRE_USER`
- `HANGFIRE_PASSWORD`

### 3) DB migrations on startup

Compose runs `db/schema.sql` and then every script under `db/migrations/*.sql` via the `migrate` service.

If you change SQL migrations and want a clean DB:

```bash
docker compose down -v
docker compose up --build
```

### 4) Local PDF storage

Imports use local storage mounted inside the API container:
- volume: `api_files`
- path: `/data/files` (configured via `FileStorage__Local__RootPath`)

### 5) Health checks

- `GET http://localhost:5000/health/live`
- `GET http://localhost:5000/health` (db + hangfire + storage)

## Production blueprint (recommended)

### Components
- Reverse proxy (Caddy/Traefik/Nginx) terminating HTTPS
- API (`Finance.Api`) behind the proxy
- Worker process for background jobs (Hangfire server) **separate from API** for better scaling
- Postgres (managed service recommended)
- Object storage (S3/MinIO) for PDF storage

### Key settings
- **HTTPS**: required for secure refresh cookies (set `AuthCookies:RefreshTokenSecure=true`, `SameSite=None`)
- **Secrets**: inject via secret manager (no secrets in repo)
  - `ConnectionStrings__Default`
  - `Jwt__SigningKey`
  - S3 credentials (`FileStorage:S3:*`)
  - Hangfire dashboard credentials (or disable dashboard entirely in prod)
- **Migrations**:
  - Run SQL migrations (or migrate to EF Core migrations) as a one-off job during deploy.
  - Avoid `EnsureCreated` in production.

### Hangfire dashboard
- Prefer disabling externally (`Hangfire:Dashboard:Enabled=false`) or restricting behind VPN / internal network.
- If enabled, keep it protected by strong credentials and HTTPS.

