import { NextIntlClientProvider } from 'next-intl';
import { notFound } from 'next/navigation';

type Messages = typeof import('@/messages/en/common.json');

export async function generateStaticParams() {
  return [
    { locale: 'en' },
    { locale: 'fa' },
  ];
}

export default async function NextIntlProvider({
  children,
  params: { locale },
}: {
  children: React.ReactNode;
  params: { locale: string };
}) {
  if (!['en', 'fa'].includes(locale)) {
    notFound();
  }

  let messages: Messages;
  try {
    messages = (await import(`@/messages/${locale}/common.json`)) as Messages;
  } catch {
    notFound();
  }

  const dir = locale === 'fa' ? 'rtl' : 'ltr';

  return (
    <html lang={locale} dir={dir}>
      <body>
        <NextIntlClientProvider locale={locale} messages={messages} timeZone="Asia/Tehran">
          {children}
        </NextIntlClientProvider>
      </body>
    </html>
  );
}

export { useTranslations, useLocale } from 'next-intl';