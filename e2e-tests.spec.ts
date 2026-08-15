import { test, expect } from '@playwright/test';

const FRONTEND_URL = 'http://localhost:3000';
const API_URL = 'http://localhost:5000';

test.describe('NovaShop Full E2E Flow', () => {
  let token: string;

  test('1. Homepage loads and shows products', async ({ page }) => {
    await page.goto(FRONTEND_URL);
    await expect(page).toHaveTitle(/NovaShop/);
    // Hero section visible
    await expect(page.getByText('Discover Handmade')).toBeVisible();
    await expect(page.getByText('Shop Collection')).toBeVisible();
    // Featured products section
    await expect(page.getByText('Featured Dolls')).toBeVisible();
  });

  test('2. Products page shows catalog', async ({ page }) => {
    await page.goto(`${FRONTEND_URL}/products`);
    await page.waitForTimeout(2000);
    // Should see product cards
    const productLinks = page.locator('a[href^="/products/"]');
    const count = await productLinks.count();
    expect(count).toBeGreaterThan(0);
  });

  test('3. Product detail page loads', async ({ page }) => {
    await page.goto(`${FRONTEND_URL}/products/3`);
    await page.waitForTimeout(2000);
    // Product name visible
    await expect(page.getByText('Handmade Bunny Doll')).toBeVisible();
    // Price visible
    await expect(page.getByText(/34\.99/)).toBeVisible();
    // Add to Cart button visible
    const addBtn = page.getByRole('button', { name: /Add to Cart/i });
    await expect(addBtn).toBeVisible();
  });

  test('4. Register a new user', async ({ page }) => {
    await page.goto(`${FRONTEND_URL}/register`);
    await page.waitForTimeout(1000);
    
    const username = `testuser_${Date.now()}`;
    await page.fill('#username', username);
    await page.fill('#email', `${username}@test.com`);
    await page.fill('#password', 'test123');
    await page.click('button[type="submit"]');
    
    // After registration, should redirect to products
    await page.waitForURL('**/products');
    
    // Check auth cookie was set
    const cookies = await page.context().cookies();
    const tokenCookie = cookies.find(c => c.name === 'token');
    expect(tokenCookie).toBeTruthy();
    token = tokenCookie!.value;
  });

  test('5. Add product to cart', async ({ page, context }) => {
    // Ensure we have a token
    if (!token) {
      // Register first
      await page.goto(`${FRONTEND_URL}/register`);
      const username = `cartuser_${Date.now()}`;
      await page.fill('#username', username);
      await page.fill('#email', `${username}@test.com`);
      await page.fill('#password', 'test123');
      await page.click('button[type="submit"]');
      await page.waitForURL('**/products');
      const cookies = await context.cookies();
      token = cookies.find(c => c.name === 'token')?.value || '';
    }

    // Go to product detail
    await page.goto(`${FRONTEND_URL}/products/3`);
    await page.waitForTimeout(2000);
    
    // Click Add to Cart
    const addBtn = page.getByRole('button', { name: /Add to Cart/i });
    await addBtn.click();
    await page.waitForTimeout(1500);
    
    // Cart sheet should show the item
    // The cart icon should update
    await page.goto(`${FRONTEND_URL}/products`);
    await page.waitForTimeout(1000);
  });

  test('6. Checkout flow', async ({ page, context }) => {
    // Ensure authenticated
    if (!token) {
      await page.goto(`${FRONTEND_URL}/register`);
      const username = `checkoutuser_${Date.now()}`;
      await page.fill('#username', username);
      await page.fill('#email', `${username}@test.com`);
      await page.fill('#password', 'test123');
      await page.click('button[type="submit"]');
      await page.waitForURL('**/products');
      const cookies = await context.cookies();
      token = cookies.find(c => c.name === 'token')?.value || '';
    }

    // Add product to cart via API directly
    const res = await page.request.post(`${API_URL}/api/cart`, {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      data: {
        productId: 4,
        quantity: 1
      }
    });
    expect(res.ok()).toBeTruthy();

    // Go to checkout page
    await page.goto(`${FRONTEND_URL}/checkout`);
    await page.waitForTimeout(2000);
    
    // Should see the checkout form
    await expect(page.getByText('Shipping Information')).toBeVisible();
    await expect(page.getByText('Order Summary')).toBeVisible();
    
    // Fill shipping info
    await page.fill('#fullName', 'John Doe');
    await page.fill('#email', 'john@example.com');
    await page.fill('#phone', '123-456-7890');
    await page.fill('#address', '123 Main St');
    await page.fill('#city', 'Tehran');
    await page.fill('#postalCode', '12345');
    
    // Submit order
    await page.click('button[type="submit"]');
    
    // Should redirect to order confirmation
    await page.waitForURL('**/orders/**', { timeout: 10000 });
    
    // Should see success message
    await expect(page.getByText(/Thank You/i)).toBeVisible();
    await expect(page.getByText(/order #/i)).toBeVisible();
  });

  test('7. Login works', async ({ page }) => {
    // First register a user via API
    const username = `logintest_${Date.now()}`;
    const registerRes = await page.request.post(`${API_URL}/api/auth/register`, {
      headers: { 'Content-Type': 'application/json' },
      data: { username, email: `${username}@test.com`, password: 'test123' }
    });
    expect(registerRes.ok()).toBeTruthy();

    // Now login
    await page.goto(`${FRONTEND_URL}/login`);
    await page.waitForTimeout(1000);
    
    await page.fill('#username', username);
    await page.fill('#password', 'test123');
    await page.click('button[type="submit"]');
    
    // Should redirect to products
    await page.waitForURL('**/products');
    
    // Check cookie
    const cookies = await page.context().cookies();
    expect(cookies.find(c => c.name === 'token')).toBeTruthy();
  });
});
