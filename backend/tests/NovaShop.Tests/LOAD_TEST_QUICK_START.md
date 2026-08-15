# NovaShop Load Testing - Quick Start Guide
# Essential documentation for running load tests

# Quick Setup

## 1. Project Structure
The load testing infrastructure includes:

- `nova-shop-load-test.js` - Main k6 load test script
- `.env.k6.example` - Environment configuration
- `setup-k6.sh` - Setup and verification script
- `verify-load-tests.sh` - Verification script

## 2. Quick Commands

### Setup Script
```bash
cd /f/Projects/NovaShop/backend/tests/NovaShop.Tests
./setup-k6.sh
```

### Smoke Test (Minimum Validation)
```bash
k6 run nova-shop-load-test.js --vus=1 --duration=2m
```

### Load Test (100 users)
```bash
BASE_URL=http://localhost:5000 AUTH_TOKEN=your-token k6 run nova-shop-load-test.js --vus=100 --duration=10m
```

### Stress Test (500 users)
```bash
BASE_URL=https://your-api.com AUTH_TOKEN=your-token k6 run nova-shop-load-test.js --vus=500 --duration=15m
```

## Environment Configuration

### Step 1: Copy Environment File
```bash
cp .env.k6.example .env
```

### Step 2: Configure Environment Variables
Edit the `.env` file and configure:

| Variable | Description | Default |
|----------|-------------|---------|
| `BASE_URL` | API base URL | `http://localhost:5000` |
| `AUTH_TOKEN` | Authentication token | `` |
| `SMOKE_TARGET` | Smoke test users | `1` |
| `LOAD_TARGET` | Load test users | `100` |
| `STRESS_TARGET` | Stress test users | `500` |
| `SPIKE_TARGET` | Spike test users | `1000` |
| `RESPONSE_TIME_P95` | P95 threshold (ms) | `500` |
| `RESPONSE_TIME_P99` | P99 threshold (ms) | `1000` |
| `ERROR_RATE` | Error rate threshold | `0.1` |

## Test Scenarios

All tests use the following HTTP endpoints:

### 1. Product List (GET `/api/products`)
Returns paginated list of products
```bash
GET /api/products?searchTerm=laptop&pageNumber=1&pageSize=20
```

### 2. Product Detail (GET `/api/products/{id}`)
Returns detailed product information
```bash
GET /api/products/1
```

### 3. Add to Cart (POST `/api/cart/items`)
Adds product to user cart
```bash
POST /api/cart/items
Content-Type: application/json
{"productId": 1, "quantity": 2}
```

### 4. Checkout (POST `/api/orders`)
Creates new order from cart
```bash
POST /api/orders
Content-Type: application/json
{"shippingAddress": "123 Main St", "paymentMethod": "CreditCard"}
```

### 5. Login (POST `/api/auth/login`)
User authentication
```bash
POST /api/auth/login
Content-Type: application/json
{"username": "testuser", "password": "TestPassword123"}
```

### 6. Search (GET `/api/products/search`)
Product search with query parameter
```bash
GET /api/products/search?q=laptop
```

## Test Profiles

### Smoke Test
- **Users**: 1
- **Duration**: 2 minutes
- **Purpose**: Basic functionality verification
- **Thresholds**: P95 < 2s, Error Rate < 10%

### Load Test
- **Users**: 100
- **Duration**: 10 minutes
- **Purpose**: Performance under normal load
- **Thresholds**: P95 < 500ms, Error Rate < 10%

### Stress Test
- **Users**: 500
- **Duration**: 15 minutes
- **Purpose**: Performance under high load
- **Thresholds**: P95 < 1000ms, Error Rate < 10%

### Spike Test
- **Users**: 1000
- **Duration**: 10 minutes
- **Purpose**: Response to sudden load changes
- **Thresholds**: P95 < 2000ms, Error Rate < 15%

## Performance Metrics

### Response Time
- **P95**: 95th percentile response time
- **P99**: 99th percentile response time
- **Thresholds**: Configured per test profile

