'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { AlertTriangle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useLocale } from '@/lib/locale-context';

export default function ProductError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  const router = useRouter();
  const { t } = useLocale();

  useEffect(() => {
    console.error('Product page error:', error);
  }, [error]);

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 px-4 text-center">
      <div className="rounded-full bg-destructive/10 p-4">
        <AlertTriangle className="size-8 text-destructive" />
      </div>
      <h2 className="text-xl font-bold">{t('error.loadFailed')}</h2>
      <p className="max-w-md text-sm text-muted-foreground">
        {t('error.notFound')} {t('error.networkHint')}
      </p>
      {error.digest && (
        <p className="font-mono text-xs text-muted-foreground/50">
          Error ID: {error.digest}
        </p>
      )}
      <div className="flex gap-3 pt-2">
        <Button onClick={reset}>{t('error.retry')}</Button>
        <Button variant="outline" onClick={() => router.push('/products')}>
          {t('header.shop')}
        </Button>
      </div>
    </div>
  );
}
