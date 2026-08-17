'use client';

import { API_GATEWAY_URL } from './config';
import { translations as fallbackTranslations, type Locale } from './translations';

// Single authoritative source for UI translations is now the Backend.
// This client loads the full dictionary per-locale in bulk (one request) and
// caches it. The static `translations` import is used ONLY as an offline/dev
// fallback so the UI never renders raw keys when the API is unreachable.

const memoryCache = new Map<Locale, Record<string, string>>();
const inFlight = new Map<Locale, Promise<Record<string, string>>>();

export type TranslationMap = Record<string, string>;

async function fetchLocale(locale: Locale): Promise<TranslationMap> {
  const res = await fetch(`${API_GATEWAY_URL}/api/translations?locale=${locale}`, {
    headers: { Accept: 'application/json' },
    cache: 'no-store',
  });
  if (!res.ok) throw new Error(`translations ${res.status}`);
  const data = (await res.json()) as { locale: string; translations: TranslationMap };
  return data.translations ?? {};
}

export async function loadTranslations(locale: Locale): Promise<TranslationMap> {
  // 1) memory cache
  const cached = memoryCache.get(locale);
  if (cached) return cached;

  // 2) dedup concurrent requests
  const existing = inFlight.get(locale);
  if (existing) return existing;

  const p = (async () => {
    try {
      const map = await fetchLocale(locale);
      memoryCache.set(locale, map);
      try {
        localStorage.setItem(`novashop-translations-${locale}`, JSON.stringify(map));
      } catch {
        /* ignore quota errors */
      }
      return map;
    } catch {
      // 3) persisted cache
      try {
        const stored = localStorage.getItem(`novashop-translations-${locale}`);
        if (stored) {
          const map = JSON.parse(stored) as TranslationMap;
          memoryCache.set(locale, map);
          return map;
        }
      } catch {
        /* ignore */
      }
      // 4) static fallback
      const fb = (fallbackTranslations[locale] ?? {}) as TranslationMap;
      memoryCache.set(locale, fb);
      return fb;
    } finally {
      inFlight.delete(locale);
    }
  })();

  inFlight.set(locale, p);
  return p;
}

/** Synchronous read of whatever is currently in the cache (may be fallback). */
export function getCachedTranslations(locale: Locale): TranslationMap {
  return memoryCache.get(locale) ?? ((fallbackTranslations[locale] ?? {}) as TranslationMap);
}

export function primeFromStatic(locale: Locale): TranslationMap {
  const fb = (fallbackTranslations[locale] ?? {}) as TranslationMap;
  if (!memoryCache.has(locale)) memoryCache.set(locale, fb);
  return fb;
}
