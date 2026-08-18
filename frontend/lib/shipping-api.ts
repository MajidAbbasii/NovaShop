import { API_GATEWAY_URL } from '@/lib/config';

export interface ShippingSettings {
  courierPrice: number;
  postPrice: number;
  postFreeShippingThreshold: number;
  pickupPrice: number;
}

export interface ShippingMethod {
  method: string;
  displayKey: string;
  price: number;
  isFree: boolean;
}

export interface ShippingMethods {
  methods: ShippingMethod[];
  postFreeShippingThreshold: number;
}

/** Admin: current shipping settings (Courier / Post / Pickup prices in Toman). */
export async function getShippingSettings(): Promise<ShippingSettings> {
  const res = await fetch(`${API_GATEWAY_URL}/api/admin/shipping-settings`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error('Failed to load shipping settings');
  return res.json();
}

/** Admin: persist shipping settings. */
export async function updateShippingSettings(
  settings: ShippingSettings
): Promise<ShippingSettings> {
  const res = await fetch(`${API_GATEWAY_URL}/api/admin/shipping-settings`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(settings),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => null);
    throw new Error(err?.error ?? err?.errors?.join(', ') ?? 'Failed to save shipping settings');
  }
  return res.json();
}

/** Customer: available shipping methods with current server-side rates. */
export async function getShippingMethods(): Promise<ShippingMethods> {
  const res = await fetch(`${API_GATEWAY_URL}/api/shipping-methods`, {
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
  });
  if (!res.ok) throw new Error('Failed to load shipping methods');
  return res.json();
}

function authHeaders(): Record<string, string> {
  if (typeof document === 'undefined') return {};
  const match = document.cookie.match(/(?:^|;\s*)token=([^;]*)/);
  const token = match ? decodeURIComponent(match[1]) : null;
  return token ? { Authorization: `Bearer ${token}` } : {};
}
