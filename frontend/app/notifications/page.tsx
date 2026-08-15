'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { useLocale } from '@/lib/locale-context';
import { authHeaders, isAuthenticated } from '@/lib/cart-api';
import { Bell, Package, CreditCard, Truck, Home, Wallet, CheckCheck, ShoppingBag, Camera } from 'lucide-react';
import { API_GATEWAY_URL } from '@/lib/config';
import { cn } from '@/lib/utils';

interface AppNotification {
  id: number;
  orderId?: number;
  customDollRequestId?: number;
  type: string;
  channel: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

function typeIcon(type: string) {
  if (type === 'OrderPlaced' || type.startsWith('Status_')) return Package;
  if (type === 'PaymentSuccessful') return CreditCard;
  if (type.includes('Shipped')) return Truck;
  if (type.includes('Delivered')) return Home;
  if (type.includes('Wallet') || type.includes('wallet')) return Wallet;
  if (type.startsWith('CustomDoll')) return Camera;
  return Bell;
}

function typeTone(type: string): string {
  if (type === 'PaymentSuccessful') return 'bg-green-100 text-green-700';
  if (type.includes('Shipped') || type.includes('Delivered')) return 'bg-sky-100 text-sky-700';
  if (type.includes('Wallet') || type.includes('wallet')) return 'bg-amber-100 text-amber-700';
  if (type === 'CustomDollApproved') return 'bg-green-100 text-green-700';
  if (type === 'CustomDollRejected') return 'bg-red-100 text-red-700';
  return 'bg-primary/10 text-primary';
}

function relatedHref(n: AppNotification): string | null {
  if (n.orderId) return `/orders/${n.orderId}`;
  if (n.customDollRequestId) return `/custom-doll-requests/${n.customDollRequestId}`;
  return null;
}

export default function NotificationsPage() {
  const router = useRouter();
  const { t, dir, locale } = useLocale();
  const [items, setItems] = useState<AppNotification[] | null>(null);
  const [error, setError] = useState(false);

  const load = async () => {
    try {
      const res = await fetch(`${API_GATEWAY_URL}/api/notifications?pageSize=50`, {
        headers: authHeaders(),
      });
      if (!res.ok) throw new Error('Failed');
      const data = await res.json();
      setItems(data.items ?? []);
    } catch {
      setError(true);
    }
  };

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace('/login');
      return;
    }
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [router]);

  const markRead = async (id: number) => {
    setItems((prev) => prev?.map((n) => (n.id === id ? { ...n, isRead: true } : n)) ?? []);
    try {
      await fetch(`${API_GATEWAY_URL}/api/notifications/${id}/read`, {
        method: 'POST',
        headers: authHeaders(),
      });
    } catch {
      // optimistic; ignore failures
    }
  };

  const markAllRead = async () => {
    setItems((prev) => prev?.map((n) => ({ ...n, isRead: true })) ?? []);
    try {
      await fetch(`${API_GATEWAY_URL}/api/notifications/read-all`, {
        method: 'POST',
        headers: authHeaders(),
      });
    } catch {
      // optimistic; ignore failures
    }
  };

  if (items === null && !error) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="w-full max-w-xl space-y-3 px-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="store-skeleton h-20" />
          ))}
        </div>
      </div>
    );
  }

  const unreadCount = items?.filter((n) => !n.isRead).length ?? 0;

  return (
    <div className="min-h-screen bg-amber-50/30 py-10">
      <div className="mx-auto max-w-xl px-4 sm:px-6 lg:px-8">
        <div className={`mb-6 flex items-center justify-between ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
          <h1 className="flex items-center gap-2 text-2xl font-bold">
            <Bell className="size-6 text-primary" />
            {t('notifications.title')}
            {unreadCount > 0 && (
              <span className="rounded-full bg-primary px-2 py-0.5 text-xs font-bold text-primary-foreground">
                {unreadCount}
              </span>
            )}
          </h1>
          {unreadCount > 0 && (
            <Button variant="ghost" size="sm" className="gap-1.5" onClick={markAllRead}>
              <CheckCheck className="size-4" />
              {t('notifications.markAllRead')}
            </Button>
          )}
        </div>

        {error ? (
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-12 text-center text-muted-foreground">
              {t('notifications.loadError')}
            </CardContent>
          </Card>
        ) : items && items.length === 0 ? (
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-16 text-center">
              <Bell className="mx-auto mb-4 size-12 text-muted-foreground/50" />
              <p className="text-lg font-medium">{t('notifications.empty')}</p>
              <p className="mt-1 text-sm text-muted-foreground">
                {t('notifications.empty.desc')}
              </p>
              <Button className="mt-6 rounded-full" asChild>
                <Link href="/products">
                  <ShoppingBag className="size-4" />
                  {t('notifications.goToStore')}
                </Link>
              </Button>
            </CardContent>
          </Card>
        ) : (
          <ul className="space-y-2.5">
            {items?.map((n) => {
              const Icon = typeIcon(n.type);
              const href = relatedHref(n);
              const body = (
                <div
                  className={cn(
                    'flex items-start gap-3 rounded-2xl border p-3.5 transition-all',
                    n.isRead
                      ? 'border-border/60 bg-card'
                      : 'border-primary/25 bg-primary/5 shadow-sm'
                  )}
                >
                  <span className={cn('mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full', typeTone(n.type))}>
                    <Icon className="size-4" />
                  </span>
                  <div className="min-w-0 flex-1">
                    <div className={`flex items-start gap-2 ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
                      <p className={cn('flex-1 text-sm font-medium', !n.isRead && 'font-semibold')}>
                        {n.title}
                      </p>
                      {!n.isRead && (
                        <span className="mt-1 size-2 shrink-0 rounded-full bg-primary" aria-label={t('notifications.unread')} />
                      )}
                    </div>
                    {n.message && (
                      <p className="mt-0.5 text-xs leading-relaxed text-muted-foreground">
                        {n.message}
                      </p>
                    )}
                    <p className="mt-1.5 text-[11px] text-muted-foreground/80">
                      {new Intl.DateTimeFormat(locale, {
                        year: 'numeric',
                        month: 'short',
                        day: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                      }).format(new Date(n.createdAt))}
                    </p>
                  </div>
                  {!n.isRead && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="shrink-0 text-xs"
                      onClick={() => markRead(n.id)}
                    >
                      {t('notifications.markRead')}
                    </Button>
                  )}
                </div>
              );
              return (
                <li key={n.id}>
                  {href ? (
                    <Link href={href} className="block" onClick={() => !n.isRead && markRead(n.id)}>
                      {body}
                    </Link>
                  ) : (
                    body
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </div>
  );
}