'use client';

import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import {
  translations,
  type Locale,
  isLocale,
  localeDir,
} from './translations';

export const STORAGE_KEY = 'novashop-locale';
export const DEFAULT_LOCALE: Locale = 'fa';

interface LocaleContextValue {
  locale: Locale;
  dir: 'rtl' | 'ltr';
  setLocale: (l: Locale) => void;
  /** Translate a dot-path key; supports `{var}` placeholders via args. */
  t: (key: string, fallback?: string) => string;
  tva: (key: string, args: Record<string, string | number>, fallback?: string) => string;
  /** Locale-aware number formatting (latin for en, eastern arabic for fa/ar). */
  formatNumber: (n: number | string, opts?: { maxFractionDigits?: number }) => string;
}

const LocaleContext = createContext<LocaleContextValue | null>(null);

function interpolate(template: string, args: Record<string, string | number>): string {
  return template.replace(/\{(\w+)\}/g, (_, k: string) =>
    k in args ? String(args[k]) : ''
  );
}

export function LocaleProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(DEFAULT_LOCALE);

  useEffect(() => {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved && isLocale(saved)) {
      setLocaleState(saved);
      applyDir(saved);
    }
  }, []);

  useEffect(() => {
    applyDir(locale);
  }, [locale]);

  function applyDir(l: Locale) {
    if (typeof document === 'undefined') return;
    document.documentElement.lang = l;
    document.documentElement.dir = localeDir(l);
  }

  const setLocale = useCallback((l: Locale) => {
    setLocaleState(l);
    try {
      localStorage.setItem(STORAGE_KEY, l);
    } catch (e) {
      console.warn('Could not persist locale', e);
    }
    applyDir(l);
  }, []);

  const t = useCallback(
    (key: string, fallback?: string): string => {
      const map = translations[locale];
      if (!map) return fallback ?? key;
      const value = map[key];
      return value ?? fallback ?? key;
    },
    [locale]
  );

  const tva = useCallback(
    (key: string, args: Record<string, string | number>, fallback?: string): string => {
      const resolved = t(key, fallback);
      return interpolate(resolved, args);
    },
    [t]
  );

  const formatNumber = useCallback(
    (n: number | string, opts?: { maxFractionDigits?: number }): string => {
      const value = typeof n === 'string' ? Number(n) : n;
      if (Number.isNaN(value)) return String(n);
      // fa and ar both use Eastern Arabic-Indic digits (۰-۹ / ٠-٩).
      return new Intl.NumberFormat(locale, {
        maximumFractionDigits: opts?.maxFractionDigits ?? 2,
      }).format(value);
    },
    [locale]
  );

  const dir = localeDir(locale);

  return (
    <LocaleContext.Provider value={{ locale, dir, setLocale, t, tva, formatNumber }}>
      {children}
    </LocaleContext.Provider>
  );
}

export function useLocale(): LocaleContextValue {
  const ctx = useContext(LocaleContext);
  if (!ctx) throw new Error('useLocale must be inside <LocaleProvider>');
  return ctx;
}