/* Shared health/geometry for the storefront design system (globals.css). */
/* Kept as a component-level utility module so pages import one tiny helper. */

/** Discount percentage from original → current price. 0 if none. */
export function discountPercent(price: number, originalPrice?: number | null): number {
  if (!originalPrice || originalPrice <= price || price <= 0) return 0;
  return Math.round(((originalPrice - price) / originalPrice) * 100);
}

/** Picsum seed fallback for demo products without images. */
export function fallbackImage(seed: string | number): string {
  return `https://picsum.photos/seed/${seed}/400/400`;
}