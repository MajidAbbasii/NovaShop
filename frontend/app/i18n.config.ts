import { getRequestConfig } from 'next-intl/server';

export const locales = ['en', 'fa'] as const;
export const defaultLocale = 'en' as const;
export const localePrefix = 'as-needed' as const;

export default getRequestConfig(async ({ locale }) => {
  const safeLocale = locales.includes(locale as (typeof locales)[number]) ? locale : defaultLocale;

  try {
    const messages = (await import(`@/messages/${safeLocale}/common.json`)).default;
    return {
      locale: safeLocale as string,
      messages,
      timeZone: safeLocale === 'fa' ? 'Asia/Tehran' : 'UTC',
    };
  } catch {
    return {
      locale: safeLocale as string,
      messages: {},
      timeZone: 'UTC',
    };
  }
});
