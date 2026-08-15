import { API_GATEWAY_URL } from '@/lib/config';

const BACKEND_URL = API_GATEWAY_URL;

export interface CartItemDto {
  id: number;
  productId: number;
  productName: string;
  imageUrl: string;
  productColorId?: number | null;
  colorName?: string;
  unitPrice: number;
  quantity: number;
}

export interface CartDto {
  id: number;
  userId: number;
  totalAmount: number;
  items: CartItemDto[];
}

async function authFetch(url: string, options?: RequestInit): Promise<Response> {
  return fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...authHeaders(),
      ...options?.headers,
    },
  });
}

export async function fetchCart(): Promise<CartDto> {
  const res = await authFetch(`${BACKEND_URL}/api/cart`);
  if (!res.ok) throw new Error('Failed to fetch cart');
  return res.json();
}

export async function addToCart(productId: number, quantity: number = 1, productColorId?: number | null): Promise<void> {
  const res = await authFetch(`${BACKEND_URL}/api/cart`, {
    method: 'POST',
    body: JSON.stringify({ productId, quantity, productColorId: productColorId ?? null }),
  });
  if (!res.ok) throw new Error('Failed to add to cart');
}

export async function updateCartItemQuantity(cartItemId: number, quantity: number): Promise<void> {
  const res = await authFetch(`${BACKEND_URL}/api/cart/items/${cartItemId}`, {
    method: 'PUT',
    body: JSON.stringify({ quantity }),
  });
  if (!res.ok) throw new Error('Failed to update cart item');
}

export async function removeCartItem(cartItemId: number): Promise<void> {
  const res = await authFetch(`${BACKEND_URL}/api/cart/items/${cartItemId}`, {
    method: 'DELETE',
  });
  if (!res.ok && res.status !== 204) throw new Error('Failed to remove cart item');
}

export async function clearCart(): Promise<void> {
  const userId = getCurrentUserId();
  if (!userId) return;
  const res = await authFetch(`${BACKEND_URL}/api/cart?userId=${userId}`, {
    method: 'DELETE',
  });
  if (!res.ok) throw new Error('Failed to clear cart');
}

/** Compute subtotal from items (sum of unitPrice * quantity) */
export function computeSubtotal(items: CartItemDto[]): number {
  return items.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0);
}

/** Check if user is authenticated */
export function isAuthenticated(): boolean {
  return hasToken();
}

// Token helpers needed by authFetch
function getToken(): string | null {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/(?:^|;\s*)token=([^;]*)/);
  return match ? decodeURIComponent(match[1]) : null;
}

function decodeToken(token: string): { sub: number; role: string } | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return { sub: Number(payload.sub), role: payload.role ?? '' };
  } catch {
    return null;
  }
}

export function getCurrentUserId(): number | null {
  const token = getToken();
  if (!token) return null;
  return decodeToken(token)?.sub ?? null;
}

export function hasToken(): boolean {
  return !!getToken();
}

export function authHeaders(): Record<string, string> {
  const token = getToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}
