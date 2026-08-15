import { useLocale } from './locale-context';
import { formatToman } from './currency';

/** Currency display — values are native Toman (see ./currency). */
export function formatCurrency(amount: number, locale: string): string {
  return formatToman(amount, locale);
}

export function formatDate(date: Date | string, locale: string): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  if (isNaN(d.getTime())) return String(date);
  return new Intl.DateTimeFormat(locale, {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(d);
}

export function formatDateShort(date: Date | string, locale: string): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  if (isNaN(d.getTime())) return String(date);
  return new Intl.DateTimeFormat(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(d);
}

export function formatNumber(number: number | string, locale: string): string {
  const n = typeof number === 'string' ? Number(number) : number;
  return new Intl.NumberFormat(locale, {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(n);
}

/** Convert latin (ASCII) digits to Eastern Arabic-Indic digits (used by fa & ar). */
export function toNativeDigits(str: string | number, locale: string): string {
  const s = String(str);
  if (locale === 'en') return s;
  const map: Record<string, string> = locale === 'fa'
    ? { '0': '۰', '1': '۱', '2': '۲', '3': '۳', '4': '۴', '5': '۵', '6': '۶', '7': '۷', '8': '۸', '9': '۹' }
    : { '0': '٠', '1': '١', '2': '٢', '3': '٣', '4': '٤', '5': '٥', '6': '٦', '7': '٧', '8': '٨', '9': '٩' };
  return s.replace(/[0-9]/g, (d) => map[d]);
}

/**
 * Tiny hook that binds the above helpers to the active locale.
 * Use in client components: const { fCurrency, fDate } = useFormatters();
 */
export function useFormatters() {
  const { locale } = useLocale();
  return {
    locale,
    currency: (n: number) => formatCurrency(n, locale),
    date: (d: Date | string) => formatDate(d, locale),
    dateShort: (d: Date | string) => formatDateShort(d, locale),
    number: (n: number | string) => formatNumber(n, locale),
    native: (s: string | number) => toNativeDigits(s, locale),
  };
}