# NovaShop Monitoring Stack

Complete Prometheus + Grafana + OpenTelemetry monitoring for NovaShop.

## Architecture

```
NovaShop API Services (product-api, auth-api, etc.)
  │
  │ metrics (Prometheus format on /metrics)
  │ traces (OTLP gRPC → otel-collector:4317)
  │
  ▼
OpenTelemetry Collector (otel-collector)
  │  ┌────────────────────────────────┐
  │  │  Receives OTLP from all services  │
  │  │  Exposes Prometheus endpoint      │
  │  └────────────────────────────────┘
  │
  ├──────────► Prometheus Server (:9090)
  │            │
  │            └── /metrics scrape
  │            └── rule evaluation → Alertmanager
  │
  └──────────► Prometheus Server (root)
               └── Scrapes /metrics from all services + otel-collector + node-exporter + cadvisor
               └── Alertmanager (:9093) handles alerts

Grafana (:3000)
  └── Prometheus datasource (auto-provisioned)
  └── 3 dashboards (auto-provisioned)
```

## Services

| Service | Port | Purpose |
|---------|------|---------|
| novashop-api-gateway | 5100 | YARP reverse proxy, /health, /metrics |
| product-api, auth-api, etc. | 80 | API services, /health, /metrics |
| otel-collector | 4317/4318/9090 | OTLP receiver + Prometheus exporter |
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
| `otel-config.yml` | OTel Collector config |
| `grafana-dashboards/` | JSON dashboards + provisioning |
| `grafana-datasources/` | Datasource provisioning |

## Exposed Endpoints

### API (each service on :80)
- `/health` — Liveness/readiness probe
- `/metrics` — Prometheus metrics (via prometheus-net)
- `/api/*` — Business endpoints

### API Gateway (:5100)
- `/health` — Gateway health check
- `/metrics` — Gateway Prometheus metrics

## Metrics Collected

### HTTP metrics (automatic via prometheus-net AspNetCore instrumentation)
- `http_requests_total` — Request count by method, path, status
- `http_request_duration_seconds_bucket` — Histogram of request durations
- `http_requests_in_progress` — Active requests

### OTel metrics (via OpenTelemetry SDK)
- AspNetCore instrumentation metrics
- HttpClient instrumentation metrics
- Runtime metrics (GC, JIT, memory)
- Custom `NovaShop` activity source traces

### Business metrics
Business metrics can be added via `prometheus-net` static API:

```csharp
using Prometheus;

var ordersCreated = Metrics.CreateCounter(
    "orders_created_total",
    "Total number of orders created");

// In controller:
ordersCreated.Inc();
```
