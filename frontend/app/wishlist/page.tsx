'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { useLocale } from '@/lib/locale-context';
import { formatCurrency } from '@/lib/formatters';
import { authHeaders, isAuthenticated, addToCart } from '@/lib/cart-api';
import { API_GATEWAY_URL } from '@/lib/config';
import { Heart, Loader2, ShoppingBag, Trash2 } from 'lucide-react';

interface WishlistItem {
  id: number;
  productId: number;
  productName: string;
  productPrice: number;
  productImageUrl: string;
  addedAt: string;
}

export default function WishlistPage() {
  const router = useRouter();
  const { t, dir, locale } = useLocale();
  const [items, setItems] = useState<WishlistItem[] | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace('/login');
      return;
    }
    (async () => {
      try {
        const res = await fetch(`${API_GATEWAY_URL}/api/wishlist?pageNumber=1&pageSize=50`, {
          headers: authHeaders(),
        });
        if (!res.ok) throw new Error('Failed');
        const data = await res.json();
        setItems(data.items ?? []);
      } catch {
        setError(true);
      }
    })();
  }, [router]);

  const removeItem = async (productId: number) => {
    try {
      await fetch(`${API_GATEWAY_URL}/api/wishlist/${productId}`, {
        method: 'DELETE',
        headers: authHeaders(),
      });
      setItems((prev) => prev?.filter((i) => i.productId !== productId) ?? []);
    } catch {
      // swallow; state unchanged
    }
  };

  const handleAddToCart = (productId: number) => {
    addToCart(productId, 1).catch(() => {});
  };

  if (items === null && !error) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-amber-600" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-amber-50/30 py-10">
      <div className="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
        <h1 className={`mb-6 text-2xl font-bold ${dir === 'rtl' ? 'text-right' : ''}`}>
          {t('wishlist.title')}
        </h1>

        {error ? (
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-12 text-center text-muted-foreground">
              {t('error.loadFailed')}
            </CardContent>
          </Card>
        ) : items && items.length === 0 ? (
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-16 text-center">
              <Heart className="mx-auto mb-4 size-12 text-muted-foreground/50" />
              <p className="text-lg font-medium">{t('wishlist.empty')}</p>
              <p className="mt-1 text-sm text-muted-foreground">{t('wishlist.empty.desc')}</p>
              <Button className="mt-6 rounded-full" asChild>
                <Link href="/products">{t('cart.continue')}</Link>
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {items?.map((item) => (
              <Card key={item.id} className="overflow-hidden rounded-2xl border-0 shadow-sm">
                <Link href={`/products/${item.productId}`} className="block">
                  <div className="relative aspect-square w-full overflow-hidden bg-muted">
                    {item.productImageUrl ? (
                      <Image
                        src={item.productImageUrl}
                        alt={item.productName}
                        fill
                        className="object-cover transition-transform hover:scale-105"
                        sizes="(max-width: 768px) 50vw, 33vw"
                      />
                    ) : (
                      <div className="flex size-full items-center justify-center text-muted-foreground/40">
                        <ShoppingBag className="size-8" />
                      </div>
                    )}
                  </div>
                </Link>
                <CardContent className="p-4">
                  <Link
                    href={`/products/${item.productId}`}
                    className="block truncate text-sm font-medium hover:underline"
                  >
                    {item.productName}
                  </Link>
                  <div className="mt-1 text-sm font-semibold">
                    {formatCurrency(item.productPrice, locale)}
                  </div>
                  <div className="mt-3 flex items-center gap-2">
                    <Button
                      size="sm"
                      className="flex-1 gap-1 rounded-full"
                      onClick={() => handleAddToCart(item.productId)}
                    >
                      <ShoppingBag className="size-3.5" />
                      {t('wishlist.inCart')}
                    </Button>
                    <Button
                      variant="outline"
                      size="icon"
                      className="size-9 shrink-0 text-muted-foreground hover:text-destructive"
                      onClick={() => removeItem(item.productId)}
                      aria-label={t('wishlist.remove')}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}