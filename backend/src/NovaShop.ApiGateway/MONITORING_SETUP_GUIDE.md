# NovaShop Infrastructure Monitoring Quick Start
# Complete setup guide for Prometheus, Grafana, and OpenTelemetry monitoring

# Table of Contents
1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Quick Setup](#quick-setup)
4. [Dashboard Import](#dashboard-import)
5. [Alert Configuration](#alert-configuration)
6. [Troubleshooting](#troubleshooting)
7. [Best Practices](#best-practices)

# Overview

NovaShop provides a comprehensive monitoring infrastructure that includes:

- **Prometheus**: Metrics collection and storage
- **Grafana**: Visualization and dashboards
- **OpenTelemetry**: Standardized instrumentation
- **Service Discovery**: Automatic monitoring of all microservices
- **Alerting**: Proactive monitoring and alerting system

This setup provides complete visibility into API performance, system health, and business metrics.

# Prerequisites

## Required Services

Ensure all following services are running:

- ✅ **NovaShop API Gateway**: `http://localhost:5100`
- ✅ **Product API**: `http://localhost:5001`
- ✅ **Auth API**: `http://localhost:5002`
- ✅ **Order API**: `http://localhost:5003`
- ✅ **Cart API**: `http://localhost:5004`
- ✅ **Redis Cache**: `redis://localhost:6379`
- ✅ **SQL Server**: `Server=localhost;Database=master;User Id=sa;...`

## Dependencies

- Docker and Docker Compose
- 8GB RAM minimum (for multi-container setup)
- 20GB disk space (for metrics storage)
- Basic knowledge of Prometheus and Grafana

# Quick Setup

## Step 1: Start Monitoring Stack

Use Docker Compose to start all monitoring services:

```bash
# Navigate to the NovaShop root directory
cd /f/Projects/NovaShop

# Start the full infrastructure (including monitoring)
docker-compose up -d

# Verify services are running
docker ps | grep novashop
```

## Step 2: Access Monitoring Interfaces

### Grafana
- URL: `http://localhost:3000`
- Default credentials: `admin/admin`

### Prometheus
- URL: `http://localhost:9090`
- Metrics endpoint: `http://localhost:9090/metrics`

### API Gateway Health
- URL: `http://localhost:5100/health`

## Step 3: Configure Data Sources

### Grafana Data Source Setup

1. **Login to Grafana**: `http://localhost:3000`
2. **Click "Connections"** in the left sidebar
3. **Click "Data sources"** in the connections menu
4. **Click "Add new data source"**
5. **Select "Prometheus"**
6. **Configure:**
   ```
   URL: http://prometheus:9090
   Access: Server (proxied)
   ```
7. **Click "Save & test"**

## Step 4: Import Dashboards

### Import API Performance Dashboard

1. **In Grafana, click "Dashboards" → "Import"**
2. **Select "Import from panel JSON"**
3. **Upload or paste the contents of:**
   ```
   /f/Projects/NovaShop/backend/src/NovaShop.ApiGateway/grafana-dashboards/nova-shop-api-performance-dashboard.json
   ```
4. **Configure import settings:**
   - **Dashboard title**: "NovaShop API Performance"
   - **Data source**: Prometheus
   - **Folder**: NovaShop
   - **Overwrite existing**: ✅
5. **Click "Import"**

### Import System Health Dashboard

1. **Repeat the same process for:**
   ```
   /f/Projects/NovaShop/backend/src/NovaShop.ApiGateway/grafana-dashboards/nova-shop-system-health-dashboard.json
   ```
2. **Title**: "NovaShop System Health"

### Import Business Metrics Dashboard

1. **Repeat for:**
   ```
   /f/Projects/NovaShop/backend/src/NovaShop.ApiGateway/grafana-dashboards/nova-shop-business-metrics-dashboard.json
   ```
2. **Title**: "NovaShop Business Metrics"

## Step 5: Configure Alerts

### Grafana Alerting Setup

1. **Navigate to "Alerts" → "Alert rules"** in Grafana
2. **Click "Create alert rule"**
3. **Configure based on these rules:**

### Alert Rule 1: High Error Rate
- **Expression**: `rate(http_requests_total{status="5xx"}[5m]) > 0.05`
- **For**: `2m`
- **Severity**: Critical
- **Label**: Service Down
- **Description**: High error rate detected: {{ $value | humanizePercentage }}

### Alert Rule 2: High Response Time
- **Expression**: `histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1.0`
- **For**: `5m`
- **Severity**: Warning
- **Label**: Performance Issue
- **Description**: High response time: {{ $value }}s (95th percentile)

### Alert Rule 3: Service Health Check
- **Expression**: `up == 0`
- **For**: `1m`
- **Severity**: Critical
- **Label**: Service Down
- **Description**: Service {{ $labels.job }} is not responding

# Dashboard Details

## NovaShop API Performance Dashboard

### Key Metrics:
- **Request Rate**: HTTP requests per second by endpoint
- **Response Time**: P95 response time for critical services
- **Error Rate**: Percentage of 5xx errors by service
- **HTTP Status Codes**: Distribution of all HTTP status codes
- **Active Connections**: Current number of active requests

### Panels:
1. **Request Rate by Endpoint** (Stat card)
2. **Response Time (P95)** (Stat card)
3. **Error Rate by Service** (Stat card)
4. **HTTP Status Codes** (Heatmap)
5. **Active Connections** (Stat card)
6. **Requests per Minute Trend** (Graph)

## NovaShop System Health Dashboard

### Key Metrics:
- **CPU Usage**: Total CPU utilization across all nodes
- **Memory Usage**: Available vs total memory
- **Disk Space**: Available disk space on root filesystem
- **Network I/O**: Network receive and transmit rates
- **Database Connections**: Active SQL Server connections
- **Redis Memory**: Redis memory usage

### Panels:
1. **CPU Usage** (Stat card)
2. **Memory Usage** (Stat card)
3. **Disk Space** (Stat card)
4. **Network I/O** (Stat card)
5. **Database Connections** (Stat card)
6. **Redis Memory Usage** (Stat card)
7. **Container Statistics** (Stat card)
8. **Service Health Summary** (Table)

## NovaShop Business Metrics Dashboard

### Key Metrics:
- **Orders Today**: Total orders created in last 24 hours
- **Revenue Today**: Total revenue in last 24 hours
- **Average Order Value**: Average revenue per order
- **Active Users**: Current number of active users
- **Cart Operations**: Total cart operations by type
- **Product Views**: Total product views

### Panels:
1. **Orders Today** (Stat card)
2. **Revenue Today** (Stat card)
3. **Average Order Value** (Stat card)
4. **Active Users** (Stat card)
5. **Cart Operations** (Stat card)
6. **Product Views** (Stat card)
7. **Orders Over Time** (Graph)
8. **Revenue Trend** (Graph)
9. **Top Selling Products** (Table)

# Alert Configuration

## Grafana Alerting Rules

### Error Rate Alert
```yaml
title: High Error Rate
dashboard: NovaShop API Performance
group: System Alerts
expr: rate(http_requests_total{status="5xx"}[5m]) > 0.05
for: 2m
labels:
severity: critical
team: backend
annotations:
summary: High error rate detected
description: Error rate is {{ $value | humanizePercentage }}
```

### Response Time Alert
```yaml
title: High Response Time
dashboard: NovaShop API Performance
group: Performance Alerts
expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1.0
for: 5m
labels:
severity: warning
team: backend
annotations:
summary: High response time detected
description: 95th percentile response time is {{ $value }}s
```

### Service Down Alert
```yaml
title: Service Down
dashboard: NovaShop System Health
group: Infrastructure Alerts
expr: up == 0
for: 1m
labels:
severity: critical
team: infrastructure
annotations:
summary: Service is down
description: Service {{ $labels.job }} has been down for more than 1 minute
```

# Troubleshooting

## Common Issues and Solutions

### Issue 1: Grafana Cannot Connect to Prometheus
**Solution**: Verify Prometheus is running and accessible
```bash
docker logs novashop-prometheus
# Check for any startup errors
```

**Fix**: Ensure Prometheus is healthy and scraping targets
```bash
http://localhost:9090/-/ready
```

### Issue 2: Dashboards Not Loading
**Solution**: Check dashboard JSON syntax
```bash
# Validate JSON syntax
python -m json.tool nova-shop-api-performance-dashboard.json
```

### Issue 3: Metrics Not Appearing
**Solution**: Verify scraping configuration
```bash
# Check Prometheus targets
http://localhost:9090/targets
```

**Fix**: Ensure all services have proper metrics endpoints

### Issue 4: Grafana Permissions
**Solution**: Check data source permissions
- Verify "Grafana" service has read access to dashboard files
- Check file permissions: `chmod 644 *.json`

## Health Checks

### Quick Health Status
```bash
#!/bin/bash
# health-check.sh

set -e

echo "=== NovaShop Monitoring Health Check ==="

echo "1. Checking Prometheus..."
if curl -f http://localhost:9090/-/ready > /dev/null 2>&1; then
    echo "✅ Prometheus is healthy"
else
    echo "❌ Prometheus is not healthy"
    exit 1
fi

echo "2. Checking Grafana..."
if curl -f http://localhost:3000/api/health > /dev/null 2>&1; then
    echo "✅ Grafana is healthy"
else
    echo "❌ Grafana is not healthy"
    exit 1
fi

echo "3. Checking API Gateway..."
if curl -f http://localhost:5100/health > /dev/null 2>&1; then
    echo "✅ API Gateway is healthy"
else
    echo "❌ API Gateway is not healthy"
    exit 1
fi

echo "4. Checking key services..."
if curl -f http://localhost:5001/health > /dev/null 2>&1; then
    echo "✅ Product API is healthy"
else
    echo "❌ Product API is not healthy"
fi

if curl -f http://localhost:5002/health > /dev/null 2>&1; then
    echo "✅ Auth API is healthy"
else
    echo "❌ Auth API is not healthy"
fi

echo "======================================="
echo "Health check completed"
echo "========================================"
```

# Best Practices

## Configuration Management

### 1. Backup Configuration
- Regularly backup dashboard JSON files
- Version control dashboard configurations
- Store configuration in source control

### 2. Environment-Specific Configuration
- Use different configuration files for development, staging, production
- Separate Prometheus configuration for different environments
- Environment-specific alerting rules

### 3. Performance Optimization
- Set appropriate scrape intervals (15s recommended)
- Use efficient PromQL queries
- Configure retention policies appropriately

## Security Considerations

### 1. Access Control
- Restrict Grafana access with firewall rules
- Use authentication for Grafana
- Secure Prometheus endpoints

### 2. Data Privacy
- Ensure metrics don't contain sensitive data
- Configure retention policies for old data
- Anonymize user data in metrics

### 3. Monitoring Security
- Monitor for unusual activity
- Set up alerts for security events
- Regular security audits

## Scaling Considerations

### 1. Horizontal Scaling
- Scale Prometheus with multiple instances
- Configure Grafana replication for high availability
- Consider external storage for large datasets

### 2. Data Retention
- Set appropriate retention policies
- Archive old metrics to long-term storage
- Regular cleanup of old data

## Monitoring Best Practices

### 1. Alert Best Practices
- Avoid alert fatigue with proper thresholds
- Use grouping and deduplication
- Implement escalating alert strategies

### 2. Dashboard Best Practices
- Keep dashboards focused and readable
- Use appropriate time ranges
- Implement consistent naming and organization

### 3. Performance Monitoring
- Monitor monitoring system itself
- Set up alerts for monitoring system issues
- Regular performance tuning

# Next Steps

## Advanced Configuration

### 1. Rate Limiting
Configure rate limiting in the API Gateway:
```json
"RateLimit": {
  "Enable": true,
  "PermitLimit": 100,
  "Window": "00:01:00"
}
```

### 2. CORS Configuration
```json
"Cors": {
  "AllowedOrigins": ["http://localhost:3000"],
  "AllowAnyMethod": true,
  "AllowAnyHeader": true,
  "AllowCredentials": true
}
```

### 3. Authentication
```json
"Authentication": {
  "EnableJwt": true,
  "Jwt": {
    "Issuer": "NovaShop",
    "Audience": "NovaShop.API",
    "SecretKey": "your-secret-key"
  }
}
```

## Automation

### 1. Monitoring Automation
- Set up automated service discovery
- Configure alerts based on business KPIs
- Implement automated health checks

### 2. Continuous Integration
- Integrate monitoring tests with CI/CD pipeline
- Automated testing of service health
- Performance regression detection

## Custom Metrics

### 1. Business Metrics
- Track revenue and conversions
- Monitor user engagement
- Measure customer satisfaction

### 2. Performance Metrics
- Response time by endpoint
- Throughput and capacity utilization
- Error rate and availability

## Closing

The NovaShop monitoring infrastructure provides comprehensive visibility into your microservices architecture. By following these best practices and configuration guidelines, you can ensure reliable monitoring, effective alerting, and actionable insights into your system's performance and health.

For ongoing maintenance:

1. **Regular Updates**: Keep Prometheus and Grafana updated
2. **Performance Tuning**: Adjust retention policies and scrape intervals as needed
3. **Security Audits**: Regularly review access and security configurations
4. **Backup**: Regular backups of dashboards and configurations
5. **Documentation**: Keep documentation up to date with changes

The monitoring stack is designed to scale with your application and provide the insights needed for operational excellence.

---

*© 2024 NovaShop Monitoring Team*  
*Version: 1.0*
