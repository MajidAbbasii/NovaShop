'use client';

import { useState, useEffect } from 'react';
import { useParams, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { useLocale } from '@/lib/locale-context';
import { formatCurrency } from '@/lib/formatters';
import { statusKey } from '@/lib/admin-i18n';
import { CheckCircle2, Package, ArrowLeft, Store, Clock, CreditCard, Truck, Home, XCircle } from 'lucide-react';
import { API_GATEWAY_URL } from '@/lib/config';

const STATUS_ICONS: Record<string, typeof Package> = {
  Pending: Clock,
  Confirmed: CheckCircle2,
  Processing: Package,
  Paid: CreditCard,
  Shipped: Truck,
  Delivered: Home,
  Cancelled: XCircle,
  Failed: XCircle,
};

function OrderTimelineStep({
  status,
  changedAt,
  isLast,
  lang,
}: {
  status: string;
  changedAt: string;
  isLast: boolean;
  lang: string;
}) {
  const { t } = useLocale();
  const Icon = STATUS_ICONS[status] ?? Clock;
  const date = new Intl.DateTimeFormat(lang, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(changedAt));

  return (
    <li className="relative flex gap-3 pb-5 last:pb-0">
      {/* Connector line */}
      {!isLast && (
        <span
          aria-hidden
          className="absolute top-8 bottom-0 start-[13px] w-0.5 rounded bg-green-200"
        />
      )}
      {/* Node */}
      <span className="relative z-10 mt-0.5 flex size-7 shrink-0 items-center justify-center rounded-full bg-green-100 text-green-700">
        <Icon className="size-3.5" />
      </span>
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium">{t(statusKey(status))}</p>
        <p className="text-xs text-muted-foreground">{date}</p>
      </div>
    </li>
  );
}

interface OrderItem {
  productId: number;
  productName: string;
  colorName?: string;
  quantity: number;
  unitPrice: number;
}

interface OrderHistoryEntry {
  fromStatus: string;
  toStatus: string;
  note?: string;
  changedByRole: string;
  changedAt: string;
}

interface OrderData {
  id: number;
  totalAmount: number;
  discountAmount?: number;
  discountCode?: string;
  status: string;
  trackingCode?: string;
  shippingAddress: string;
  shippingMethod?: string;
  shippingCost?: number;
  paymentMethod?: string;
  paymentStatus?: string;
  createdAt: string;
  items: OrderItem[];
  statusHistory?: OrderHistoryEntry[];
}

export default function OrderConfirmationPage() {
  const params = useParams();
  const searchParams = useSearchParams();
  const { t, dir, locale } = useLocale();
  const orderId = params?.id as string;
  const customerName = searchParams?.get('name') || t('order.thankYou');
  const [order, setOrder] = useState<OrderData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!orderId) return;
    async function load() {
      try {
        const headers: Record<string, string> = {};
        const token = typeof document !== 'undefined'
          ? document.cookie.match(/(?:^|; )token=([^;]*)/)?.[1]
          : null;
        if (token) headers['Authorization'] = `Bearer ${token}`;

        const res = await fetch(`${API_GATEWAY_URL}/api/orders/${orderId}`, { headers });
        if (res.ok) {
          const data = await res.json();
          setOrder(data);
        }
      } catch (e) { console.error(e); }
      finally { setLoading(false); }
    }
    load();
  }, [orderId]);

  const ArrowIcon = ArrowLeft;

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-b from-green-50 to-amber-50/30">
        <div className="animate-pulse text-center">
          <Package className="mx-auto mb-4 size-12 text-green-600 animate-bounce" />
          <p className="text-muted-foreground">{t('loading.order')}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-green-50 to-amber-50/30 py-12">
      <div className="mx-auto max-w-lg px-4 text-center">
        <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-green-100 shadow-sm">
          <CheckCircle2 className="size-10 text-green-600" />
        </div>

        <h1 className="text-3xl font-bold">{t('order.title')}</h1>
        <p className="mt-2 text-muted-foreground">
          {t('order.desc')}
        </p>

        {order && (
          <Card className={`mt-8 shadow-lg border-0 rounded-2xl ${dir === 'rtl' ? 'text-right' : 'text-left'}`}>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-lg">
                <Package className="size-5" />
                {t('order.id')} #{order.id}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">{t('order.status')}</span>
                <span className="font-medium capitalize text-green-600">{t(statusKey(order.status))}</span>
              </div>
              {order.trackingCode && (
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">{t('order.trackingCode')}</span>
                  <span className="font-mono font-semibold" dir="ltr">{order.trackingCode}</span>
                </div>
              )}
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">{t('order.date')}</span>
                <span>{new Intl.DateTimeFormat(locale, { year: 'numeric', month: 'short', day: 'numeric' }).format(new Date(order.createdAt))}</span>
              </div>

              {order.statusHistory && order.statusHistory.length > 0 && (
                <>
                  <Separator />
                  <div className={`space-y-2 ${dir === 'rtl' ? 'text-right' : ''}`}>
                    <p className="text-sm font-semibold">{t('order.timeline')}</p>
                    <ol className="space-y-0">
                      {order.statusHistory.map((h, i) => (
                        <OrderTimelineStep
                          key={i}
                          status={h.toStatus}
                          changedAt={h.changedAt}
                          isLast={i === order.statusHistory!.length - 1}
                          lang={locale}
                        />
                      ))}
                    </ol>
                  </div>
                </>
              )}

              <Separator />

              <div className="space-y-2">
                {order.items.map((item, i) => (
                  <div key={i} className="flex justify-between text-sm">
                    <span>
                      {item.productName}
                      {item.colorName && (
                        <span className="text-xs text-muted-foreground"> ({item.colorName})</span>
                      )}{' '}
                      × {item.quantity}
                    </span>
                    <span>{formatCurrency(item.unitPrice * item.quantity, locale)}</span>
                  </div>
                ))}
              </div>

              <Separator />

              {order.discountAmount && order.discountAmount > 0 && (
                <div className="flex justify-between text-sm">
                  <span>{t('order.discount')}{order.discountCode ? ` (${order.discountCode})` : ''}</span>
                  <span className="font-semibold text-green-600">−{formatCurrency(order.discountAmount, locale)}</span>
                </div>
              )}

              <div className="flex justify-between font-semibold text-lg">
                <span>{t('order.total')}</span>
                <span>{formatCurrency(order.totalAmount, locale)}</span>
              </div>

              <div className="rounded-lg bg-amber-50 p-3 text-sm">
                <p className="font-medium">{t('order.shippingTo')}:</p>
                <p className="text-muted-foreground">{order.shippingAddress}</p>
                {order.shippingMethod && (
                  <p className="mt-1.5 text-xs font-medium text-muted-foreground">
                    {t('order.shippingMethodLabel')}:{' '}
                    {order.shippingMethod === 'PICKUP'
                      ? t('checkout.method.pickup')
                      : order.shippingMethod === 'COURIER'
                        ? t('checkout.method.courier')
                        : t('checkout.method.post')}
                    {order.shippingMethod ? ` — ${formatCurrency(order.shippingCost ?? 0, locale)}` : ''}
                  </p>
                )}
                {order.paymentMethod && (
                  <p className="mt-1 text-xs text-muted-foreground">
                    {t('order.paymentMethodLabel')}:{' '}
                    {order.paymentMethod === 'CashOnDelivery' || order.paymentMethod === 'COD'
                      ? t('checkout.payment.cod')
                      : order.paymentMethod === 'InPerson'
                        ? t('checkout.payment.inPerson')
                        : t('checkout.payment.online')}
                  </p>
                )}
                {order.paymentStatus && (
                  <p className="mt-1 text-xs text-muted-foreground">
                    {t('order.paymentStatus')}:{' '}
                    {order.paymentStatus === 'Paid'
                      ? t('status.paid')
                      : t('pendingPayment')}
                  </p>
                )}
              </div>

              <p className="text-xs text-muted-foreground text-center">
                {t('checkout.paymentMethod')}
              </p>
            </CardContent>
          </Card>
        )}

        <div className={`mt-8 flex flex-col items-center gap-3 ${dir === 'rtl' ? 'flex-col' : ''}`}>
          <Button className="gap-2 rounded-full shadow-md" asChild>
            <Link href="/products">
              {t('order.continueShopping')}
              <ArrowIcon className="size-4" />
            </Link>
          </Button>
          <Button variant="outline" className="gap-2 rounded-full" asChild>
            <Link href="/">
              <Store className="size-4" /> {t('order.backToHome')}
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
