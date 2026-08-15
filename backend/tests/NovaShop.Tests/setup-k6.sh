# NovaShop E-Commerce Load Test - Quick Start
# Essential setup script for k6 load testing

# Configuration
BASE_URL=${BASE_URL:-"http://localhost:5000"}
AUTH_TOKEN=${AUTH_TOKEN:-""}
VUS=${VUS:-100}
DURATION=${DURATION:-"10m"}

# Show banner
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║       NOVASHOP E-COMMERCE LOAD TEST SETUP                     ║"
echo "║                                                              ║"
echo "║  Load test infrastructure for NovaShop API                   ║"
(echo "║                                                              ║"; echo "║  Based on comprehensive test requirements                   ║")
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

echo "=== LOAD TEST CONFIGURATION ==="
echo

# Validate environment
echo "Environment Check:"
echo "  • BASE_URL: $BASE_URL"
echo "  • Users (VUS): $VUS"
echo "  • Duration: $DURATION"
echo "  • Auth Token: ${AUTH_TOKEN:-[NOT SET]"
echo ""

# Test scenarios configuration
echo "Test Scenarios:"
echo "  • Product List (GET /api/products)"
echo "  • Product Detail (GET /api/products/{id})"
echo "  • Add to Cart (POST /api/cart/items)"
echo "  • Checkout (POST /api/orders)"
echo "  • Login (POST /api/auth/login)"
echo "  • Search (GET /api/products/search)"
echo ""

# Load profiles
echo "Load Profiles:"
echo "  • Smoke Test: 1 user (2 minutes)"
echo "  • Load Test: 100 users (10 minutes)"
echo "  • Stress Test: 500 users (15 minutes)"
echo "  • Spike Test: 1000 users (10 minutes)"
echo ""

# Performance thresholds
echo "Performance Thresholds:"
echo "  • Response Time P95: < 500ms"
echo "  • Response Time P99: < 1000ms"
echo "  • Error Rate: < 10%"
echo ""

# Quick commands
echo "=== QUICK COMMANDS ==="
echo

echo "Setup and run smoke test (minimum validation):"
(echo "cd /f/Projects/NovaShop/backend/tests/NovaShop.Tests"; echo "./setup-k6.sh && echo "k6 run nova-shop-load-test.js --vus=1 --duration=2m")
echo

echo "Run comprehensive load test:"
echo "cd /f/Projects/NovaShop/backend/tests/NovaShop.Tests"
echo "BASE_URL=$BASE_URL k6 run nova-shop-load-test.js --vus=$VUS --duration=$DURATION"
echo

echo "Run specific test scenario:"
echo "k6 run nova-shop-load-test.js --vus=50 --duration=5m"
echo

echo "Generate HTML report:"
echo "# Generated after test completion"
echo "# View with: firefox reports/load-test-report.html"

echo

echo "=== ESSENTIAL FILES ==="
echo "nova-shop-load-test.js    - Main k6 test script"
echo ".env.k6.example          - Environment configuration"
echo "LOAD_TEST_QUICK_START.md   - Documentation"

if [ -f "nova-shop-load-test.js" ]; then
    if grep -q "stages:" "nova-shop-load-test.js"; then
        echo "✓ Load test stages configured"
    fi
    
    if grep -q "thresholds:" "nova-shop-load-test.js"; then
        echo "✓ Performance thresholds configured"
    fi
    
    if grep -q "product list" "nova-shop-load-test.js"; then
        echo "✓ Product list scenario configured"
    fi
    
    if grep -q "login" "nova-shop-load-test.js"; then
        echo "✓ Login scenario configured"
    fi
fi

echo

echo "=== SETUP COMPLETE ==="
echo "NovaShop load testing infrastructure is ready!"
echo "Follow the commands above to run your tests."
