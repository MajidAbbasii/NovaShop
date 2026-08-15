'use client';

import Link from 'next/link';
import Image from 'next/image';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { useCart } from '@/lib/cart-context';
import { computeSubtotal, isAuthenticated } from '@/lib/cart-api';
import { resolveImageUrl } from '@/lib/config';
import { useLocale } from '@/lib/locale-context';
import { formatCurrency } from '@/lib/formatters';
import {
  ShoppingCart,
  ShoppingBag,
  Trash2,
  Plus,
  Minus,
  ArrowLeft,
  Loader2,
} from 'lucide-react';
import { useEffect, useSyncExternalStore } from 'react';

export default function CartPage() {
  const router = useRouter();
  const { t, dir, locale } = useLocale();
  const { cart, isLoading, updateQuantity, removeItem } = useCart();
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false
  );

  useEffect(() => {
    if (!isAuthenticated()) router.push('/login');
  }, [router]);

  if (!mounted || isLoading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-amber-600" />
      </div>
    );
  }

  const items = cart.items ?? [];
  const subtotal = computeSubtotal(items);
  // Shipping math in native Toman (values stored/displayed as Toman).
  const shipping = subtotal >= 500_000 ? 0 : 59_900;
  const total = subtotal + shipping;

  return (
    <div className="min-h-screen bg-amber-50/30 py-10">
      <div className="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
        <div className={`mb-8 flex items-center ${dir === 'rtl' ? 'flex-row-reverse justify-between' : 'justify-between'}`}>
          <Link
            href="/products"
            className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className={`size-4 ${dir === 'rtl' ? '' : 'rotate-180'}`} />
            {t('cart.continue')}
          </Link>
          <h1 className="text-2xl font-bold">{t('cart.title')}</h1>
          <span className="w-16" />
        </div>

        {items.length === 0 ? (
          <Card className="border-0 shadow-md rounded-2xl">
            <CardContent className="py-16 text-center">
              <div className="mx-auto mb-4 flex size-16 items-center justify-center rounded-full bg-muted">
                <ShoppingBag className="size-8 text-muted-foreground/50" />
              </div>
              <p className="text-lg font-medium">{t('cart.empty')}</p>
              <p className="mt-1 text-sm text-muted-foreground">{t('cart.empty.desc')}</p>
              <Button className="mt-6 gap-2 rounded-full" asChild>
                <Link href="/products">
                  <ShoppingCart className="size-4" />
                  {t('cart.continue')}
                </Link>
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-6 lg:grid-cols-5">
            {/* Items */}
            <div className="space-y-3 lg:col-span-3">
              {items.map((item) => (
                <div
                  key={item.id}
                  className="flex gap-3 rounded-2xl border bg-card p-3 shadow-sm"
                >
                  <div className="relative size-20 shrink-0 overflow-hidden rounded-lg bg-muted">
                    {item.imageUrl ? (
                      <Image
                        src={resolveImageUrl(item.imageUrl)}
                        alt={item.productName}
                        fill
                        className="object-cover"
                        sizes="80px"
                      />
                    ) : (
                      <div className="flex size-full items-center justify-center text-muted-foreground/40">
                        <ShoppingBag className="size-6" />
                      </div>
                    )}
                  </div>

                  <div className="flex min-w-0 flex-1 flex-col justify-between">
                    <div className="flex items-start justify-between gap-2">
                      <Link
                        href={`/products/${item.productId}`}
                        className="truncate text-sm font-medium hover:underline"
                      >
                        {item.productName}
                      </Link>
                      <Button
                        variant="ghost"
                        size="icon-xs"
                        className="size-6 shrink-0 text-muted-foreground hover:text-destructive"
                        onClick={() => removeItem(item.id)}
                      >
                        <Trash2 className="size-3.5" />
                        <span className="sr-only">{t('cart.remove')}</span>
                      </Button>
                    </div>

                    {item.colorName && (
                      <span className="flex items-center gap-1 text-xs text-muted-foreground">
                        <span className="size-2.5 rounded-full border border-black/10 bg-current" aria-hidden="true" />
                        {t('product.color')}: {item.colorName}
                      </span>
                    )}

                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-1">
                        <Button
                          variant="outline"
                          size="icon-xs"
                          className="size-6"
                          onClick={() => updateQuantity(item.id, item.quantity - 1)}
                          disabled={item.quantity <= 1}
                        >
                          <Minus className="size-3" />
                        </Button>
                        <span className="w-7 text-center text-xs tabular-nums">
                          {item.quantity}
                        </span>
                        <Button
                          variant="outline"
                          size="icon-xs"
                          className="size-6"
                          onClick={() => updateQuantity(item.id, item.quantity + 1)}
                        >
                          <Plus className="size-3" />
                        </Button>
                      </div>
                      <span className="text-sm font-semibold">
                        {formatCurrency(item.unitPrice * item.quantity, locale)}
                      </span>
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Summary */}
            <div className="lg:col-span-2">
              <Card className="rounded-2xl border-0 shadow-md">
                <CardContent className="space-y-3">
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{t('cart.subtotal')}</span>
                    <span>{formatCurrency(subtotal, locale)}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{t('cart.shipping')}</span>
                    <span className={shipping === 0 ? 'text-green-600' : ''}>
                      {shipping === 0 ? t('cart.freeShipping') : formatCurrency(shipping, locale)}
                    </span>
                  </div>
                  <Separator />
                  <div className="flex justify-between text-base font-semibold">
                    <span>{t('cart.total')}</span>
                    <span>{formatCurrency(total, locale)}</span>
                  </div>
                  <Button
                    size="lg"
                    className="w-full gap-2 rounded-full"
                    asChild
                  >
                    <Link href="/checkout">
                      {t('cart.checkout')}
                      <ArrowLeft className={`size-4 ${dir === 'rtl' ? '' : 'rotate-180'}`} />
                    </Link>
                  </Button>
                  <Button variant="outline" className="w-full rounded-full" asChild>
                    <Link href="/products">{t('cart.continue')}</Link>
                  </Button>
                </CardContent>
              </Card>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}