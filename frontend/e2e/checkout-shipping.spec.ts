import { test, expect, type APIRequestContext } from '@playwright/test';

/**
 * E2E: server-side shipping cost authority (API-level, no browser binary needed).
 * Verifies the backend quote/order endpoints compute shipping from trusted
 * server-side values and ignore any client-supplied monetary fields.
 *
 * Products are discovered dynamically from the gateway so the suite does not
 * depend on a hard-coded product id that may not exist in a given dataset.
 */

const GW = 'http://localhost:5100';

interface ProductSummary {
  id: number;
  price: number;
}

async function registerAndLogin(request: APIRequestContext): Promise<string> {
  // Valid Iranian mobile: "09" + 9 digits (11 total).
  const phone = `09${String(Date.now()).slice(-9)}`;
  await request.post(`${GW}/api/auth/register`, {
    data: { username: phone, phoneNumber: phone, password: 'Str0ngP@ss!', confirmPassword: 'Str0ngP@ss!' },
  });
  const login = await request.post(`${GW}/api/auth/login`, {
    data: { username: phone, password: 'Str0ngP@ss!' },
  });
  const body = await login.json();
  return body.token as string;
}

async function addToCart(request: APIRequestContext, token: string, productId: number, quantity = 1): Promise<void> {
  await request.post(`${GW}/api/cart`, {
    data: { productId, quantity },
    headers: { Authorization: `Bearer ${token}` },
  });
}

async function clearCart(request: APIRequestContext, token: string): Promise<void> {
  const res = await request.get(`${GW}/api/cart`, { headers: { Authorization: `Bearer ${token}` } });
  const body = await res.json() as { items?: { id: number }[] };
  for (const item of body.items ?? []) {
    await request.delete(`${GW}/api/cart/items/${item.id}`, { headers: { Authorization: `Bearer ${token}` } });
  }
}

async function discoverProduct(request: APIRequestContext): Promise<ProductSummary> {
  const res = await request.get(`${GW}/api/products?pageSize=1`);
  const body = await res.json();
  const item = body.items[0] as ProductSummary;
  return { id: item.id, price: item.price };
}

const SHIPPING = { POST: 'POST', COURIER: 'COURIER', PICKUP: 'PICKUP' } as const;
type Method = typeof SHIPPING[keyof typeof SHIPPING];

test.describe('Checkout shipping cost is server-authoritative', () => {
  let token: string;
  let product: ProductSummary;
  let prices: Record<string, number>;
  let postFreeThreshold: number;

  test.beforeAll(async ({ request }) => {
    token = await registerAndLogin(request);
    product = await discoverProduct(request);
    const sm = await (await request.get(`${GW}/api/shipping-methods`, { headers: { Authorization: `Bearer ${token}` } })).json() as ShippingMethods;
    prices = sm.methods.reduce<Record<string, number>>((acc, m) => { acc[m.method] = m.price; return acc; }, {});
    postFreeThreshold = sm.postFreeShippingThreshold;
  });

  test('quote returns server-calculated shipping per method', async ({ request }) => {
    await clearCart(request, token);
    await addToCart(request, token, product.id);
    for (const method of [SHIPPING.POST, SHIPPING.COURIER, SHIPPING.PICKUP] as const) {
      const q = await request.post(`${GW}/api/orders/quote`, {
        data: { shippingMethod: method },
        headers: { Authorization: `Bearer ${token}` },
      });
      expect(q.ok(), `${method} quote should succeed`).toBeTruthy();
      const j = await q.json();
      expect(j.shippingCost).toBe(prices[method]);
      expect(j.shippingMethod).toBe(method);
      expect(j.isFreeShipping).toBe(prices[method] === 0);
    }
  });

  test('quote rejects invalid shipping method', async ({ request }) => {
    await clearCart(request, token);
    await addToCart(request, token, product.id);
    const q = await request.post(`${GW}/api/orders/quote`, {
      data: { shippingMethod: 'BOGUS' },
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(q.status()).toBe(400);
  });

  test('order creation ignores client-supplied shippingCost (tampering)', async ({ request }) => {
    await clearCart(request, token);
    await addToCart(request, token, product.id);
    const res = await request.post(`${GW}/api/orders`, {
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
    const expectedTotal = product.price + prices['COURIER'];
    expect(order.shippingMethod).toBe('COURIER');
    expect(order.shippingCost).toBe(prices['COURIER']);       // NOT 0
    expect(order.totalAmount).toBe(expectedTotal);          // NOT 1
    expect(order.payment.amount).toBe(expectedTotal);
    expect(order.originalTotal).toBe(order.totalAmount);
  });

  test('order creation ignores shippingCost=999999999 on POST (free threshold)', async ({ request }) => {
    await clearCart(request, token);
    // Add enough units so subtotal >= free-shipping threshold for POST.
    const qty = Math.max(1, Math.ceil((postFreeThreshold + 1) / product.price));
    await addToCart(request, token, product.id, qty);
    const res = await request.post(`${GW}/api/orders`, {
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
    await clearCart(request, token);
    await addToCart(request, token, product.id);
    const res = await request.post(`${GW}/api/orders`, {
      data: {
        shippingAddress: '123 Main St, City, 12345',
        paymentMethod: 'InPerson',
        shippingMethod: 'PICKUP',
      },
      headers: { Authorization: `Bearer ${token}` },
    });
    const order = await res.json();
    const id = order.id;
    const get = await request.get(`${GW}/api/orders/${id}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const fetched = await get.json();
    expect(fetched.shippingMethod).toBe('PICKUP');
    expect(fetched.shippingCost).toBe(prices['PICKUP']);
    expect(fetched.totalAmount).toBe(product.price);
  });
});
