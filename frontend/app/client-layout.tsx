'use client';

import { useEffect, useSyncExternalStore, type ReactNode } from 'react';
import { LocaleProvider, STORAGE_KEY, DEFAULT_LOCALE } from '@/lib/locale-context';

type Locale = 'fa' | 'en' | 'ar';
// Read the saved locale/theme synchronously so server and client agree.
// useSyncExternalStore gives `false` on the server (SSR markup) and the
// actual value on the client, eliminating the hydration mismatch that
// happened when an inline script mutated documentElement before React hydrated.
function readLocale(): Locale {
  try {
    const v = localStorage.getItem(STORAGE_KEY);
    return v === 'fa' || v === 'en' || v === 'ar' ? (v as Locale) : DEFAULT_LOCALE;
  } catch {
    return DEFAULT_LOCALE;
  }
}

function readTheme(): 'dark' | 'light' {
  try {
    const v = localStorage.getItem('novashop-theme');
    return v === 'light' ? 'light' : 'dark'; // dark is the default
  } catch {
    return 'dark';
  }
}

export function ClientLayout({ children }: { children: ReactNode }) {
  // On the server these are the defaults; on the client they read storage.
  const locale = useSyncExternalStore(
    () => () => ({}),
    readLocale,
    () => DEFAULT_LOCALE,
  );
  const theme = useSyncExternalStore(
    () => () => ({}),
    readTheme,
    () => 'dark',
  );

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === 'fa' || locale === 'ar' ? 'rtl' : 'ltr';
  }, [locale]);

  const dir = locale === 'fa' || locale === 'ar' ? 'rtl' : 'ltr';

  return (
    <html
      lang={locale}
      dir={dir}
      className={`h-full antialiased ${theme === 'dark' ? 'dark' : ''}`}
      style={{ direction: dir }}
    >
      <head>
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link
          href="https://fonts.googleapis.com/css2?family=Vazirmatn:wght@300;400;500;600;700;800&display=swap"
          rel="stylesheet"
        />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
      </head>
      <body className="min-h-full flex flex-col bg-background text-foreground">
        <LocaleProvider>{children}</LocaleProvider>
      </body>
    </html>
  );
}
