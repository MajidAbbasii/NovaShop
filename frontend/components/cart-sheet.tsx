'use client';

import Image from 'next/image';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetClose,
} from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { useCart } from '@/lib/cart-context';
import { computeSubtotal } from '@/lib/cart-api';
import { resolveImageUrl } from '@/lib/config';
import { useLocale } from '@/lib/locale-context';
import { formatCurrency } from '@/lib/formatters';
import {
  ShoppingCart,
  Trash2,
  Plus,
  Minus,
  ArrowLeft,
  ShoppingBag,
} from 'lucide-react';

export function CartSheet() {
  const { t, dir, locale } = useLocale();
  const pathname = usePathname();
  const {
    cart,
    itemCount,
    sheetOpen,
    closeSheet,
    updateQuantity,
    removeItem,
  } = useCart();

  // Hide the cart sheet inside the admin panel.
  if (pathname?.startsWith('/admin')) return null;

  const items = cart?.items ?? [];
  const subtotal = computeSubtotal(items);

  const ArrowIcon = dir === 'rtl' ? ArrowLeft : ArrowLeft;

  return (
    <Sheet open={sheetOpen} onOpenChange={(open) => !open && closeSheet()}>
      <SheetContent side={dir === 'rtl' ? 'left' : 'right'} className="flex w-full flex-col sm:max-w-md">
        <SheetHeader className="px-0">
          <SheetTitle className={`flex items-center gap-2 ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
            <ShoppingCart className="size-4" />
            {t('cart.title')}
            {itemCount > 0 && (
              <span className={`text-xs font-normal text-muted-foreground ${dir === 'rtl' ? 'mr-auto' : 'ml-auto'}`}>
                {itemCount} {itemCount === 1 ? t('cart.item') : t('cart.items')}
              </span>
            )}
          </SheetTitle>
        </SheetHeader>

        {items.length === 0 ? (
          <EmptyState />
        ) : (
          <>
            {/* Free shipping progress */}
            <FreeShippingBar subtotal={subtotal} />

            <div className={`flex-1 overflow-y-auto space-y-3 ${dir === 'rtl' ? 'px-0' : '-mx-4 px-4'}`}>
              {items.map((item) => (
                <CartItemRow
                  key={item.id}
                  item={item}
                  onQuantityChange={(qty) => updateQuantity(item.id, qty)}
                  onRemove={() => removeItem(item.id)}
                />
              ))}
            </div>

            <Separator />

            <div className="space-y-3 pt-1">
              <div className={`flex items-center justify-between text-sm`}>
                <span className="text-muted-foreground">{t('cart.subtotal')}</span>
                <span>{formatCurrency(subtotal, locale)}</span>
              </div>
              <div className={`flex items-center justify-between text-base font-semibold`}>
                <span>{t('cart.total')}</span>
                <span>{formatCurrency(subtotal, locale)}</span>
              </div>

              <div className="flex flex-col gap-2">
                <SheetClose asChild>
                  <Button className="w-full gap-2" asChild>
                    <Link href="/checkout">
                      {t('cart.checkout')}
                      <ArrowIcon className="size-4" />
                    </Link>
                  </Button>
                </SheetClose>

                <SheetClose asChild>
                  <Button variant="outline" className="w-full" asChild>
                    <Link href="/products">
                      {t('cart.continue')}
                    </Link>
                  </Button>
                </SheetClose>
              </div>
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  );
}

function EmptyState() {
  const { t } = useLocale();
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4">
      <div className="rounded-full bg-muted p-4">
        <ShoppingBag className="size-8 text-muted-foreground/60" />
      </div>
      <div className="text-center">
        <p className="text-base font-medium">{t('cart.empty')}</p>
        <p className="mt-1 text-sm text-muted-foreground">
          {t('cart.empty.desc')}
        </p>
      </div>
      <SheetClose asChild>
        <Button variant="outline" asChild>
          <Link href="/products">{t('cart.continue')}</Link>
        </Button>
      </SheetClose>
    </div>
  );
}

interface CartItemRowProps {
  item: {
    id: number;
    productId: number;
    productName: string;
    imageUrl: string;
    colorName?: string;
    unitPrice: number;
    quantity: number;
  };
  onQuantityChange: (qty: number) => void;
  onRemove: () => void;
}

function CartItemRow({ item, onQuantityChange, onRemove }: CartItemRowProps) {
  const { dir, locale } = useLocale();
  const { t, tva } = useLocale();
  return (
    <div className={`flex gap-3 rounded-lg border p-3 ${dir === 'rtl' ? 'flex-row' : ''}`}>
      <div className="relative size-20 shrink-0 overflow-hidden rounded-md bg-muted">
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
        <div className={`flex items-start gap-2 ${dir === 'rtl' ? 'flex-row-reverse justify-between' : 'justify-between'}`}>
          <Link
            href={`/products/${item.productId}`}
            className="text-sm font-medium leading-tight hover:underline truncate"
          >
            {item.productName}
          </Link>
          <Button
            variant="ghost"
            size="icon-xs"
            className="size-6 shrink-0 text-muted-foreground hover:text-destructive"
            onClick={onRemove}
          >
            <Trash2 className="size-3.5" />
            <span className="sr-only">{tva('cart.removeAria', { name: item.productName })}</span>
          </Button>
        </div>

        {item.colorName && (
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            <span className="size-2.5 rounded-full border border-black/10 bg-current" aria-hidden="true" />
            {t('product.color')}: {item.colorName}
          </span>
        )}

        <div className={`flex items-center justify-between ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
          <div className="flex items-center gap-1">
            <Button
              variant="outline"
              size="icon-xs"
              className="size-6"
              onClick={() => onQuantityChange(item.quantity - 1)}
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
              onClick={() => onQuantityChange(item.quantity + 1)}
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
  );
}

const FREE_SHIPPING_THRESHOLD = 500_000;

function FreeShippingBar({ subtotal }: { subtotal: number }) {
  const { t, tva, locale } = useLocale();
  const remaining = Math.max(0, FREE_SHIPPING_THRESHOLD - subtotal);
  const pct = Math.min(100, (subtotal / FREE_SHIPPING_THRESHOLD) * 100);

  return (
    <div className="rounded-xl border border-primary/15 bg-primary/5 p-3">
      <p className="text-xs font-medium text-foreground">
        {remaining === 0
          ? t('checkout.shippingFree')
          : tva('cart.freeShippingProgress', {
              amount: formatCurrency(remaining, locale),
            })}
      </p>
      <div
        className="mt-2 h-2 overflow-hidden rounded-full bg-muted"
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.round(pct)}
      >
        <div
          className="h-full rounded-full bg-primary transition-all duration-500 ease-out"
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  );
}
