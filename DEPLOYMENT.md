# NovaShop Deployment

Free public-demo deployment plan: Cloudflare Pages (frontend) + Render Web Services
(Docker) for the API Gateway and NovaShop.Api, backed by Azure SQL Database (free tier).

## Architecture

```
Frontend (Cloudflare Pages / Next.js)
        ↓  https://<gateway>.onrender.com
API Gateway (Render Web Service — YARP reverse proxy + JWT + CORS + rate limit)
        ↓  http://<api>:PORT  (private)
NovaShop.Api (Render Web Service — monolith: products, orders, auth, cart, …)
        ↓
Azure SQL Database (SQL Server)
```

Notes:
- The backend is a **2-service backend** (ApiGateway + Api monolith), NOT microservices.
- **Exactly ONE** NovaShop.Api instance must run (see Hangfire below).
- Image uploads use local filesystem (`wwwroot/images`) which is **ephemeral** on Render
  Free — see Limitations.

## Frontend Deployment — Cloudflare Pages

- Build command: `npm ci && npm run build`
- Output / publish directory: `.next` (Cloudflare Pages "Next.js" preset detects it)
- Framework preset: Next.js
- Node version: 24 (set in Cloudflare Pages dashboard if required)
- Required env var (build + runtime): `NEXT_PUBLIC_API_GATEWAY_URL`
  - Value: `https://<gateway>.onrender.com` (the Gateway's public URL)
  - Falls back to `http://localhost:5250` if unset (dev only).
- The frontend proxies ALL API traffic through the Gateway (see `frontend/lib/config.ts`).
  Do NOT hardcode the future Render URL in code; set it via the env var above.

## Gateway Deployment — Render Web Service (Docker)

- Runtime: Docker (use the repo `backend/src/NovaShop.ApiGateway/Dockerfile`)
- Health check path: `/health`
- Render injects the port via `$PORT`. Set the start command / env so the gateway
  binds `0.0.0.0:$PORT`:
  - `ASPNETCORE_URLS=http://0.0.0.0:$PORT`
  - `GATEWAY_URL=http://0.0.0.0:$PORT`
  - `ASPNETCORE_ENVIRONMENT=Production`
- YARP backend destination MUST point at the real API URL (override the Docker default
  `http://novashop-api:5000`):
  - `ReverseProxy__Clusters__backend-cluster__Destinations__destination1__Address=https://<api>.onrender.com`
- JWT (must match the API): `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`
- CORS origins: `CORS_ALLOWED_ORIGINS` (comma-separated; include the Cloudflare Pages URL)
- Database (shared): `ConnectionStrings__DefaultConnection`

## API Deployment — Render Web Service (Docker)

- Runtime: Docker (use the repo `backend/src/NovaShop.Api/Dockerfile`)
- Health check path: `/health`
- Render injects the port via `$PORT`. Bind `0.0.0.0:$PORT`:
  - `ASPNETCORE_URLS=http://0.0.0.0:$PORT`
  - `ASPNETCORE_ENVIRONMENT=Production`
- EF Core migrations run automatically at startup (`Database.Migrate()`) and seed demo
  data on first run. No separate migrator step.
- Secrets via env vars (see below). No User Secrets in production.
- Hangfire dashboard is enabled; protect it in production via
  `Hangfire__DashboardAccessKey` (required; see "Hangfire Dashboard" below).

## Hangfire Dashboard

Set `Hangfire__DashboardAccessKey` to a strong secret value in production. The dashboard reads this from configuration (`Hangfire:DashboardAccessKey`). When the key is set, the dashboard page is access-protected — an unauthenticated user receives a 401. The key is passed as a query string parameter `?accessKey=<key>` or `DashboardAccessKey` cookie. When the key is NOT set, the dashboard behaves per the `AdminHangfireAuthorizationFilter` (admin role required). Never expose the dashboard publicly without the access key.

## Database — Azure SQL Database

- Create an Azure SQL Database (free / Basic tier, e.g. 5 DTU or serverless).
- Set **"Allow Azure services and resources to access this server" = Yes** so the
  Render Web Services (Azure-hosted) can reach it.
- Connection string must use **Encrypt=True** (Azure requires TLS):
  `Server=tcp:<server>.database.windows.net,1433;Initial Catalog=NovaShopDb;User ID=<user>;Password=<pw>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;MultipleActiveResultSets=True`
- EF Core `Database.Migrate()` at startup creates the schema and seeds demo data.
- Do NOT migrate to PostgreSQL — SQL Server is required.

## Required Environment Variables (names only — no values)

### API (NovaShop.Api)
- `ASPNETCORE_ENVIRONMENT` = `Production`
- `ASPNETCORE_URLS` = `http://0.0.0.0:$PORT`
- `ConnectionStrings__DefaultConnection`  (SQL Server)
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__ExpireMinutes`
- `Cache__Provider`  (Memory | Redis)
- `Cache__RedisConnectionString`  (optional)
- `RabbitMq__Host`  (optional)
- `RabbitMq__VirtualHost`  (optional)
- `RabbitMq__Username`  (optional)
- `RabbitMq__Password`  (optional)
- `Sms__Provider`  (Log | Mock | Kavenegar)
- `Sms__ApiKey`  (Kavenegar)
- `Sms__SenderNumber`  (Kavenegar)
- `Sms__StoreName`
- `CORS_ALLOWED_ORIGINS`  (optional; CORS is enforced at the Gateway)
- `Hangfire__DashboardAccessKey`  (REQUIRED for production — see Hangfire Dashboard below)
- `OTEL_EXPORTER_OTLP_ENDPOINT`  (optional; if unset, traces are collected in-process but not exported)

### Gateway (NovaShop.ApiGateway)
- `ASPNETCORE_ENVIRONMENT` = `Production`
- `ASPNETCORE_URLS` = `http://0.0.0.0:$PORT`
- `GATEWAY_URL` = `http://0.0.0.0:$PORT`
- `ConnectionStrings__DefaultConnection`  (shared SQL Server)
- `Jwt__Key`  (must match API)
- `Jwt__Issuer`
- `Jwt__Audience`
- `CORS_ALLOWED_ORIGINS`  (comma-separated frontend origins)
- `ReverseProxy__Clusters__backend-cluster__Destinations__destination1__Address`  (API URL)

### Frontend (Cloudflare Pages)
- `NEXT_PUBLIC_API_GATEWAY_URL`  (Gateway public URL)

## Deployment Order

1. Create Azure SQL Database (free tier) + firewall ("Allow Azure services").
2. Note the full ADO.NET connection string (Encrypt=True).
3. Deploy NovaShop.Api to Render (Docker). Set env vars above. Leave `Pre-Deploy` empty
   (migrations run at startup).
4. Wait for `/health` = 200 (Render health check). Verify logs show DB migrate + seed.
5. Deploy NovaShop.ApiGateway to Render (Docker). Set `ReverseProxy__…__Address` to the
   API's public URL. Verify `/health` = 200.
6. Confirm Gateway → API forwarding (e.g. GET `/api/products` via Gateway returns 200).
7. Deploy Frontend to Cloudflare Pages. Set `NEXT_PUBLIC_API_GATEWAY_URL` to the Gateway URL.
8. Set production CORS: add the Cloudflare Pages URL to Gateway `CORS_ALLOWED_ORIGINS`.
9. Run smoke tests (register, login, list products, add to cart, create order).
10. Run E2E tests (`e2e-tests.spec.ts` via Playwright) against the deployed Gateway URL.

## Important Limitations

- **Render Free sleep:** Free Web Services spin down after ~15 min of inactivity; the first
  request after a cold start is slow (DB connection + EF migration/seed re-run). Hangfire
  recurring jobs pause while asleep. Acceptable for a demo.
- **Ephemeral local image storage:** Product/custom-doll uploads write to `wwwroot/images`,
  which is NOT persisted on Render Free. Uploaded images disappear on deploy/restart. This is
  a DEMO LIMITATION / FUTURE PRODUCTION TASK (move to Azure Blob / Cloudflare R2). Seed/demo
  product images use remote `picsum.photos` URLs, so the catalog still renders without uploads.
- **Single API instance:** Run exactly ONE NovaShop.Api. Multiple instances would execute some Hangfire recurring jobs concurrently (e.g. `release-expired-reservations` and `payment-reconciliation` have `[DisableConcurrentExecution]` applied; others rely on DB-level idempotency). Do not scale the API above 1 without reviewing each job's concurrency.
- **Kavenegar SMS:** Requires a valid Kavenegar account + credit. Until configured, set
  `Sms__Provider=Log` (messages are logged, not sent). Production uses `Sms__Provider=Kavenegar`
  with `Sms__ApiKey`.
- **Online payment disabled:** `PaymentPolicy:OnlinePaymentEnabled=false` and
  `WalletEnabled=false` by design. Only in-person / cash-on-delivery ordering is active.
- **JWT key:** Must be a long random string and MUST match between API and Gateway. Generate
  per environment; never reuse the dev placeholder.
