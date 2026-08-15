'use client';

import { useEffect, useSyncExternalStore, type ReactNode } from 'react';
import { LocaleProvider } from '@/lib/locale-context';

export function ClientLayout({ children }: { children: ReactNode }) {
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false
  );

  useEffect(() => {
    // Default dir/lang on first mount
    const locale = localStorage.getItem('novashop-locale') || 'fa';
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === 'fa' ? 'rtl' : 'ltr';
  }, []);

  return (
    <html lang="fa" dir="rtl" className="h-full antialiased" style={mounted ? undefined : { direction: 'rtl' }}>
      <head>
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link href="https://fonts.googleapis.com/css2?family=Vazirmatn:wght@300;400;500;600;700;800&display=swap" rel="stylesheet" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <script
          dangerouslySetInnerHTML={{
            __html: `try{var t=localStorage.getItem('novashop-theme');if(t==='dark'||!t)document.documentElement.classList.add('dark')}catch(e){}`,
          }}
        />
      </head>
      <body className="min-h-full flex flex-col bg-background text-foreground">
        <LocaleProvider>
          {children}
        </LocaleProvider>
      </body>
    </html>
  );
}