### Error Rate
- **Definition**: (Failed requests / Total requests) * 100
- **Thresholds**:
  - Smoke Test: < 10%
  - Load Test: < 10%
  - Stress Test: < 10%
  - Spike Test: < 15%

### Throughput
- **Requests per second**: Number of successful requests per second
- **Monitored**: Via k6 metrics

## Verification

### Setup Verification
```bash
cd /f/Projects/NovaShop/backend/tests/NovaShop.Tests
./setup-k6.sh
```

### Load Test Verification
```bash
cd /f/Projects/NovaShop/backend/tests/NovaShop.Tests
./verify-load-tests.sh
```

### Validation Commands

The verification script checks:

1. **Essential Files**: All required files exist
2. **Test Scenarios**: All HTTP endpoints configured
3. **Load Profiles**: All test profiles configured
4. **Performance Thresholds**: All thresholds set correctly
5. **Environment**: Environment variables configured
6. **Reports**: Report generation enabled

## Common Issues

### k6 Not Installed
```bash
# Install k6
curl -sL https://raw.githubusercontent.com/k6io/k6/master/getdog | bash
```

### Connection Errors
```bash
# Check API endpoint
BASE_URL=http://localhost:5000
# Ensure API is running and accessible
```

### Memory Issues
```bash
# Check available memory
free -h
# Install additional memory if needed
```

## Reporting

### Report Generation
After test completion, k6 generates:

- **HTML Report**: Interactive web report (`report.html`)
- **JSON Report**: Structured data (`report.json`)
- **CSV Report**: Tabular data (`report.csv`)

### Viewing Reports
```bash
# HTML Report
firefox report.html

# JSON Report
cat report.json | jq .

# CSV Report
head -n 10 report.csv
```

## CI/CD Integration

### GitHub Actions Example
```yaml
name: Load Testing

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  load-test:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v3
    
    - name: Setup k6
      run: |
        curl -sL https://raw.githubusercontent.com/k6io/k6/master/getdog | bash
        k6 version
    
    - name: Run Load Test
      run: |
        cd /f/Projects/NovaShop/backend/tests/NovaShop.Tests
        BASE_URL=${{ secrets.API_URL }}
        AUTH_TOKEN=${{ secrets.API_TOKEN }}
        k6 run --vus=100 --duration=10m nova-shop-load-test.js
      env:
        BASE_URL: ${{ secrets.API_URL }}
        AUTH_TOKEN: ${{ secrets.API_TOKEN }}
    
    - name: Upload Reports
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: load-test-reports
        path: |
          reports/
          load-test-report.*
```

### Thresholds Configuration
```yaml
thresholds:
  http_req_duration: ["p(95)<500", "p(99)<1000"]
  http_req_failed: ["rate<0.1"]
  errors: ["rate<0.1"]
```

## Best Practices

### Before Running Tests
1. **API Availability**: Ensure API is running and stable
2. **Database**: Verify database connectivity
3. **Environment**: Set appropriate environment variables
4. **Monitoring**: Enable monitoring tools
5. **Backup**: Backup critical data

### During Tests
1. **Monitor System Resources**: CPU, memory, network
2. **Watch Error Rates**: Set up alerts for high error rates
3. **Check Response Times**: Monitor performance metrics
4. **Review Logs**: Check for errors and warnings

### After Tests
1. **Analyze Reports**: Review performance and identify bottlenecks
2. **Clean Up**: Remove temporary test data
3. **Update Thresholds**: Adjust based on test results
4. **Document Findings**: Record test outcomes and lessons learned

## Contact and Support

For issues with load testing setup:
- Check the documentation
- Review test logs
- Verify API endpoint availability
- Ensure environment variables are set correctly

For advanced support:
- Join k6 community forums
- Check k6 documentation
- Contact infrastructure team

---

**Load Test Infrastructure Complete!** 🚀

Your NovaShop load testing infrastructure is ready for comprehensive performance testing with k6.
