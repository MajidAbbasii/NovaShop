'use client';

import { useRouter } from 'next/navigation';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { toast } from '@/hooks/use-toast';
import { ShoppingCart, Heart, Loader2 } from 'lucide-react';
import { useCart } from '@/lib/cart-context';
import { useLocale } from '@/lib/locale-context';
import {
  hasToken,
  authHeaders,
  getCurrentUserId,
} from '@/lib/review-api';

import { API_GATEWAY_URL } from '@/lib/config';

const API_BASE_URL = API_GATEWAY_URL;

function getToken(): string | null {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/(?:^|;\s*)token=([^;]*)/);
  return match ? decodeURIComponent(match[1]) : null;
}

// --- AddToCartButton ---

interface AddToCartButtonProps {
  productId: number;
  stock: number;
  colorId?: number | null;
  disabled?: boolean;
}

export function AddToCartButton({ productId, stock, colorId = null, disabled = false }: AddToCartButtonProps) {
  const router = useRouter();
  const isAuth = hasToken();
  const { addItem, isAddingItem } = useCart();
  const { t } = useLocale();

  const handleClick = () => {
    if (!isAuth) {
      toast({ title: t('product.loginRequired'), variant: 'destructive' });
      router.push('/login');
      return;
    }
    addItem(productId, 1, colorId);
  };

  return (
    <Button
      size="lg"
      className="w-full gap-2 rounded-full"
      disabled={stock <= 0 || isAddingItem || disabled}
      onClick={handleClick}
    >
      {isAddingItem ? (
        <Loader2 className="size-4 animate-spin" />
      ) : (
        <ShoppingCart className="size-4" />
      )}
      {stock > 0 ? t('product.addToCart') : t('product.outOfStock')}
    </Button>
  );
}

// --- WishlistButton ---

interface WishlistButtonProps {
  productId: number;
}

export function WishlistButton({ productId }: WishlistButtonProps) {
  const router = useRouter();
  const { t } = useLocale();
  const isAuth = hasToken();
  const token = getToken();
  const queryClient = useQueryClient();

  const checkQuery = useQuery({
    queryKey: ['wishlist-check', productId],
    queryFn: async () => {
      const res = await fetch(`${API_BASE_URL}/api/wishlist/check/${productId}`, {
        headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      });
      if (!res.ok) return false;
      const data = await res.json();
      // Backend serializes { Exists: bool } → data.exists
      return data.exists ?? data.isInWishlist ?? false;
    },
    enabled: isAuth && !!token,
    staleTime: 60_000,
  });

  const addMutation = useMutation({
    mutationFn: async () => {
      const res = await fetch(`${API_BASE_URL}/api/wishlist`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ productId }),
      });
      if (!res.ok) throw new Error('Failed');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['wishlist-check', productId] });
      toast({ title: t('wishlist.added') });
    },
    onError: () => {
      toast({ title: t('wishlist.failed'), variant: 'destructive' });
    },
  });

  const removeMutation = useMutation({
    mutationFn: async () => {
      const res = await fetch(`${API_BASE_URL}/api/wishlist/${productId}`, {
        method: 'DELETE',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (!res.ok && res.status !== 204) throw new Error('Failed');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['wishlist-check', productId] });
      toast({ title: t('wishlist.removed') });
    },
    onError: () => {
      toast({ title: t('wishlist.failed'), variant: 'destructive' });
    },
  });

  const inWishlist = checkQuery.data ?? false;
  const loading = addMutation.isPending || removeMutation.isPending || checkQuery.isLoading;

  const handleToggle = () => {
    if (!isAuth) {
      router.push('/login');
      return;
    }
    if (inWishlist) {
      removeMutation.mutate();
    } else {
      addMutation.mutate();
    }
  };

  return (
    <Button
      variant="outline"
      size="lg"
      className="gap-2 rounded-full"
      onClick={handleToggle}
      disabled={loading}
    >
      {loading ? (
        <Loader2 className="size-4 animate-spin" />
      ) : (
        <Heart
          key={String(inWishlist)}
          className={`size-4 heart-pop ${inWishlist ? 'fill-red-500 text-red-500' : ''}`}
        />
      )}
      {inWishlist ? t('wishlist.in') : t('wishlist.add')}
    </Button>
  );
}
