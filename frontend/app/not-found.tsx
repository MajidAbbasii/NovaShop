'use client';

import Link from 'next/link';
import { useLocale } from '@/lib/locale-context';

export default function NotFound() {
  const { t } = useLocale();
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4">
      <h1 className="text-6xl font-bold">{t('notFound.title')}</h1>
      <p className="text-muted-foreground">{t('notFound.message')}</p>
      <Link href="/" className="inline-flex items-center justify-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90">
        {t('notFound.goHome')}
      </Link>
    </div>
  );
}