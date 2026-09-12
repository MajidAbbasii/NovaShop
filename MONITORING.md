# NovaShop Monitoring Stack

Complete Prometheus + Grafana + OpenTelemetry tracing for NovaShop.

## Architecture

The monitoring architecture has two distinct layers:

### 1. Metrics (prometheus-net)
```
NovaShop API / Gateway
  │
  │ /metrics (Prometheus text format)
  │
  ▼
Prometheus Server (:9090)
  │
  ├── rule evaluation → Alertmanager (:9093)
  └── Grafana (:3000)
```
- prometheus-net exposes `/metrics` on each service (API :5000, Gateway :5250).
- Prometheus scrapes `/metrics` directly from each service.
- No OpenTelemetry Collector is in the metrics path — metrics flow directly from prometheus-net to Prometheus.

### 2. Traces (OpenTelemetry + OTLP)
```
NovaShop API / Gateway
  │
  │ OTLP gRPC (port 4317) — ONLY when OTEL_EXPORTER_OTLP_ENDPOINT is set
  │
  ▼
OpenTelemetry Collector (optional, docker-compose only)
  │
  └── Logs to stdout (debug)
```
- OpenTelemetry SDK collects traces (AspNetCore, HttpClient, custom "NovaShop" ActivitySource).
- Traces are exported via OTLP **only when** `OTEL_EXPORTER_OTLP_ENDPOINT` env var is set.
- If the env var is absent, traces are collected in-process but not exported.
- The OTel Collector is only available in local Docker Compose, NOT on Render.

## Services

| Service | Port | Purpose |
|---------|------|---------|
| novashop-api-gateway | 5250 | YARP reverse proxy, /health, /metrics |
| novashop-api | 5000 | Monolith API, /health, /metrics |
| otel-collector | 4317/4318/9090 | OTLP receiver (traces only, docker-compose only) |
| prometheus | 9090 | Metrics storage & querying |
| alertmanager | 9093 | Alert routing & silencing |
| grafana | 3000 | Dashboards & visualization |
| node-exporter | 9100 | Host system metrics |
| cadvisor | 8080 | Container runtime metrics |

## Quick Start

```bash
docker-compose up -d
```

Services start in dependency order. Wait ~60s for all health checks to pass.

- Grafana: http://localhost:3000 (admin/admin)
- Prometheus: http://localhost:9090
- Alertmanager: http://localhost:9093

## Files

| File | Purpose |
|------|---------|
| `prometheus.yml` | Prometheus scrape config |
| `alerts.yml` | Prometheus alerting rules |
| `alertmanager.yml` | Alertmanager routing & receivers |
| `otel-config.yml` | OTel Collector config (traces only) |
| `grafana-dashboards/` | JSON dashboards + provisioning |
| `grafana-datasources/` | Datasource provisioning |

## Exposed Endpoints

### API (port 5000)
- `/health` — Liveness/readiness probe (checks PostgreSQL + Redis)
- `/metrics` — Prometheus metrics (via prometheus-net)
- `/api/*` — Business endpoints

### API Gateway (port 5250)
- `/health` — Gateway health + downstream API reachability probe
- `/metrics` — Gateway Prometheus metrics

## Metrics Collected

### HTTP metrics (automatic via prometheus-net)
- `http_requests_total` — Request count by method, path, status
- `http_request_duration_seconds_bucket` — Histogram of request durations
- `http_requests_in_progress` — Active requests

### Traces (OpenTelemetry SDK)
- ASP.NET Core request traces
- HttpClient outgoing request traces
- Custom `NovaShop` ActivitySource activities

### Business metrics
Business metrics can be added via `prometheus-net` static API. The Grafana
"Business Metrics" dashboard currently shows HTTP-level metrics only — it no
longer references custom business metrics (`orders_created_total`,
`revenue_total`, etc.) that are not yet implemented.

```csharp
using Prometheus;

var ordersCreated = Metrics.CreateCounter(
    "orders_created_total",
    "Total number of orders created");

// In controller:
ordersCreated.Inc();
```

## OpenTelemetry Configuration

| Environment Variable | Purpose |
|---------------------|---------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP endpoint (e.g. `http://otel-collector:4317`). If unset, no traces are exported. |
| `OTEL_SERVICE_NAME` | Service name label on exported telemetry. |
| `OTEL_RESOURCE_ATTRIBUTES` | Additional resource attributes. |

## Production (Render) Notes

On Render, the OTel Collector and Prometheus are **not available**. The `/metrics`
endpoint exists but there is no Prometheus server to scrape it. OpenTelemetry
traces are collected in-process but not exported (no OTLP endpoint configured).
For production monitoring on Render, use a hosted observability backend
(Honeycomb, Datadog, Grafana Cloud, etc.) by setting `OTEL_EXPORTER_OTLP_ENDPOINT`.
