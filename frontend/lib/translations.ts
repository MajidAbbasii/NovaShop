// ---------------------------------------------------------------------------
// Translation bootstrap — TYPE & LOCALE HELPERS ONLY.
//
// The actual translation VALUES live in the Backend database and are loaded at
// runtime via the Translation API (see lib/translation-service.ts →
// GET /api/translations?locale=<locale> through the API Gateway).
//
// This file intentionally contains NO translation dictionary. It only defines
// the supported locales and helpers so the rest of the app can stay typed and
// locale-aware. Do NOT re-introduce a static dictionary here — the Backend is
// the single source of truth for all UI translations.
// ---------------------------------------------------------------------------

export type Locale = 'fa' | 'en' | 'ar';

export const SUPPORTED_LOCALES: { code: Locale; name: string; native: string; dir: 'rtl' | 'ltr' }[] = [
  { code: 'fa', name: 'Persian', native: 'فارسی', dir: 'rtl' },
  { code: 'en', name: 'English', native: 'English', dir: 'ltr' },
  { code: 'ar', name: 'Arabic', native: 'العربية', dir: 'rtl' },
];

export function isLocale(code: string): code is Locale {
  return code === 'fa' || code === 'en' || code === 'ar';
}

export function localeDir(locale: Locale): 'rtl' | 'ltr' {
  return locale === 'fa' || locale === 'ar' ? 'rtl' : 'ltr';
}

/** Shape of a loaded locale dictionary (key → translated value). */
export type TranslationMap = Record<string, string>;
