'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useLocale } from '@/lib/locale-context';
import { formatCurrency } from '@/lib/formatters';
import { authHeaders, isAuthenticated } from '@/lib/cart-api';
import { statusKey } from '@/lib/admin-i18n';
import { PackageSearch, ArrowRight } from 'lucide-react';
import { API_GATEWAY_URL } from '@/lib/config';

interface OrderItem {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
}

interface Order {
  id: number;
  totalAmount: number;
  status: string;
  trackingCode?: string;
  createdAt: string;
  items: OrderItem[];
}

interface PagedOrders {
  items: Order[];
  totalCount: number;
  totalPages: number;
}

export default function OrdersPage() {
  const router = useRouter();
  const { t, dir, locale } = useLocale();
  const [orders, setOrders] = useState<Order[] | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace('/login');
      return;
    }
    (async () => {
      try {
        const res = await fetch(`${API_GATEWAY_URL}/api/orders?pageNumber=1&pageSize=50`, {
          headers: authHeaders(),
        });
        if (!res.ok) throw new Error('Failed');
        const data: PagedOrders = await res.json();
        setOrders(data.items ?? []);
      } catch {
        setError(true);
      }
    })();
  }, [router]);

  const ArrowIcon = ArrowRight;

  if (orders === null && !error) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="w-full max-w-3xl space-y-3 px-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="store-skeleton h-24" />
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-amber-50/30 py-10">
      <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8">
        <h1 className={`mb-6 text-2xl font-bold ${dir === 'rtl' ? 'text-right' : ''}`}>
          {t('orders.title')}
        </h1>

        {error ? (
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-12 text-center text-muted-foreground">
              {t('error.loadFailed')}
            </CardContent>
          </Card>
        ) : orders && orders.length === 0 ? (
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-16 text-center">
              <PackageSearch className="mx-auto mb-4 size-12 text-muted-foreground/50" />
              <p className="text-lg font-medium">{t('orders.empty')}</p>
              <p className="mt-1 text-sm text-muted-foreground">{t('orders.empty.desc')}</p>
              <Button className="mt-6 rounded-full" asChild>
                <Link href="/products">{t('cart.continue')}</Link>
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-3">
            {orders?.map((order) => (
              <Link key={order.id} href={`/orders/${order.id}`} className="block">
                <Card className={`transition-shadow hover:shadow-lg rounded-2xl border-0 shadow-sm ${dir === 'rtl' ? 'text-right' : ''}`}>
                  <CardContent className="flex items-center justify-between gap-3 p-4">
                    <div className="min-w-0">
                      <p className="text-sm font-semibold">
                        <span className="text-muted-foreground">{t('order.id')}: </span>
                        #{order.id}
                      </p>
                      <p className="mt-1 text-xs text-muted-foreground">
                        {new Intl.DateTimeFormat(locale, { year: 'numeric', month: 'short', day: 'numeric' }).format(new Date(order.createdAt))}
                      </p>
                      <p className="mt-1 text-sm font-medium">{formatCurrency(order.totalAmount, locale)}</p>
                      <p className="mt-0.5 text-xs text-muted-foreground">
                        {order.items?.length ?? 0} {t('cart.items')}
                      </p>
                    </div>
                    <div className="flex flex-col items-end gap-2">
                      <Badge variant="outline" className="text-xs">{t(statusKey(order.status))}</Badge>
                      <span className="flex items-center gap-1 text-xs text-primary">
                        {t('orders.view')} <ArrowIcon className="size-3.5" />
                      </span>
                    </div>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}