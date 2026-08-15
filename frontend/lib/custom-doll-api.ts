import { API_GATEWAY_URL } from "@/lib/config";
import { authHeaders } from "@/lib/cart-api";

export type CustomDollStatus = "PendingReview" | "Approved" | "Rejected" | "CustomerAccepted";

export interface CustomDollRequest {
  id: number;
  imageUrl: string;
  description: string;
  status: CustomDollStatus;
  price: number | null;
  currency: string;
  adminMessage: string | null;
  createdAt: string;
  updatedAt: string | null;
  reviewedAt: string | null;
}

export interface AdminCustomDollRequest extends CustomDollRequest {
  userId: number;
  customerUsername: string;
  customerPhone: string;
  customerEmail?: string | null;
  reviewedBy: number | null;
}

export async function createCustomDollRequest(
  imageUrl: string,
  description: string
): Promise<number> {
  const res = await fetch(`${API_GATEWAY_URL}/api/custom-doll-requests`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ imageUrl, description }),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new Error(data?.message || "Failed to create request");
  }
  return res.json();
}

export async function getMyCustomDollRequests(): Promise<CustomDollRequest[]> {
  const res = await fetch(`${API_GATEWAY_URL}/api/custom-doll-requests?pageSize=50`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error("Failed to load requests");
  const data = await res.json();
  return data.items ?? [];
}

export async function getMyCustomDollRequest(id: number): Promise<CustomDollRequest> {
  const res = await fetch(`${API_GATEWAY_URL}/api/custom-doll-requests/${id}`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error("Failed to load request");
  return res.json();
}

export async function getAdminCustomDollRequests(): Promise<AdminCustomDollRequest[]> {
  const res = await fetch(`${API_GATEWAY_URL}/api/admin/custom-doll-requests?pageSize=100`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error("Failed to load requests");
  const data = await res.json();
  return data.items ?? [];
}

export async function getAdminCustomDollRequest(id: number): Promise<AdminCustomDollRequest> {
  const res = await fetch(`${API_GATEWAY_URL}/api/admin/custom-doll-requests/${id}`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error("Failed to load request");
  return res.json();
}

export async function approveCustomDollRequest(
  id: number,
  price: number,
  adminMessage: string
): Promise<void> {
  const res = await fetch(`${API_GATEWAY_URL}/api/admin/custom-doll-requests/${id}/approve`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ price, adminMessage }),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new Error(data?.message || "Failed to approve");
  }
}

export async function rejectCustomDollRequest(
  id: number,
  adminMessage: string
): Promise<void> {
  const res = await fetch(`${API_GATEWAY_URL}/api/admin/custom-doll-requests/${id}/reject`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ adminMessage }),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new Error(data?.message || "Failed to reject");
  }
}

export async function acceptCustomDollRequest(id: number): Promise<void> {
  const res = await fetch(`${API_GATEWAY_URL}/api/custom-doll-requests/${id}/accept`, {
    method: "POST",
    headers: authHeaders(),
  });
  if (!res.ok) {
    const data = await res.json().catch(() => null);
    throw new Error(data?.message || "Failed to accept");
  }
}
