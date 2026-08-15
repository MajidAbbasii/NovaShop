import { useLocale } from './locale-context';
import { formatToman } from './currency';

export const ORDER_STATUSES = [
  'Pending',
  'Confirmed',
  'Processing',
  'Paid',
  'Shipped',
  'Delivered',
  'Cancelled',
  'Failed',
] as const;

export const VALID_TRANSITIONS: Record<string, string[]> = {
  Pending: ['Confirmed', 'Paid', 'Cancelled', 'Failed'],
  Confirmed: ['Processing', 'Paid', 'Cancelled', 'Failed'],
  Processing: ['Paid', 'Cancelled', 'Failed'],
  Paid: ['Shipped', 'Cancelled'],
  Shipped: ['Delivered', 'Cancelled'],
  Delivered: [],
  Cancelled: [],
  Failed: [],
};

/** Map an order-status key to a translation key: status.pending, status.confirmed, ... */
export function statusKey(status: string): string {
  const map: Record<string, string> = {
    Pending: 'pending',
    Confirmed: 'confirmed',
    Processing: 'processing',
    Paid: 'paid',
    Shipped: 'shipped',
    Delivered: 'delivered',
    Cancelled: 'cancelled',
    Failed: 'failed',
  };
  return `status.${map[status] ?? status.toLowerCase()}`;
}

export function discountTypeKey(type: string): string {
  return type === 'Percentage' ? 'admin.discounts.percentage' : 'admin.discounts.fixed';
}

function localeOf(): string {
  try {
    // Intl best-match for the current locale state; falls back to fa-IR.
    if (typeof document !== 'undefined' && document.documentElement.lang) {
      const lang = document.documentElement.lang;
      if (lang === 'en') return 'en-US';
      if (lang === 'ar') return 'ar-EG';
    }
  } catch {
    /* ignore */
  }
  return 'fa-IR';
}

export function formatCurrency(n: number, lang?: string): string {
  // Values are native Toman (see ./currency).
  return formatToman(n, lang ?? localeOf());
}

export function formatNumber(n: number, lang?: string): string {
  return new Intl.NumberFormat(lang ?? localeOf()).format(n);
}

export function formatDate(iso: string, lang?: string): string {
  try {
    return new Intl.DateTimeFormat(lang ?? localeOf(), {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(iso));
  } catch {
    return iso;
  }
}

export function formatDateShort(iso: string, lang?: string): string {
  try {
    return new Intl.DateTimeFormat(lang ?? localeOf(), {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    }).format(new Date(iso));
  } catch {
    return iso;
  }
}

export function toPersianDigits(s: string | number): string {
  return String(s).replace(/[0-9]/g, (d) => '۰۱۲۳۴۵۶۷۸۹'[Number(d)]);
}

/**
 * Backwards-compatible stand-in for the old Persian-only label maps.
 * Prefer t(statusKey(status)) / t(discountTypeKey(type)) in components.
 */
export const ORDER_STATUS_LABELS: Record<string, string> = {
  Pending: 'در انتظار',
  Confirmed: 'تأیید شده',
  Processing: 'در حال پردازش',
  Paid: 'پرداخت شده',
  Shipped: 'ارسال شده',
  Delivered: 'تحویل شده',
  Cancelled: 'لغو شده',
  Failed: 'ناموفق',
};

export const DISCOUNT_TYPE_LABELS: Record<string, string> = {
  Percentage: 'درصدی',
  Fixed: 'مبلغ ثابت',
};

/**
 * Locale-bound formatters for admin components.
 * Use: const { currency, dateShort, ... } = useAdminFormatters();
 */
export function useAdminFormatters() {
  const { locale } = useLocale();
  return {
    locale,
    currency: (n: number) => formatCurrency(n, locale),
    number: (n: number) => formatNumber(n, locale),
    date: (iso: string) => formatDate(iso, locale),
    dateShort: (iso: string) => formatDateShort(iso, locale),
  };
}