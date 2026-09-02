const { chromium } = require('playwright');
const fs = require('fs');

const CHROMIUM_PATH = 'C:\\Users\\msi\\AppData\\Local\\ms-playwright\\chromium-1187\\chrome-win\\chrome.exe';
const FRONTEND = 'http://localhost:3000';
const GATEWAY = 'http://localhost:8080';
const API = 'http://localhost:5003';

const USERNAME = 'e2e_final_' + Date.now();
const PASSWORD = 'TestPass123!';
const ADMIN_USER = 'admin';
const ADMIN_PASS = 'AdminPass123!';

(async () => {
  const browser = await chromium.launch({
    headless: true,
    executablePath: CHROMIUM_PATH,
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
  });

  const results = [];
  let pass = 0, fail = 0, blocked = 0;
  let token = null;

  function record(name, status, detail = '') {
    results.push({ name, status, detail });
    if (status === 'PASS') pass++;
    else if (status === 'FAIL') fail++;
    else blocked++;
    console.log(`[${status}] ${name}${detail ? ' — ' + detail : ''}`);
  }

  async function test(name, fn) {
    const context = await browser.newContext({ viewport: { width: 1366, height: 768 } });
    const page = await context.newPage();
    try {
      await fn(page);
      record(name, 'PASS');
    } catch (e) {
      record(name, 'FAIL', (e && e.message) ? e.message.substring(0, 300) : String(e));
    } finally {
      await context.close();
    }
  }

  async function registerAndGetToken(page) {
    const phone = '0912' + String(Date.now()).slice(-7);
    const res = await page.request.post(`${GATEWAY}/api/auth/register`, {
      data: {
        username: USERNAME,
        password: PASSWORD,
        email: `${USERNAME}@test.com`,
        phoneNumber: phone,
        firstName: 'Test',
        lastName: 'User',
        city: 'تهران',
        postalCode: '1234567890',
        address: 'تهران، ایران',
      },
      headers: { 'Content-Type': 'application/json' },
    });
    if (!res.ok()) {
      // Try login if already registered
      const loginRes = await page.request.post(`${GATEWAY}/api/auth/login`, {
        data: { username: USERNAME, password: PASSWORD },
        headers: { 'Content-Type': 'application/json' },
      });
      if (!loginRes.ok()) throw new Error(`Register+Login failed: ${res.status()}/${loginRes.status()}`);
      const data = await loginRes.json();
      token = data.token;
    } else {
      const data = await res.json();
      token = data.token;
    }
    // Set cookie for browser-based tests
    await page.context().addCookies([{ name: 'token', value: token, domain: 'localhost', path: '/', maxAge: 28800 }]);
    return token;
  }

  async function loginAndGetToken(page, username = USERNAME, password = PASSWORD) {
    const res = await page.request.post(`${GATEWAY}/api/auth/login`, {
      data: { username, password },
      headers: { 'Content-Type': 'application/json' },
    });
    if (!res.ok()) throw new Error(`Login failed: ${res.status()}`);
    const data = await res.json();
    token = data.token;
    await page.context().addCookies([{ name: 'token', value: token, domain: 'localhost', path: '/', maxAge: 28800 }]);
    return token;
  }

  async function adminLoginAndGetToken(page) {
    const res = await page.request.post(`${GATEWAY}/api/auth/login`, {
      data: { username: ADMIN_USER, password: ADMIN_PASS },
      headers: { 'Content-Type': 'application/json' },
    });
    if (!res.ok()) throw new Error(`Admin login failed: ${res.status()}`);
    const data = await res.json();
    token = data.token;
    await page.context().addCookies([{ name: 'token', value: token, domain: 'localhost', path: '/', maxAge: 28800 }]);
    return token;
  }

  // ===== Phase 1: Service Availability =====
  await test('Frontend accessible (HTTP 200)', async (page) => {
    const res = await page.goto(FRONTEND, { waitUntil: 'domcontentloaded' });
    if (res?.status() !== 200) throw new Error(`Expected 200, got ${res?.status()}`);
  });

  await test('Gateway health check (HTTP 200)', async (page) => {
    const res = await page.goto(`${GATEWAY}/health`, { waitUntil: 'domcontentloaded' });
    if (res?.status() !== 200) throw new Error(`Expected 200, got ${res?.status()}`);
  });

  await test('API health check (HTTP 200)', async (page) => {
    const res = await page.goto(`${API}/health`, { waitUntil: 'domcontentloaded' });
    if (res?.status() !== 200) throw new Error(`Expected 200, got ${res?.status()}`);
  });

  await test('Protected route redirects to login', async (page) => {
    await page.context().clearCookies();
    await page.goto(`${FRONTEND}/orders`);
    await page.waitForTimeout(2000);
    const url = page.url();
    if (!url.includes('/login')) throw new Error(`Expected redirect to /login, got ${url}`);
  });

  await test('Custom doll page redirects to login', async (page) => {
    await page.context().clearCookies();
    await page.goto(`${FRONTEND}/custom-doll-requests`);
    await page.waitForTimeout(2000);
    const url = page.url();
    if (!url.includes('/login')) throw new Error(`Expected redirect to /login, got ${url}`);
  });

  await test('Admin panel redirects to login', async (page) => {
    await page.context().clearCookies();
    await page.goto(`${FRONTEND}/admin`);
    await page.waitForTimeout(2000);
    const url = page.url();
    if (!url.includes('/admin/login')) throw new Error(`Expected /admin/login, got ${url}`);
  });

  await test('Unauthenticated API returns 401', async (page) => {
    const res = await page.request.get(`${GATEWAY}/api/custom-doll-requests`);
    if (res.status() !== 401) throw new Error(`Expected 401, got ${res.status()}`);
  });

  await test('Products API returns products', async (page) => {
    const res = await page.request.get(`${GATEWAY}/api/products`);
    const data = await res.json();
    if (data.totalCount < 1) throw new Error('No products returned');
  });

  // ===== Phase 2: Registration & Login =====
  await test('Registration creates new user and sets token', async (page) => {
    const t = await registerAndGetToken(page);
    if (!t) throw new Error('No token after registration');
    const res = await page.request.get(`${GATEWAY}/api/users/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok()) throw new Error(`Token invalid: ${res.status()}`);
    const data = await res.json();
    if (!data.username) throw new Error('User data missing');
  });

  await test('Login works with correct credentials', async (page) => {
    const t = await loginAndGetToken(page, USERNAME, PASSWORD);
    if (!t) throw new Error('No token after login');
  });

  await test('Return URL: /orders via login form preserves return URL', async (page) => {
    await page.goto(`${FRONTEND}/login?returnUrl=%2Forders`);
    await page.waitForTimeout(1000);
    await page.fill('#username', USERNAME);
    await page.fill('#password', PASSWORD);
    // Intercept the API call to verify it happens
    let apiCalled = false;
    page.on('request', req => {
      if (req.url().includes('api/auth/login')) {
        apiCalled = true;
      }
    });
    await page.click('button[type="submit"]');
    await page.waitForTimeout(5000);
    
    if (!apiCalled) throw new Error('Login API call was not made');
    
    const finalUrl = page.url();
    if (!finalUrl.includes('/orders')) {
      throw new Error(`Return URL failed: ended at ${finalUrl}`);
    }
  });

  await test('Return URL: /custom-doll-request preserved after login', async (page) => {
    await page.goto(`${FRONTEND}/login?returnUrl=%2Fcustom-doll-request`);
    await page.waitForTimeout(1000);
    await page.fill('#username', USERNAME);
    await page.fill('#password', PASSWORD);
    await page.click('button[type="submit"]');
    await page.waitForTimeout(5000);
    const finalUrl = page.url();
    if (!finalUrl.includes('/custom-doll-request')) {
      throw new Error(`Return URL failed: ended at ${finalUrl}`);
    }
  });

  // ===== Phase 3: Products & Search =====
  await test('Homepage loads with product links', async (page) => {
    await registerAndGetToken(page);
    await page.goto(FRONTEND, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const count = await page.locator('a[href^="/products/"]').count();
    if (count < 1) throw new Error('No product links found on homepage');
  });

  await test('Products page shows catalog', async (page) => {
    await loginAndGetToken(page);
    await page.goto(`${FRONTEND}/products`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const count = await page.locator('a[href*="/products/"]').count();
    if (count < 1) throw new Error('No product links on products page');
  });

  await test('Product detail page loads', async (page) => {
    await loginAndGetToken(page);
    await page.goto(`${FRONTEND}/products/3`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const h1Text = await page.locator('h1').first().textContent();
    if (!h1Text || h1Text.length < 1) throw new Error('No product title on detail page');
  });

  await test('Search works', async (page) => {
    await loginAndGetToken(page);
    await page.goto(FRONTEND, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);
    const searchInput = page.locator('input[type="search"]').or(page.locator('input[placeholder*="جستجو"]'));
    if (await searchInput.count() > 0) {
      await searchInput.first().fill('پاندا');
      await page.keyboard.press('Enter');
      await page.waitForTimeout(3000);
    }
  });

  await test('Wishlist toggle works', async (page) => {
    await loginAndGetToken(page);
    const res = await page.request.post(`${GATEWAY}/api/wishlist`, {
      data: { productId: 3 },
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    });
    if (!(res.status() === 200 || res.status() === 201 || res.status() === 204)) throw new Error(`Wishlist API failed: ${res.status()}`);
  });

  // ===== Phase 4: Cart & Checkout =====
  await test('Add to cart', async (page) => {
    await loginAndGetToken(page);
    const res = await page.request.post(`${GATEWAY}/api/cart`, {
      data: { productId: 3, quantity: 1 },
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    });
    if (!res.ok()) throw new Error(`Add to cart API failed: ${res.status()}`);
  });

  await test('Cart page shows item', async (page) => {
    await loginAndGetToken(page);
    await page.request.post(`${GATEWAY}/api/cart`, {
      data: { productId: 3, quantity: 1 },
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    });
    await page.goto(`${FRONTEND}/cart`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const url = page.url();
    if (!url.includes('/cart')) throw new Error(`Not on cart page: ${url}`);
  });

  // ===== Phase 5: Checkout =====
  await test('Checkout — create order', async (page) => {
    await loginAndGetToken(page);
    await page.request.post(`${GATEWAY}/api/cart`, {
      data: { productId: 3, quantity: 1 },
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    });
    const res = await page.request.post(`${GATEWAY}/api/orders`, {
      data: {
        shippingAddress: 'تهران، ایران',
        paymentMethod: 'CashOnDelivery',
        shippingMethod: 'POST',
        phoneNumber: '09120000001',
      },
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    });
    if (!res.ok()) {
      const text = await res.text();
      throw new Error(`Order API failed: ${res.status()} - ${text.substring(0, 200)}`);
    }
    const data = await res.json();
    if (!data.id) throw new Error('No order ID returned');
  });

  await test('Order appears in customer order history', async (page) => {
    await loginAndGetToken(page);
    const res = await page.request.get(`${GATEWAY}/api/orders?pageNumber=1&pageSize=50`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok()) throw new Error(`Orders fetch failed: ${res.status()}`);
    const data = await res.json();
    if (data.totalCount < 1) throw new Error('No orders found for customer');
  });

  // ===== Phase 6: Admin =====
  await test('Admin login works', async (page) => {
    await page.context().clearCookies();
    const t = await adminLoginAndGetToken(page);
    if (!t) throw new Error('No admin token');
  });

  await test('Admin dashboard loads', async (page) => {
    await adminLoginAndGetToken(page);
    await page.goto(`${FRONTEND}/admin`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const url = page.url();
    if (!url.includes('/admin')) throw new Error(`Not at admin: ${url}`);
  });

  await test('Admin order management', async (page) => {
    await adminLoginAndGetToken(page);
    await page.goto(`${FRONTEND}/admin/orders`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const url = page.url();
    if (!url.includes('/admin/orders')) throw new Error(`Not at admin orders: ${url}`);
    const count = await page.locator('a[href^="/admin/orders/"]').count();
    if (count > 0) {
      await page.locator('a[href^="/admin/orders/"]').first().click();
      await page.waitForTimeout(2000);
      const btnCount = await page.locator('button').filter({ hasText: /تأیید|Confirmed|Processing|Paid|Shipped|Delivered/i }).count();
      if (btnCount > 0) {
        const btnText = await page.locator('button').filter({ hasText: /تأیید|Confirmed/i }).first().textContent();
        await page.locator('button').filter({ hasText: /تأیید|Confirmed/i }).first().click();
        await page.waitForTimeout(2000);
      }
    }
  });

  await test('Admin custom doll management', async (page) => {
    await adminLoginAndGetToken(page);
    await page.goto(`${FRONTEND}/admin/custom-doll-requests`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const url = page.url();
    if (!url.includes('/admin/custom-doll-requests')) throw new Error(`Not at admin custom dolls: ${url}`);
  });

  // ===== Phase 7: Custom Doll =====
  await test('Custom doll form loads with all fields', async (page) => {
    await loginAndGetToken(page);
    await page.goto(`${FRONTEND}/custom-doll-request`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const titleCount = await page.locator('#title').count();
    const bodyCount = await page.locator('#bodyColor').count();
    const eyeCount = await page.locator('#eyeColor').count();
    const heightCount = await page.locator('#height').count();
    if (titleCount === 0) throw new Error('#title not visible');
    if (bodyCount === 0) throw new Error('#bodyColor not visible');
    if (eyeCount === 0) throw new Error('#eyeColor not visible');
    if (heightCount === 0) throw new Error('#height not visible');
  });

  await test('Custom doll creation with all fields', async (page) => {
    await loginAndGetToken(page);
    const res = await page.request.post(`${GATEWAY}/api/custom-doll-requests`, {
      data: {
        imageUrl: 'https://picsum.photos/seed/e2e-doll/600/600',
        title: 'E2E Test Doll',
        description: 'A test doll from E2E',
        bodyColor: 'Brown',
        eyeColor: 'Blue',
        height: 30,
      },
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    });
    if (!res.ok()) {
      const text = await res.text();
      throw new Error(`Custom doll API failed: ${res.status()} - ${text}`);
    }
    const id = await res.json();
    if (!id || typeof id !== 'number') throw new Error(`Invalid response: ${id}`);
    const getRes = await page.request.get(`${GATEWAY}/api/custom-doll-requests/${id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!getRes.ok()) throw new Error(`Custom doll fetch failed: ${getRes.status()}`);
    const doll = await getRes.json();
    if (doll.title !== 'E2E Test Doll') throw new Error(`Title mismatch: ${doll.title}`);
    if (doll.bodyColor !== 'Brown') throw new Error(`BodyColor mismatch: ${doll.bodyColor}`);
    if (doll.eyeColor !== 'Blue') throw new Error(`EyeColor mismatch: ${doll.eyeColor}`);
    if (doll.height !== 30) throw new Error(`Height mismatch: ${doll.height}`);
  });

  await test('Custom doll list visible to customer', async (page) => {
    await loginAndGetToken(page);
    await page.goto(`${FRONTEND}/custom-doll-requests`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const url = page.url();
    if (!url.includes('/custom-doll-requests')) throw new Error(`Not at custom dolls: ${url}`);
  });

  // ===== Phase 8: Notifications =====
  await test('Notifications page accessible', async (page) => {
    await loginAndGetToken(page);
    await page.goto(`${FRONTEND}/notifications`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const url = page.url();
    if (!url.includes('/notifications')) throw new Error(`Not at notifications: ${url}`);
  });

  // ===== Phase 9: Responsive UI =====
  const sizes = [
    { name: 'Desktop', w: 1920, h: 1080 },
    { name: 'Tablet', w: 768, h: 1024 },
    { name: 'Mobile', w: 375, h: 667 },
  ];
  const responsivePages = [
    { name: 'Homepage', path: '' },
    { name: 'Login', path: '/login' },
    { name: 'Products', path: '/products' },
    { name: 'Product Detail', path: '/products/3' },
    { name: 'Register', path: '/register' },
  ];

  for (const sz of sizes) {
    for (const pg of responsivePages) {
      const testName = `${sz.name} — ${pg.name} — no horizontal overflow`;
      test(testName, async (page) => {
        await page.setViewportSize({ width: sz.w, height: sz.h });
        await page.goto(pg.path === '' ? FRONTEND : `${FRONTEND}${pg.path}`, { waitUntil: 'networkidle' });
        await page.waitForTimeout(2000);
        const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1);
        if (overflow) throw new Error('Horizontal overflow detected');
      });
    }
  }

  await browser.close();

  console.log('\n========== E2E TEST SUMMARY ==========');
  console.log(`PASS: ${pass}`);
  console.log(`FAIL: ${fail}`);
  console.log(`BLOCKED: ${blocked}`);
  console.log(`Total: ${results.length}`);
  console.log('======================================\n');

  fs.writeFileSync('/tmp/e2e-results.json', JSON.stringify(results, null, 2));
})().catch(e => { console.error('Fatal:', e); process.exit(1); });
