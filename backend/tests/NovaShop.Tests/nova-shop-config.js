// k6 Options File
// File: nova-shop-config.js

// Environment variable schema
export let config = {
  // Environment configuration
  environment: {
    baseUrl: __ENV.BASE_URL || "http://localhost:5000",
    authToken: __ENV.AUTH_TOKEN || "",
    
    // Load test environment
    smokeTarget: 1,
    loadTarget: 100,
    stressTarget: 500,
    spikeTarget: 1000,
    
    // Thresholds
    responseTimeP95: 500,  // milliseconds
    responseTimeP99: 1000, // milliseconds
    errorRate: 0.1,       // 10%
    
    // Test scenarios configuration
    scenarios: {
      productListWeight: 30,
      productDetailWeight: 25,
      addToCartWeight: 20,
      checkoutWeight: 15,
      loginWeight: 10,
      searchWeight: 10,
    },
  },

  // Threshold configuration
  thresholds: {
    http_req_duration: [
      `p(95)<${__ENV.RESPONSE_TIME_P95 || 500}`,     // Response time P95
      `p(99)<${__ENV.RESPONSE_TIME_P99 || 1000}`,    // Response time P99
    ],
    http_req_failed: [`rate<${__ENV.ERROR_RATE || 0.1}`], // Error rate
    errors: [`rate<${__ENV.ERROR_RATE || 0.1}`], // Custom error rate
  },

  // Stage configuration
  stages: [
    { duration: "1m", target: __ENV.SMOKE_TARGET || 1 },      // Smoke: 1 user
    { duration: "5m", target: __ENV.LOAD_TARGET || 100 },    // Load: 100 users
    { duration: "10m", target: __ENV.STRESS_TARGET || 500 }, // Stress: 500 users
    { duration: "5m", target: __ENV.SPIKE_TARGET || 1000 },  // Spike: 1000 users
    { duration: "5m", target: 0 },                           // Teardown
  ],

  // Load test configuration
  loadTest: {
    maxVUs: 1000,
    stages: [
      { duration: "2m", target: 10 },
      { duration: "5m", target: 50 },
      { duration: "10m", target: 100 },
      { duration: "5m", target: 0 },
    ],
  },

  // Spike test configuration
  spikeTest: {
    stages: [
      { duration: "30s", target: 1 },
      { duration: "1m", target: 100 },
      { duration: "30s", target: 1000 },
      { duration: "1m", target: 0 },
    ],
  },

  // Continuous test configuration
  continuousTest: {
    loopIterations: 1000,
    maxDuration: "30m",
    scenario: {
      executor: "shared",
      iterations: 1000,
      vus: 10,
      duration: "5m",
    },
  },
};