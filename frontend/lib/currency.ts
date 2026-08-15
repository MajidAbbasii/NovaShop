/**
 * Centralized currency handling — NO conversions.
 *
 * The entire application stores, transfers, and displays Iranian Toman (IRT).
 * Values are native Toman end-to-end (seed data, DB columns, DTOs, API,
 * cart/order/discount math, display). There is no internal unit to convert.
 *
 * If a payment gateway ever requires a different unit (e.g. Rial), convert
 * ONLY inside that provider's integration layer — never in shared code.
 */
export const CURRENCY_CODE = 'IRT';

/** Localized currency label. Persian & Arabic use native script, English uses Latin. */
export function currencyLabel(locale: string): string {
  if (locale === 'en') return 'Toman';
  return 'تومان';
}

/** Toman display formatter. `locale` drives digit grouping (e.g. fa-IR). */
export function formatToman(amount: number, locale: string): string {
  if (!Number.isFinite(amount)) return String(amount);
  return new Intl.NumberFormat(locale, {
    maximumFractionDigits: 0,
  }).format(amount) + ` ${currencyLabel(locale)}`;
}