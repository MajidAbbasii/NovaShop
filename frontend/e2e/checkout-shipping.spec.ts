import { test, expect } from '@playwright/test';

/**
 * E2E: server-side shipping cost authority (API-level, no browser binary needed).
 * Verifies the backend quote/order endpoints compute shipping from trusted
 * server-side values and ignore any client-supplied monetary fields.
 */

// Register a fresh user via the gateway, then log in to obtain a JWT.
async function registerAndLogin(request): Promise<string> {
  const phone = `0913${String(Date.now()).slice(0, 9)}`;
  await request.post('http://localhost:5100/api/auth/register', {
    data: { username: phone, phoneNumber: phone, password: 'Str0ngP@ss!', confirmPassword: 'Str0ngP@ss!' },
  });
  const login = await request.post('http://localhost:5100/api/auth/login', {
    data: { username: phone, password: 'Str0ngP@ss!' },
  });
  const body = await login.json();
  return body.token as string;
}

async function addToCart(request, token: string, productId = 5, quantity = 1) {
  await request.post('http://localhost:5100/api/cart', {
    data: { productId, quantity },
    headers: { Authorization: `Bearer ${token}` },
  });
}

test.describe('Checkout shipping cost is server-authoritative', () => {
  let token: string;

  test.beforeAll(async ({ request }) => {
    token = await registerAndLogin(request);
  });

  test('quote returns server-calculated shipping per method', async ({ request }) => {
    await addToCart(request, token);
    for (const [method, expected] of [['POST', 59900], ['COURIER', 129000], ['PICKUP', 0]] as const) {
      const q = await request.post('http://localhost:5100/api/orders/quote', {
        data: { shippingMethod: method },
        headers: { Authorization: `Bearer ${token}` },
      });
      expect(q.ok(), `${method} quote should succeed`).toBeTruthy();
      const j = await q.json();
      expect(j.shippingCost).toBe(expected);
      expect(j.shippingMethod).toBe(method);
      expect(j.isFreeShipping).toBe(expected === 0);
    }
  });

  test('quote rejects invalid shipping method', async ({ request }) => {
    await addToCart(request, token);
    const q = await request.post('http://localhost:5100/api/orders/quote', {
      data: { shippingMethod: 'BOGUS' },
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(q.status()).toBe(400);
  });

  test('order creation ignores client-supplied shippingCost (tampering)', async ({ request }) => {
    await addToCart(request, token);
    // COURIER with shippingCost:0 → backend must still compute 129000
    const res = await request.post('http://localhost:5100/api/orders', {
      data: {
        shippingAddress: '123 Main St, City, 12345',
        paymentMethod: 'InPerson',
        shippingMethod: 'COURIER',
        shippingCost: 0,      // tamper attempt
        grandTotal: 1,        // tamper attempt
        totalAmount: 1,       // tamper attempt
      },
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(res.ok()).toBeTruthy();
    const order = await res.json();
    expect(order.shippingMethod).toBe('COURIER');
    expect(order.shippingCost).toBe(129000);                 // NOT 0
    expect(order.totalAmount).toBe(289900 + 129000);         // 418900, NOT 1
    expect(order.payment.amount).toBe(418900);
    expect(order.originalTotal).toBe(order.totalAmount);
  });

  test('order creation ignores shippingCost=999999999 on POST (free threshold)', async ({ request }) => {
    // Add 4 units of product 5 (289,900 x4 = 1,159,600 >= 500k => free POST)
    await addToCart(request, token, 5, 4);
    const res = await request.post('http://localhost:5100/api/orders', {
      data: {
        shippingAddress: '123 Main St, City, 12345',
        paymentMethod: 'InPerson',
        shippingMethod: 'POST',
        shippingCost: 999999999,
        grandTotal: 0,
      },
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(res.ok()).toBeTruthy();
    const order = await res.json();
    expect(order.shippingMethod).toBe('POST');
    expect(order.shippingCost).toBe(0);        // free (threshold met), NOT 999999999
  });

  test('order snapshot persists server-side computed values', async ({ request }) => {
    await addToCart(request, token);
    const res = await request.post('http://localhost:5100/api/orders', {
      data: {
        shippingAddress: '123 Main St, City, 12345',
        paymentMethod: 'InPerson',
        shippingMethod: 'PICKUP',
      },
      headers: { Authorization: `Bearer ${token}` },
    });
    const order = await res.json();
    const id = order.id;
    // fetch order detail again — snapshot must be unchanged
    const get = await request.get(`http://localhost:5100/api/orders/${id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const fetched = await get.json();
    expect(fetched.shippingMethod).toBe('PICKUP');
    expect(fetched.shippingCost).toBe(0);
    expect(fetched.totalAmount).toBe(289900);
  });
});
