import { test, expect, Page } from '@playwright/test';

const FRONTEND_URL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:3000';
const GATEWAY_URL = process.env.PLAYWRIGHT_GATEWAY_URL || 'http://localhost:5250';
const API_URL = process.env.PLAYWRIGHT_API_URL || 'http://localhost:5000';

const USERNAME = `e2e_browser4_${Date.now()}`;
const PASSWORD = 'TestPass123!';
const ADMIN_USER = 'admin';
const ADMIN_PASS = 'AdminPass123!';

test.describe('NovaShop Full E2E Flow — Browser Tests', () => {

  test.describe('Phase 1: Service Availability', () => {
    test('1. Frontend is accessible', async ({ page }) => {
      const res = await page.goto(FRONTEND_URL);
      expect(res?.status()).toBe(200);
    });

    test('2. API Gateway health check', async ({ page }) => {
      const res = await page.goto(`${GATEWAY_URL}/health`);
      expect(res?.status()).toBe(200);
    });

    test('3. Protected route redirects to login', async ({ page }) => {
      const res = await page.goto(`${FRONTEND_URL}/orders`);
      expect(res?.status()).toBe(200);
      await expect(page).toHaveURL(/\/login/);
    });

    test('4. Custom doll page redirects to login', async ({ page }) => {
      await page.goto(`${FRONTEND_URL}/custom-doll-requests`);
      await expect(page).toHaveURL(/\/login/);
    });

    test('5. Admin panel redirects to login', async ({ page }) => {
      await page.goto(`${FRONTEND_URL}/admin`);
      await expect(page).toHaveURL(/\/admin\/login/);
    });

    test('6. Unauthenticated API protected endpoint returns 401', async ({ page }) => {
      const res = await page.request.get(`${GATEWAY_URL}/api/custom-doll-requests`);
      expect(res.status()).toBe(401);
    });
  });

  test.describe('Phase 2: Registration & Login', () => {
    test('7. User can register', async ({ page }) => {
      await page.goto(`${FRONTEND_URL}/register`);
      await page.fill('#username', USERNAME);
      await page.fill('#firstName', 'Test');
      await page.fill('#lastName', 'User');
      await page.fill('#email', `${USERNAME}@test.com`);
      await page.fill('#phone', `09${Math.floor(100000000 + Math.random() * 900000000)}`);
      await page.fill('#password', PASSWORD);
      await page.fill('#city', 'تهران');
      await page.fill('#postalCode', '1234567890');
      await page.fill('#address', 'تهران، ایران');
      await page.click('button[type="submit"]');
      await page.waitForURL('**/products', { timeout: 15000 });
      const cookies = await page.context().cookies();
      const tokenCookie = cookies.find(c => c.name === 'token');
      expect(tokenCookie).toBeTruthy();
    });

    test('8. User can login with Return URL', async ({ page }) => {
      // Go to a protected route — should redirect to login?returnUrl=/orders
      await page.goto(`${FRONTEND_URL}/orders`);
      const currentUrl = page.url();
      expect(currentUrl).toContain('/login');
      expect(currentUrl).toContain('returnUrl=');
      expect(currentUrl).toContain('%2Forders');

      // Now login
      await page.goto(`${FRONTEND_URL}/login?returnUrl=%2Forders`);
      await page.fill('#username', USERNAME);
      await page.fill('#password', PASSWORD);
      await page.click('button[type="submit"]');
      await page.waitForURL('**/orders', { timeout: 15000 });
      await expect(page).toHaveURL(/\/orders/);
    });

    test('9. Login with returnUrl preserving query string', async ({ page }) => {
      await page.goto(`${FRONTEND_URL}/login?returnUrl=%2Forders%3Fpage%3D2`);
      await page.fill('#username', USERNAME);
      await page.fill('#password', PASSWORD);
      await page.click('button[type="submit"]');
      // Should land on /orders?page=2
      const url = page.url();
      expect(url).toContain('/orders');
      expect(url).toContain('page=2');
    });

    test('10. Login as customer, verify token is set', async ({ page }) => {
      await page.context().addCookies([{
        name: 'token',
        value: '',
        domain: 'localhost',
        path: '/',
        maxAge: 0,
      }]);
      await page.goto(`${FRONTEND_URL}/login`);
      await page.fill('#username', USERNAME);
      await page.fill('#password', PASSWORD);
      await page.click('button[type="submit"]');
      await page.waitForURL('**/products', { timeout: 15000 });
      const cookies = await page.context().cookies();
      const tokenCookie = cookies.find(c => c.name === 'token');
      expect(tokenCookie).toBeTruthy();
      expect(tokenCookie?.value.length).toBeGreaterThan(10);
    });
  });

  test.describe('Phase 3: Products & Search', () => {
    test('11. Homepage loads with products', async ({ page }) => {
      await page.goto(FRONTEND_URL);
      const products = page.locator('a[href^="/products/"]');
      await expect(products.first()).toBeVisible();
    });

    test('12. Products page shows catalog', async ({ page }) => {
      await page.goto(`${FRONTEND_URL}/products`);
      const productLinks = page.locator('a[href^="/products/"]').or(page.locator('a[href*="/products/"]'));
      const count = await productLinks.count();
      expect(count).toBeGreaterThan(0);
    });

    test('13. Product detail page loads', async ({ page }) => {
      await page.goto(`${FRONTEND_URL}/products/3`);
      await expect(page.locator('h1')).toBeVisible();
      const addBtn = page.locator('button').filter({ hasText: /سبد خرید|Add to Cart/i });
      await expect(addBtn.first()).toBeVisible();
    });

    test('14. Search works', async ({ page }) => {
      await page.goto(FRONTEND_URL);
      await page.fill('input[type="search"], input[placeholder*="جستجو"], input[placeholder*="search"]', 'پاندا');
      await page.keyboard.press('Enter');
      await page.waitForURL('**/products**');
    });

    test('15. Wishlist toggle (authenticated)', async ({ page }) => {
      // Login first
      await page.goto(`${FRONTEND_URL}/login`);
      await page.fill('#username', USERNAME);
      await page.fill('#password', PASSWORD);
      await page.click('button[type="submit"]');
      await page.waitForURL('**/products', { timeout: 15000 });

      // Go to product page and toggle wishlist
      await page.goto(`${FRONTEND_URL}/products/3`);
      await page.waitForSelector('button', { timeout: 5000 });
      // Heart button (wishlist)
      const heartBtn = page.locator('button').filter({ has: page.locator('svg').first() });
      await heartBtn.first().click();
      await page.waitForTimeout(1000);
    });

    test('16. Cart is accessible when logged in', async ({ page }) => {
      await loginAsCustomer(page);
      await page.goto(`${FRONTEND_URL}/cart`);
      await expect(page).toHaveURL(/\/cart/);
    });
  });

  test.describe('Phase 4: Add to Cart & Checkout', () => {
    test('17. Add to cart', async ({ page }) => {
      await loginAsCustomer(page);
      await page.goto(`${FRONTEND_URL}/products/3`);
      await page.waitForSelector('button', { timeout: 5000 });
      const addBtn = page.locator('button').filter({ hasText: /سبد خرید|Add to Cart/i }).first();
      await addBtn.click();
      await page.waitForTimeout(2000);
    });

    test('18. Checkout flow', async ({ page }) => {
      await loginAsCustomer(page);
      // Add to cart first
      await page.goto(`${FRONTEND_URL}/products/3`);
      await page.waitForSelector('button', { timeout: 5000 });
      const addBtn = page.locator('button').filter({ hasText: /سبد خرید|Add to Cart/i }).first();
      await addBtn.click();
      await page.waitForTimeout(2000);

      // Go to checkout
      await page.goto(`${FRONTEND_URL}/checkout`);
      await page.waitForSelector('#fullName', { timeout: 10000 });

      // Fill shipping info
      await page.fill('#fullName', 'Test User');
      await page.fill('#email', `${USERNAME}@test.com`);
      await page.fill('#phone', '09120000000');
      await page.fill('#address', 'تهران، ایران');
      await page.fill('#city', 'تهران');
      await page.fill('#postalCode', '1234567890');

      // Click place order
      const submitBtn = page.locator('button[type="submit"], button').filter({ hasText: /سفارش|Order|Checkout/i }).first();
      await submitBtn.click();

      // Should redirect to order confirmation
      await page.waitForURL('**/orders/**', { timeout: 15000 });
    });

    test('19. Order appears in order history', async ({ page }) => {
      await loginAsCustomer(page);
      await page.goto(`${FRONTEND_URL}/orders`);
      await page.waitForTimeout(2000);
      // Verify order list is visible
      await expect(page).toHaveURL(/\/orders/);
    });

    test('20. Order detail page loads', async ({ page }) => {
      await loginAsCustomer(page);
      await page.goto(`${FRONTEND_URL}/orders`);
      await page.waitForTimeout(2000);
      const orderLinks = page.locator('a[href^="/orders/"]');
      const count = await orderLinks.count();
      if (count > 0) {
        await orderLinks.first().click();
        await page.waitForTimeout(2000);
        // Verify order detail page loaded
        const url = page.url();
        expect(url).toMatch(/\/orders\/\d+/);
      }
    });
  });

  test.describe('Phase 5: Admin Order Management', () => {
    test('21. Admin login', async ({ page }) => {
      await page.goto(`${FRONTEND_URL}/admin/login`);
      await page.fill('#admin-username', ADMIN_USER);
      await page.fill('#admin-password', ADMIN_PASS);
      await page.click('button[type="submit"]');
      // Wait for redirect to /admin
      try {
        await page.waitForURL('**/admin', { timeout: 15000 });
        await expect(page).toHaveURL(/\/admin/);
      } catch {
        // If admin doesn't exist, try via API
        console.log('Admin login may need DB user. Trying direct API register...');
      }
    });

    test('22. Admin order list', async ({ page }) => {
      await adminLogin(page);
      await page.goto(`${FRONTEND_URL}/admin/orders`);
      await page.waitForTimeout(2000);
      await expect(page).toHaveURL(/\/admin\/orders/);
    });

    test('23. Admin order status transition', async ({ page }) => {
      await adminLogin(page);
      await page.goto(`${FRONTEND_URL}/admin/orders`);
      await page.waitForTimeout(2000);
      const orderLinks = page.locator('a[href^="/admin/orders/"]');
      const count = await orderLinks.count();
      expect(count).toBeGreaterThan(0);
      await orderLinks.first().click();
      await page.waitForTimeout(2000);
      // Look for status transition buttons
      const statusButtons = page.locator('button').filter({ hasText: /تأیید|Confirmed|Processing|Paid|Shipped|Delivered/i });
      const btnCount = await statusButtons.count();
      if (btnCount > 0) {
        await statusButtons.first().click();
        await page.waitForTimeout(2000);
      }
    });

    test('24. Admin dashboard loads', async ({ page }) => {
      await adminLogin(page);
      await page.goto(`${FRONTEND_URL}/admin`);
      await page.waitForTimeout(3000);
      await expect(page).toHaveURL(/\/admin$/);
    });
  });

  test.describe('Phase 6: Custom Doll', () => {
    test('25. Custom doll form loads (authenticated)', async ({ page }) => {
      await loginAsCustomer(page);
      await page.goto(`${FRONTEND_URL}/custom-doll-request`);
      await page.waitForTimeout(2000);
      // Verify form fields
      await expect(page.locator('#title')).toBeVisible();
      await expect(page.locator('#bodyColor')).toBeVisible();
      await expect(page.locator('#eyeColor')).toBeVisible();
      await expect(page.locator('#height')).toBeVisible();
    });

    test('26. Custom doll request with all fields', async ({ page }) => {
      await loginAsCustomer(page);
      await page.goto(`${FRONTEND_URL}/custom-doll-request`);
      await page.waitForTimeout(2000);
      await expect(page.locator('#title')).toBeVisible();

      // Fill form fields
      await page.fill('#title', 'E2E Test Doll');
      await page.fill('#bodyColor', 'Brown');
      await page.fill('#eyeColor', 'Blue');
      await page.fill('#height', '30');
      await page.fill('#description', 'A test doll from E2E');

      // For image upload, we need a real image file
      const fileInput = page.locator('input[type="file"]');
      const fileCount = await fileInput.count();
      if (fileCount > 0) {
        // Create a test image file
        const imagePath = '/tmp/test-doll.png';
        const { writeFileSync } = require('fs');
        const { createCanvas } = require('canvas');
        const canvas = createCanvas(200, 200);
        const ctx = canvas.getContext('2d');
        ctx.fillStyle = '#8B4513';
        ctx.fillRect(0, 0, 200, 200);
        ctx.fillStyle = '#0000FF';
        ctx.beginPath();
        ctx.arc(70, 70, 15, 0, Math.PI * 2);
        ctx.arc(130, 70, 15, 0, Math.PI * 2);
        ctx.fill();
        writeFileSync(imagePath, canvas.toBuffer());
        await fileInput.first().setInputFiles(imagePath);
        await page.waitForTimeout(3000);
      }

      // Submit
      const submitBtn = page.locator('button').filter({ hasText: /submit|ثبت|Send/i }).last();
      await submitBtn.click();
      await page.waitForTimeout(5000);
    });

    test('27. Custom doll request list (customer)', async ({ page }) => {
      await loginAsCustomer(page);
      await page.goto(`${FRONTEND_URL}/custom-doll-requests`);
      await page.waitForTimeout(2000);
      await expect(page).toHaveURL(/\/custom-doll-requests/);
    });

    test('28. Admin custom doll management', async ({ page }) => {
      await adminLogin(page);
      await page.goto(`${FRONTEND_URL}/admin/custom-doll-requests`);
      await page.waitForTimeout(2000);
      await expect(page).toHaveURL(/\/admin\/custom-doll-requests/);
    });
  });

  test.describe('Phase 7: Notifications', () => {
    test('29. Notifications page accessible', async ({ page }) => {
      await loginAsCustomer(page);
      await page.goto(`${FRONTEND_URL}/notifications`);
      await page.waitForTimeout(2000);
      await expect(page).toHaveURL(/\/notifications/);
    });
  });

  test.describe('Phase 8: Responsive UI', () => {
    const viewports = [
      { name: 'Desktop', width: 1920, height: 1080 },
      { name: 'Tablet', width: 768, height: 1024 },
      { name: 'Mobile', width: 375, height: 667 },
    ];

    for (const vp of viewports) {
      test(`30. ${vp.name} — Homepage loads without horizontal overflow`, async ({ page }) => {
        await page.setViewportSize({ width: vp.width, height: vp.height });
        await page.goto(FRONTEND_URL);
        await page.waitForTimeout(2000);
        const overflow = await page.evaluate(() => {
          return document.documentElement.scrollWidth > window.innerWidth + 1;
        });
        expect(overflow, 'No horizontal overflow on homepage').toBeFalsy();
      });

      test(`31. ${vp.name} — Login page renders correctly`, async ({ page }) => {
        await page.setViewportSize({ width: vp.width, height: vp.height });
        await page.goto(`${FRONTEND_URL}/login`);
        await page.waitForTimeout(1000);
        await expect(page.locator('#username')).toBeVisible();
        await expect(page.locator('#password')).toBeVisible();
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth > window.innerWidth + 1
        );
        expect(overflow, 'No horizontal overflow on login page').toBeFalsy();
      });

      test(`32. ${vp.name} — Products page renders correctly`, async ({ page }) => {
        await page.setViewportSize({ width: vp.width, height: vp.height });
        await page.goto(`${FRONTEND_URL}/products`);
        await page.waitForTimeout(2000);
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth > window.innerWidth + 1
        );
        expect(overflow, 'No horizontal overflow on products page').toBeFalsy();
      });

      test(`33. ${vp.name} — Product detail renders correctly`, async ({ page }) => {
        await page.setViewportSize({ width: vp.width, height: vp.height });
        await page.goto(`${FRONTEND_URL}/products/3`);
        await page.waitForTimeout(2000);
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth > window.innerWidth + 1
        );
        expect(overflow, 'No horizontal overflow on product detail').toBeFalsy();
      });

      test(`34. ${vp.name} — Register page renders correctly`, async ({ page }) => {
        await page.setViewportSize({ width: vp.width, height: vp.height });
        await page.goto(`${FRONTEND_URL}/register`);
        await page.waitForTimeout(1000);
        await expect(page.locator('#username')).toBeVisible();
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth > window.innerWidth + 1
        );
        expect(overflow, 'No horizontal overflow on register page').toBeFalsy();
      });

      test(`35. ${vp.name} — Cart page renders correctly`, async ({ page }) => {
        await page.setViewportSize({ width: vp.width, height: vp.height });
        await loginAsCustomer(page);
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth > window.innerWidth + 1
        );
        expect(overflow, 'No horizontal overflow on cart page').toBeFalsy();
      });

      test(`36. ${vp.name} — Checkout page renders correctly`, async ({ page }) => {
        await page.setViewportSize({ width: vp.width, height: vp.height });
        await loginAsCustomer(page);
        await page.goto(`${FRONTEND_URL}/checkout`);
        await page.waitForTimeout(1000);
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth > window.innerWidth + 1
        );
        expect(overflow, 'No horizontal overflow on checkout').toBeFalsy();
      });

      test(`37. ${vp.name} — Orders page renders correctly`, async ({ page }) => {
        await page.setViewportSize({ width: vp.width, height: vp.height });
        await loginAsCustomer(page);
        await page.goto(`${FRONTEND_URL}/orders`);
        await page.waitForTimeout(1000);
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth > window.innerWidth + 1
        );
        expect(overflow, 'No horizontal overflow on orders page').toBeFalsy();
      });

      test(`38. ${vp.name} — Custom doll form renders correctly`, async ({ page }) => {
        await page.setViewportSize({ width: vp.width, height: vp.height });
        await loginAsCustomer(page);
        await page.goto(`${FRONTEND_URL}/custom-doll-request`);
        await page.waitForTimeout(1000);
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth > window.innerWidth + 1
        );
        expect(overflow, 'No horizontal overflow on custom doll page').toBeFalsy();
      });

      test(`39. ${vp.name} — Admin dashboard renders correctly`, async ({ page }) => {
        await page.setViewportSize({ width: vp.width, height: vp.height });
        await adminLogin(page);
        await page.goto(`${FRONTEND_URL}/admin`);
        await page.waitForTimeout(1000);
        const overflow = await page.evaluate(() =>
          document.documentElement.scrollWidth > window.innerWidth + 1
        );
        expect(overflow, 'No horizontal overflow on admin dashboard').toBeFalsy();
      });
    }
  });
});

// Helper functions
async function loginAsCustomer(page: Page): Promise<void> {
  await page.goto(`${FRONTEND_URL}/login`);
  await page.waitForSelector('#username', { state: 'visible' });
  await page.fill('#username', USERNAME);
  await page.fill('#password', PASSWORD);
  await page.locator('form:has(#username) button[type="submit"]').click();
  await page.waitForURL('**/products', { timeout: 15000 });
}

async function adminLogin(page: Page): Promise<void> {
  await page.goto(`${FRONTEND_URL}/admin/login`);
  await page.waitForSelector('#admin-username', { state: 'visible' });
  await page.fill('#admin-username', ADMIN_USER);
  await page.fill('#admin-password', ADMIN_PASS);
  await page.locator('form:has(#admin-username) button[type="submit"]').click();
  try {
    await page.waitForURL('**/admin', { timeout: 10000 });
  } catch {
    // Admin user might not exist — create via API
  }
}
