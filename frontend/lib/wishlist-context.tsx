'use client';

import {
  createContext,
  useContext,
  useCallback,
  type ReactNode,
} from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { authHeaders, isAuthenticated } from '@/lib/cart-api';
import { API_GATEWAY_URL } from '@/lib/config';

interface WishlistContextValue {
  wishlistCount: number;
  isLoading: boolean;
  refetch: () => void;
}

interface PagedWishlist {
  totalCount: number;
}

const WishlistContext = createContext<WishlistContextValue | null>(null);

export function WishlistProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const queryKey = ['wishlist', 'count'];

  const { data, isLoading } = useQuery({
    queryKey,
    queryFn: async () => {
      // pageSize=1 so we only read totalCount, not the full list
      const res = await fetch(`${API_GATEWAY_URL}/api/wishlist?pageNumber=1&pageSize=1`, {
        headers: { ...authHeaders() },
      });
      if (!res.ok) throw new Error('Failed to fetch wishlist');
      return (await res.json()) as PagedWishlist;
    },
    enabled: isAuthenticated(),
    staleTime: 30_000,
    refetchOnMount: true,
    refetchOnWindowFocus: true,
  });

  const refetch = useCallback(() => {
    queryClient.invalidateQueries({ queryKey });
  }, [queryClient]);

  return (
    <WishlistContext.Provider
      value={{
        wishlistCount: data?.totalCount ?? 0,
        isLoading,
        refetch,
      }}
    >
      {children}
    </WishlistContext.Provider>
  );
}

export function useWishlist(): WishlistContextValue {
  const ctx = useContext(WishlistContext);
  if (!ctx) throw new Error('useWishlist must be used inside <WishlistProvider>');
  return ctx;
}
