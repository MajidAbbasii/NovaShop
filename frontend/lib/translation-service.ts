'use client';

import { API_GATEWAY_URL } from './config';
import type { Locale, TranslationMap } from './translations';

// The Backend database is the SINGLE SOURCE OF TRUTH for UI translations.
// This client loads the full dictionary per-locale in bulk (one request) and
// caches it. There is intentionally NO static/full dictionary fallback: if the
// API is unreachable we surface the translation key (or a previously cached
// value) rather than maintaining a second copy of the translation database.

const memoryCache = new Map<Locale, Record<string, string>>();
const inFlight = new Map<Locale, Promise<Record<string, string>>>();

export type { TranslationMap };

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
      // 3) persisted cache (last good values, if any)
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
      // 4) last resort: empty map. The Backend remains authoritative; components
      //    render the key itself (never a full static dictionary).
      const empty: TranslationMap = {};
      memoryCache.set(locale, empty);
      return empty;
    } finally {
      inFlight.delete(locale);
    }
  })();

  inFlight.set(locale, p);
  return p;
}

/** Synchronous read of whatever is currently in the cache (may be empty). */
export function getCachedTranslations(locale: Locale): TranslationMap {
  return memoryCache.get(locale) ?? {};
}

/**
 * Prime the cache from any previously persisted (last-good) value so the very
 * first client render has something to show before the API responds. This is
 * NOT the static dictionary — it is the last successful backend response.
 */
export function primeFromPersisted(locale: Locale): TranslationMap {
  try {
    const stored = localStorage.getItem(`novashop-translations-${locale}`);
    if (stored) {
      const map = JSON.parse(stored) as TranslationMap;
      if (!memoryCache.has(locale)) memoryCache.set(locale, map);
      return map;
    }
  } catch {
    /* ignore */
  }
  return {};
}
