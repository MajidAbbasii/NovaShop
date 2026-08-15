import { API_GATEWAY_URL } from '@/lib/config';

const API_BASE_URL = API_GATEWAY_URL;

export interface ReviewDto {
  id: number;
  productId: number;
  userId: number;
  userName?: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface CreateReviewRequest {
  productId: number;
  userId: number;
  rating: number;
  comment: string;
}

// --- Token helpers ---

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
  const token =
    typeof window !== 'undefined'
      ? (localStorage.getItem('token') || document.cookie.match(/(?:^|;\s*)token=([^;]*)/)?.[1])
      : null;
  return token ? { Authorization: `Bearer ${token}` } : {};
}

// --- Review API ---

export async function getProductReviews(productId: number): Promise<ReviewDto[]> {
  const res = await fetch(`${API_BASE_URL}/api/products/${productId}/reviews`, {
    cache: 'no-store',
  });
  if (!res.ok) throw new Error(`Failed to fetch reviews: ${res.status}`);
  return res.json();
}

export async function createReview(data: CreateReviewRequest): Promise<number> {
  const res = await fetch(`${API_BASE_URL}/api/reviews`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `Failed to create review: ${res.status}`);
  }
  return res.json();
}

export async function deleteReview(reviewId: number): Promise<void> {
  const res = await fetch(`${API_BASE_URL}/api/reviews/${reviewId}`, {
    method: 'DELETE',
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(`Failed to delete review: ${res.status}`);
}
