'use client';

import Image from 'next/image';
import Link from 'next/link';
import { useState } from 'react';
import { cn } from '@/lib/utils';
import { resolveImageUrl } from '@/lib/config';
import { useLocale } from '@/lib/locale-context';
import { formatCurrency } from '@/lib/formatters';
import { useCart } from '@/lib/cart-context';
import { useAuth } from '@/lib/auth-context';
import { toast } from '@/hooks/use-toast';
import { ShoppingCart, Heart, Loader2, Check, Star } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { API_GATEWAY_URL } from '@/lib/config';

export interface StoreProduct {
  id: number;
  name: string;
  price: number;
  originalPrice?: number;
  imageUrl?: string;
  rating?: number;
  stock?: number;
  categoryId?: number;
}

interface StoreProductCardProps {
  product: StoreProduct;
  className?: string;
}

/** Warm, premium product card used across the storefront grid. */
export function StoreProductCard({ product, className }: StoreProductCardProps) {
  const { t, tva, dir, locale } = useLocale();
  const { addItem, isAddingItem } = useCart();
  const { isAuthenticated } = useAuth();
  const [justAdded, setJustAdded] = useState(false);
  const queryClient = useQueryClient();
  const token = typeof document !== 'undefined'
    ? document.cookie.match(/(?:^|;\s*)token=([^;]*)/)?.[1] ?? null
    : null;

  const wishlistQuery = useQuery({
    queryKey: ['wishlist-check', product.id],
    queryFn: async () => {
      const res = await fetch(`${API_GATEWAY_URL}/api/wishlist/check/${product.id}`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (!res.ok) return false;
      const data = await res.json();
      return data.exists ?? data.isInWishlist ?? false;
    },
    enabled: isAuthenticated && !!token,
    staleTime: 60_000,
  });
  const inWishlist = wishlistQuery.data ?? false;

  const wishlistMutation = useMutation({
    mutationFn: async () => {
      if (inWishlist) {
        const res = await fetch(`${API_GATEWAY_URL}/api/wishlist/${product.id}`, {
          method: 'DELETE',
          headers: token ? { Authorization: `Bearer ${token}` } : {},
        });
        if (!res.ok && res.status !== 204) throw new Error('Failed');
      } else {
        const res = await fetch(`${API_GATEWAY_URL}/api/wishlist`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify({ productId: product.id }),
        });
        if (!res.ok) throw new Error('Failed');
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['wishlist-check', product.id] });
      toast({ title: inWishlist ? t('wishlist.removed') : t('wishlist.added') });
    },
    onError: () => {
      toast({ title: t('wishlist.failed'), variant: 'destructive' });
    },
  });

  const handleWishlist = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!isAuthenticated) {
      toast({ title: t('product.loginRequired'), variant: 'destructive' });
      return;
    }
    wishlistMutation.mutate();
  };

  const discountPercent =
    product.originalPrice && product.originalPrice > product.price
      ? Math.round(((product.originalPrice - product.price) / product.originalPrice) * 100)
      : 0;
  const inStock = (product.stock ?? 0) > 0;
  const img = resolveImageUrl(product.imageUrl) ||
    `https://picsum.photos/seed/doll-${product.id}/400/400`;

  const handleAdd = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!isAuthenticated) {
      toast({ title: t('product.loginRequired'), variant: 'destructive' });
      return;
    }
    addItem(product.id);
    setJustAdded(true);
    window.setTimeout(() => setJustAdded(false), 1200);
  };

  return (
    <Link
      href={`/products/${product.id}`}
      className={cn(
        'group relative flex flex-col overflow-hidden rounded-2xl border border-border/60 bg-card shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-lg',
        className
      )}
    >
      {/* Image */}
      <div className="relative aspect-square overflow-hidden bg-amber-50">
        <Image
          src={img}
          alt={product.name}
          fill
          sizes="(max-width: 640px) 50vw, 25vw"
          className="object-cover transition-transform duration-500 ease-out group-hover:scale-110"
        />
        {discountPercent > 0 && (
          <span className="absolute top-3 start-3 rounded-full bg-destructive px-2.5 py-1 text-xs font-bold text-white shadow-sm">
            {tva('product.discountBadge', { percent: discountPercent })}
          </span>
        )}
        {!inStock && (
          <span className="absolute inset-0 flex items-center justify-center bg-black/40 text-sm font-semibold text-white backdrop-blur-[1px]">
            {t('product.outOfStock')}
          </span>
        )}
      </div>

      {/* Body */}
      <div className="flex flex-1 flex-col gap-1 p-3">
        <h3 className="truncate text-sm font-semibold">{product.name}</h3>
        {typeof product.rating === 'number' && (
          <div className="flex items-center gap-1 text-xs text-muted-foreground">
            <Star className="size-3 fill-amber-400 text-amber-400" />
            {product.rating.toFixed(1)}
          </div>
        )}

        <div className="mt-auto flex items-baseline gap-1.5 pt-1">
          <span className="text-lg font-bold text-primary">
            {formatCurrency(product.price, locale)}
          </span>
          {discountPercent > 0 && (
            <span className="text-xs text-muted-foreground line-through">
              {formatCurrency(product.originalPrice!, locale)}
            </span>
          )}
        </div>

        {/* Actions */}
        <div className="mt-2 flex items-center gap-2">
          <button
            type="button"
            onClick={handleAdd}
            disabled={!inStock || isAddingItem}
            aria-label={t('product.addToCart')}
            className={cn(
              'flex h-9 flex-1 items-center justify-center gap-1.5 rounded-full text-xs font-semibold transition-all active:scale-95',
              justAdded
                ? 'bg-green-600 text-white'
                : 'bg-primary text-primary-foreground hover:bg-primary/90',
              (!inStock || isAddingItem) && 'cursor-not-allowed opacity-50'
            )}
          >
            {isAddingItem ? (
              <Loader2 className="size-3.5 animate-spin" />
            ) : justAdded ? (
              <Check className="size-3.5" />
            ) : (
              <ShoppingCart className="size-3.5" />
            )}
            {justAdded ? t('product.added') : inStock ? t('product.addToCart') : t('product.outOfStock')}
          </button>

          <button
            type="button"
            onClick={handleWishlist}
            disabled={wishlistMutation.isPending}
            aria-label={inWishlist ? t('wishlist.remove') : t('wishlist.add')}
            className={cn(
              'flex size-9 shrink-0 items-center justify-center rounded-full border transition-all active:scale-90',
              inWishlist
                ? 'border-rose-200 bg-rose-50 text-rose-500'
                : 'border-border/70 text-muted-foreground hover:border-rose-200 hover:text-rose-500'
            )}
          >
            <Heart
              key={String(inWishlist)}
              className={cn('size-4 heart-pop', inWishlist && 'fill-rose-500 text-rose-500')}
            />
          </button>
        </div>
      </div>
    </Link>
  );
}
