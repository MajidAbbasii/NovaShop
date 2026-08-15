'use client';

import {
  createContext,
  useContext,
  useState,
  useCallback,
  type ReactNode,
} from 'react';
import {
  useQuery,
  useMutation,
  useQueryClient,
} from '@tanstack/react-query';
import {
  fetchCart,
  addToCart,
  updateCartItemQuantity,
  removeCartItem,
  clearCart,
  isAuthenticated,
  type CartDto,
} from '@/lib/cart-api';
import { toast } from '@/hooks/use-toast';
import { useLocale } from '@/lib/locale-context';

interface CartContextValue {
  cart: CartDto;
  isLoading: boolean;
  itemCount: number;
  sheetOpen: boolean;
  openSheet: () => void;
  closeSheet: () => void;
  addItem: (productId: number, quantity?: number, colorId?: number | null) => void;
  updateQuantity: (cartItemId: number, quantity: number) => void;
  removeItem: (cartItemId: number) => void;
  clearAll: () => void;
  isAddingItem: boolean;
}

const CartContext = createContext<CartContextValue | null>(null);

export function CartProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [sheetOpen, setSheetOpen] = useState(false);
  const { t } = useLocale();

  const queryKey = ['cart'];

  const { data: cart, isLoading } = useQuery<CartDto>({
    queryKey,
    queryFn: fetchCart,
    enabled: isAuthenticated(),
    staleTime: 30_000,
    refetchOnMount: true,
    refetchOnWindowFocus: true,
  });

  const itemCount = (cart?.items ?? []).reduce(
    (sum, i) => sum + i.quantity,
    0
  );

  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey });
  }, [queryClient]);

  const addMutation = useMutation({
    mutationFn: ({ productId, qty, colorId }: { productId: number; qty: number; colorId?: number | null }) =>
      addToCart(productId, qty, colorId),
    onSuccess: () => {
      invalidate();
      toast({ title: t('cart.added') });
    },
    onError: () => {
      toast({ title: t('cart.addFailed'), variant: 'destructive' });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({
      cartItemId,
      quantity,
    }: {
      cartItemId: number;
      quantity: number;
    }) => updateCartItemQuantity(cartItemId, quantity),
    onMutate: async ({ cartItemId, quantity }) => {
      // Optimistic update
      await queryClient.cancelQueries({ queryKey });
      const previous = queryClient.getQueryData<CartDto>(queryKey);
      if (previous) {
        queryClient.setQueryData<CartDto>(queryKey, {
          ...previous,
          items: previous.items.map((i) =>
            i.id === cartItemId ? { ...i, quantity } : i
          ),
          totalAmount: previous.items.reduce(
            (sum, i) =>
              sum +
              (i.id === cartItemId ? quantity : i.quantity) * i.unitPrice,
            0
          ),
        });
      }
      return { previous };
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKey, context.previous);
      }
      toast({ title: t('cart.updateFailed'), variant: 'destructive' });
    },
    onSettled: () => invalidate(),
  });

  const removeMutation = useMutation({
    mutationFn: (cartItemId: number) => removeCartItem(cartItemId),
    onMutate: async (cartItemId) => {
      await queryClient.cancelQueries({ queryKey });
      const previous = queryClient.getQueryData<CartDto>(queryKey);
      if (previous) {
        queryClient.setQueryData<CartDto>(queryKey, {
          ...previous,
          items: previous.items.filter((i) => i.id !== cartItemId),
          totalAmount: previous.items
            .filter((i) => i.id !== cartItemId)
            .reduce((sum, i) => sum + i.unitPrice * i.quantity, 0),
        });
      }
      return { previous };
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKey, context.previous);
      }
      toast({ title: t('cart.removeFailed'), variant: 'destructive' });
    },
    onSettled: () => invalidate(),
  });

  const clearMutation = useMutation({
    mutationFn: clearCart,
    onSuccess: () => {
      invalidate();
      toast({ title: t('cart.cleared') });
    },
    onError: () => {
      toast({ title: t('cart.clearFailed'), variant: 'destructive' });
    },
  });

  const addItem = useCallback(
    (productId: number, quantity = 1, colorId?: number | null) => {
      if (!isAuthenticated()) {
        toast({ title: t('error.loginRequired'), variant: 'destructive' });
        return;
      }
      addMutation.mutate({ productId, qty: quantity, colorId });
    },
    [addMutation, t]
  );

  const updateQuantity = useCallback(
    (cartItemId: number, quantity: number) => {
      if (quantity <= 0) {
        removeMutation.mutate(cartItemId);
        return;
      }
      updateMutation.mutate({ cartItemId, quantity });
    },
    [updateMutation, removeMutation]
  );

  const removeItem = useCallback(
    (cartItemId: number) => removeMutation.mutate(cartItemId),
    [removeMutation]
  );

  const clearAll = useCallback(() => clearMutation.mutate(), [clearMutation]);

  const openSheet = useCallback(() => setSheetOpen(true), []);
  const closeSheet = useCallback(() => setSheetOpen(false), []);

  return (
    <CartContext.Provider
      value={{
        cart: cart ?? { id: 0, userId: 0, totalAmount: 0, items: [] },
        isLoading,
        itemCount,
        sheetOpen,
        openSheet,
        closeSheet,
        addItem,
        updateQuantity,
        removeItem,
        clearAll,
        isAddingItem: addMutation.isPending,
      }}
    >
      {children}
    </CartContext.Provider>
  );
}

export function useCart(): CartContextValue {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error('useCart must be used inside <CartProvider>');
  return ctx;
}
