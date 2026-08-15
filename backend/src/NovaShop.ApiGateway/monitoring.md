# NovaShop Monitoring Infrastructure
# Grafana dashboards and configuration for NovaShop microservices monitoring

# Directory structure for monitoring configuration
# This file is primarily for documentation purposes

grafana-dashboards:
  # NovaShop API Performance Dashboard
  # Shows API endpoint performance, response times, and error rates
  - name: "nova-shop-api-performance-dashboard.json"
    description: "API Performance Metrics - Request rate, response times, error rates"
    interval: "30s"
    tags: ["nova-shop", "api", "performance", "backend"]
    panels: 8
    metrics:
      - http_requests_total: "Request counts by endpoint and status code"
      - http_request_duration_seconds: "Response time distribution"
      - active_users_total: "Active user tracking"
      - revenue_total: "Business revenue metrics"
    created: "2024-01-01"

  # NovaShop System Health Dashboard  
  # Shows infrastructure health, resource usage, and service status
  - name: "nova-shop-system-health-dashboard.json"
    description: "Infrastructure Health - CPU, memory, disk, network"
    interval: "30s"
    tags: ["nova-shop", "system", "health", "infrastructure"]
    panels: 8
    metrics:
      - node_cpu_seconds_total: "CPU usage statistics"
      - node_memory_MemAvailable_bytes: "Memory availability"
      - node_filesystem_free_bytes: "Disk space metrics"
      - node_network_receive_bytes_total: "Network I/O statistics"
      - sqlserver_connections: "Database connection counts"
    created: "2024-01-01"

  # NovaShop Business Metrics Dashboard
  # Shows business KPIs, revenue, and user activity
  - name: "nova-shop-business-metrics-dashboard.json"
    description: "Business Metrics - Orders, revenue, user activity"
    interval: "1m"
    tags: ["nova-shop", "business", "metrics", "kpi"]
    panels: 9
    metrics:
      - orders_created_total: "Order creation statistics"
      - revenue_total: "Revenue tracking"
      - active_users_total: "Active user monitoring"
      - cart_operations_total: "Cart operation metrics"
      - product_views_total: "Product view tracking"
    created: "2024-01-01"

# Dashboard data sources configuration
# This file is typically stored in /etc/grafana/provisioning/datasources/datasources.yml
# and is placed in this directory for reference
grafana-datasources:
  datasources:
    - name: Prometheus
      type: prometheus
      url: http://prometheus:9090
      access: proxy
      is_default: true
      version: 1
      editable: true

# Dashboard provisioning configuration
# This file would normally be used by Grafana provisioning
grafana-dashboards-provisioning:
  dashboard:
    apiVersion: 2
    providers:
      - name: "NovaShop Dashboards"
        orgId: 1
        folder: "NovaShop"
        type: file
        disableDeletion: false
        updateInterval: "1m"
        allowUiUpdates: true
        options:
          path: /var/lib/grafana/dashboards
          foldersFromFilesStructure: true

# Alert rules for Grafana
# These are typically stored in /etc/grafana/provisioning/alerting rules
alerting:
  rules:
    - name: "NovaShop Service Alerts"
      folder: "NovaShop"
      rules:
        - alert: "HighErrorRate"
          expr: "rate(http_requests_total{status=\"5xx\"}[5m]) > 0.05"
          for: "2m"
          labels:
            severity: "critical"
            team: "backend"
          annotations:
            summary: "High error rate detected"
            description: "Error rate is {{ $value | humanizePercentage }}"

        - alert: "HighResponseTime"
          expr: "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1.0"
          for: "5m"
          labels:
            severity: "warning"
            team: "backend"
          annotations:
            summary: "High response time detected"
            description: "95th percentile response time is {{ $value }}s"

        - alert: "ServiceDown"
          expr: "up == 0"
          for: "1m"
          labels:
            severity: "critical"
            team: "backend"
          annotations:
            summary: "Service is down"
            description: "Service {{ $labels.job }} has been down"

# Configuration examples
examples:
  datasource_example: "Setup Prometheus as the primary metrics source"
  dashboard_example: "Create performance dashboards with key API metrics"
  alerting_example: "Implement proactive alerts for system issues"
  export_example: "Export dashboards as JSON for version control"

# Quick start guide
quickstart:
  step1: "Place dashboard JSON files in grafana-dashboards directory"
  step2: "Ensure Prometheus is scraping metrics from all services"
  step3: "Restart Grafana to load dashboards automatically"
  step4: "Import dashboards via Grafana's import feature"
  step5: "Configure data sources and alerts"
  step6: "Set up monitoring alerts for critical services"

# File permissions required
permissions:
  dashboards_directory: "755 (read/execute)"
  individual_files: "644 (read)"
  grafana_service: "must have read access to dashboard files"
  backup_requirement: "Regular backup of dashboard configuration files"
