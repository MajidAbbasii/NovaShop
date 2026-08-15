/**
 * Single API entry point for the entire frontend.
 * ALL API requests go through the API Gateway (YARP reverse proxy).
 * Never call backend services directly.
 */
export const API_GATEWAY_URL =
  process.env.NEXT_PUBLIC_API_GATEWAY_URL ?? 'http://localhost:5100';

/** Prefix gateway for relative image paths; absolute URLs pass through. */
export const resolveImageUrl = (url?: string | null): string =>
  url && !/^https?:\/\//i.test(url) ? `${API_GATEWAY_URL}${url}` : (url ?? '');
