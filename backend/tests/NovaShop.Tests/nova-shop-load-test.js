import http from "k6/http";
import { check, sleep } from "k6";
import { Rate } from "k6/metrics";

// Custom error rate metric
const errorRate = new Rate("errors");

// Configuration
const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";
const AUTH_TOKEN = __ENV.AUTH_TOKEN || "";

// Load test scenarios
export let options = {
  stages: [
    { duration: "2m", target: 1 },      // Smoke: 1 user
    { duration: "10m", target: 100 },    // Load: 100 users  
    { duration: "15m", target: 500 },   // Stress: 500 users
    { duration: "10m", target: 1000 },  // Spike: 1000 users
    { duration: "5m", target: 0 },       // Teardown
  ],
  thresholds: {
    http_req_duration: ["p(95)<500", "p(99)<1000"],
    http_req_failed: ["rate<0.1"],
    errors: ["rate<0.1"],
  },
};

export default function (data) {
  const userId = __VU;

  // Test data for this virtual user
  const testData = {
    productList: () => {
      const url = `${BASE_URL}/api/products`;
      const params = { headers: { "Accept": "application/json" } };
      const response = http.get(url, params);
      errorRate.add(response.status >= 400 && response.status < 600);
      return response;
    },
    
    productDetail: () => {
      const productId = Math.floor(Math.random() * 1000) + 1;
      const url = `${BASE_URL}/api/products/${productId}`;
      const params = { headers: { "Accept": "application/json" } };
      const response = http.get(url, params);
      errorRate.add(response.status >= 400 && response.status < 600);
      return response;
    },
    
    addToCart: () => {
      const url = `${BASE_URL}/api/cart/items`;
      const cartItem = {
        productId: Math.floor(Math.random() * 1000) + 1,
        quantity: Math.floor(Math.random() * 5) + 1,
      };
      const params = {
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${AUTH_TOKEN}`,
        },
      };
      const body = JSON.stringify(cartItem);
      const response = http.post(url, body, params);
      errorRate.add(response.status >= 400 && response.status < 600);
      return response;
    },
    
    checkout: () => {
      const url = `${BASE_URL}/api/orders`;
      const orderData = {
        shippingAddress: "123 Test Street",
        paymentMethod: Math.random() > 0.5 ? "CreditCard" : "PayPal",
      };
      const params = {
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${AUTH_TOKEN}`,
        },
      };
      const body = JSON.stringify(orderData);
      const response = http.post(url, body, params);
      errorRate.add(response.status >= 400 && response.status < 600);
      return response;
    },
    
    login: () => {
      const url = `${BASE_URL}/api/auth/login`;
      const params = {
        headers: { "Content-Type": "application/json" },
      };
      const body = JSON.stringify({
        username: `testuser_${userId}",
        password: "TestPassword123",
      });
      const response = http.post(url, body, params);
      errorRate.add(response.status >= 400 && response.status < 600);
      return response;
    },
    
    search: () => {
      const queries = ["laptop", "phone", "shirt", "shoes", "watch", "backpack"];
      const query = queries[Math.floor(Math.random() * queries.length)];
      const url = `${BASE_URL}/api/products/search?q=${query}`;
      const params = { headers: { "Accept": "application/json" } };
      const response = http.get(url, params);
      errorRate.add(response.status >= 400 && response.status < 600);
      return response;
    },
  };

  // Select weighted random scenario
  const totalWeight = Object.values(testData).reduce((sum, fn) => sum + fn.weight, 0);
  let randomValue = Math.random() * totalWeight;
  let selectedFn;

  for (const key in testData) {
    const scenario = testData[key];
    randomValue -= scenario.weight;
    if (randomValue <= 0) {
      selectedFn = scenario.fn;
      break;
    }
  }

  const response = selectedFn();

  // Check responses
  const checkName = `Test_${selectedFn.name}`;
  const checks = {
    [`${checkName}_Status`]: response.status >= 200 && response.status < 300 || response.status === 401 || response.status === 400,
    [`${checkName}_Response_Time`]: response.timings.duration < 5000,
  };

  let checkPassed = true;
  for (const [name, checkFunc] of Object.entries(checks)) {
    checkPassed = checkFunc(response) && checkPassed;
  }

  check(checkName, checkPassed, checks);

  // Random sleep between 0.1 and 2.5 seconds
  sleep(Math.random() * 2.4 + 0.1);
}
