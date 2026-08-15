'use client';

import { useState, useEffect, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import type { Product } from '@/lib/api';
import type { ReviewDto } from '@/lib/review-api';
import {
  getProductReviews,
  createReview,
  deleteReview,
  hasToken,
  getCurrentUserId,
} from '@/lib/review-api';
import { ProductGallery } from '@/components/ui/product-gallery';
import { StarRating } from '@/components/ui/star-rating';
import { ReviewList } from '@/components/ui/review-list';
import { ReviewForm } from '@/components/ui/review-form';
import { RelatedProducts } from '@/components/ui/related-products';
import { AddToCartButton, WishlistButton } from '@/components/ui/product-actions';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { resolveImageUrl } from '@/lib/config';
import { useLocale } from '@/lib/locale-context';
import { formatCurrency } from '@/lib/formatters';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Separator } from '@/components/ui/separator';
import { toast } from '@/hooks/use-toast';
import {
  ChevronLeft,
  Star,
  MessageSquarePlus,
  LogIn,
  Package,
  ShieldCheck,
  Truck,
  Heart,
  Check,
} from 'lucide-react';

interface Props {
  product: Product;
  category?: Product['category'] | null;
  initialRelatedProducts: Product[];
}

export default function ProductDetailClient({
  product,
  category: categoryProp,
  initialRelatedProducts,
}: Props) {
  const { t, dir, locale } = useLocale();
  const productCategory = categoryProp ?? product.category;
  const router = useRouter();
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [selectedColorId, setSelectedColorId] = useState<number | null>(null);

  const colors = product.colors ?? [];
  const hasColors = colors.length > 0;
  const availableColors = useMemo(
    () => colors.filter((c) => c.isActive && (c.stock ?? 0) > 0),
    [colors]
  );
  const selectedColor = colors.find((c) => c.id === selectedColorId) ?? null;

  // Auto-select first available color, or first active color
  useEffect(() => {
    if (hasColors && selectedColorId === null) {
      const first = availableColors[0] ?? colors.find((c) => c.isActive);
      if (first) setSelectedColorId(first.id);
    }
  }, [hasColors, availableColors, colors, selectedColorId]);

  // Effective stock/price: per-color when a color is selected
  const effectiveStock = selectedColor ? (selectedColor.stock ?? 0) : product.stock;
  const effectivePrice = selectedColor && selectedColor.price != null ? selectedColor.price : product.price;

  // Images: when a color is selected, show ONLY that color's images;
  // otherwise fall back to product-level images (null colorId or legacy).
  const images = useMemo(() => {
    if (selectedColor) {
      const colorImages = (selectedColor.images ?? []).slice().sort((a, b) => a.displayOrder - b.displayOrder);
      if (colorImages.length > 0) return colorImages.map((i) => resolveImageUrl(i.url));
      // color without images: fall back to product-level images (no color) if any
      const productLevel = (product.images ?? []).filter((i) => i.productColorId == null).sort((a, b) => a.displayOrder - b.displayOrder);
      if (productLevel.length > 0) return productLevel.map((i) => resolveImageUrl(i.url));
      return [];
    }
    const all = (product.images ?? []).slice().sort((a, b) => a.displayOrder - b.displayOrder);
    if (all.length > 0) return all.map((i) => resolveImageUrl(i.url));
    return product.imageUrl ? [resolveImageUrl(product.imageUrl)] : [];
  }, [selectedColor, product.images, product.imageUrl]);

  const currentUserId = getCurrentUserId();
  const isAuth = hasToken();

  const {
    data: reviews,
    isLoading: reviewsLoading,
    refetch: refetchReviews,
  } = useQuery<ReviewDto[]>({
    queryKey: ['reviews', product.id],
    queryFn: () => getProductReviews(product.id),
    initialData: product.reviews ?? [],
    staleTime: 30_000,
  });

  const reviewCount = reviews?.length ?? 0;
  const totalRating =
    reviews && reviews.length > 0
      ? reviews.reduce((s, r) => s + r.rating, 0)
      : 0;
  const averageRating =
    reviews && reviews.length > 0
      ? totalRating / reviewCount
      : product.rating;

  const handleSubmitReview = async (rating: number, comment: string) => {
    if (!currentUserId) {
      toast({ title: t('error.loginRequired'), variant: 'destructive' });
      return;
    }
    try {
      await createReview({
        productId: product.id,
        userId: currentUserId,
        rating,
        comment,
      });
      toast({ title: t('review.submitted') });
      setAddDialogOpen(false);
      refetchReviews();
    } catch (e: unknown) {
      toast({
        title: t('review.submitFailed'),
        description: e instanceof Error ? e.message : t('error.generic'),
        variant: 'destructive',
      });
    }
  };

  const handleDeleteReview = async (reviewId: number) => {
    try {
      await deleteReview(reviewId);
      toast({ title: t('review.deleted') });
      refetchReviews();
    } catch (e: unknown) {
      toast({
        title: t('review.deleteFailed'),
        description: e instanceof Error ? e.message : t('error.generic'),
        variant: 'destructive',
      });
    }
  };

  const BackIcon = dir === 'rtl' ? ChevronLeft : ChevronLeft;

  return (
    <div className="min-h-screen bg-amber-50/30 py-8">
      <div className="mx-auto max-w-6xl space-y-6 px-4 sm:px-6 lg:px-8">
        {/* Back button */}
        <div className={`flex items-center ${dir === 'rtl' ? 'flex-row-reverse justify-between' : 'justify-between'}`}>
          <div />
          <Button
            variant="ghost"
            size="sm"
            onClick={() => router.push('/products')}
            className="gap-1.5"
          >
            <BackIcon className="size-4" />
            {t('product.back')}
          </Button>
        </div>

        {/* Product card */}
        <Card className="overflow-hidden border-0 rounded-2xl shadow-md">
          <CardContent className={`grid gap-8 p-6 lg:grid-cols-2 ${dir === 'rtl' ? 'lg:direction-rtl' : ''}`}>
            <ProductGallery images={images} productName={product.name} />

            <div className={`flex flex-col gap-4 ${dir === 'rtl' ? 'text-right' : ''}`}>
              <div>
                <h1 className="text-2xl font-bold">{product.name}</h1>
                {productCategory && (
                  <Link
                    href={`/products?category=${productCategory.id}`}
                    className="text-xs text-muted-foreground hover:text-primary"
                  >
                    {productCategory.name}
                  </Link>
                )}
              </div>

              <div className={`flex items-center gap-2 ${dir === 'rtl' ? 'flex-row-reverse justify-end' : ''}`}>
                <StarRating
                  rating={Math.round(averageRating)}
                  size={16}
                  showValue
                />
                <span className="text-xs text-muted-foreground">
                  ({reviewCount})
                </span>
              </div>

              <div className={`flex items-baseline gap-3 ${dir === 'rtl' ? 'flex-row-reverse justify-end' : ''}`}>
                <span className="text-3xl font-bold text-amber-700">
                  {formatCurrency(effectivePrice, locale)}
                </span>
                {product.originalPrice &&
                  product.originalPrice > effectivePrice && (
                    <span className="text-lg text-muted-foreground line-through">
                      {formatCurrency(product.originalPrice, locale)}
                    </span>
                  )}
              </div>

              <Separator />

              {product.description && (
                <p className="text-sm leading-relaxed text-muted-foreground">
                  {product.description}
                </p>
              )}

              {/* Handmade note */}
              <div className="rounded-lg bg-amber-50 p-3 text-sm">
                <p className="flex items-center gap-2">
                  <Heart className="size-4 text-rose-500 shrink-0" />
                  {t('product.handmadeNote')}
                </p>
              </div>

              <div className={`flex items-center gap-2 text-sm ${dir === 'rtl' ? 'flex-row-reverse justify-end' : ''}`}>
                {effectiveStock > 0 ? (
                  <>
                    <Package className="size-4 text-green-600" />
                    <span className="font-medium text-green-600">
                      {t('product.inStock')} ({effectiveStock})
                    </span>
                  </>
                ) : (
                  <span className="font-medium text-destructive">
                    {t('product.outOfStock')}
                  </span>
                )}
              </div>

              {hasColors && (
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">{t('product.selectColor')}</span>
                    {selectedColor && (
                      <span className="text-xs text-muted-foreground">{selectedColor.name}</span>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-2" role="radiogroup" aria-label={t('product.selectColor')}>
                    {colors.map((c) => {
                      const disabled = !c.isActive || c.stock <= 0;
                      const selected = c.id === selectedColorId;
                      return (
                        <button
                          key={c.id}
                          type="button"
                          role="radio"
                          aria-checked={selected}
                          aria-label={`${c.name}${selected ? '، انتخاب شده' : ''}${disabled ? '، ناموجود' : ''}`}
                          disabled={disabled}
                          onClick={() => setSelectedColorId(c.id)}
                          className={cn(
                            'flex items-center gap-2 rounded-full border-2 px-3 py-1.5 text-sm transition',
                            selected
                              ? 'border-primary bg-primary/10 font-semibold'
                              : 'border-border hover:border-primary/40',
                            disabled && 'cursor-not-allowed opacity-40 hover:border-border'
                          )}
                        >
                          <span
                            className="size-4 rounded-full border border-black/10"
                            style={{ backgroundColor: c.hexCode || '#ccc' }}
                            aria-hidden="true"
                          />
                          {c.name}
                          {selected && <Check className="size-3.5 text-primary" />}
                        </button>
                      );
                    })}
                  </div>
                  {hasColors && availableColors.length === 0 && (
                    <p className="text-xs font-medium text-destructive">{t('product.outOfStock')}</p>
                  )}
                </div>
              )}

              <div className={`flex gap-3 pt-2 ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
                <div className="flex-1">
                  <AddToCartButton
                    productId={product.id}
                    stock={effectiveStock}
                    colorId={selectedColorId}
                    disabled={hasColors && !selectedColor}
                  />
                </div>
                <WishlistButton productId={product.id} />
              </div>

              <div className={`mt-2 flex flex-wrap gap-4 text-xs text-muted-foreground ${dir === 'rtl' ? 'flex-row-reverse justify-end' : ''}`}>
                <span className="flex items-center gap-1">
                  <ShieldCheck className="size-3.5" />
                  {t('product.paymentNote')}
                </span>
                <span className="flex items-center gap-1">
                  <Truck className="size-3.5" />
                  {t('product.shippingNote')}
                </span>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Reviews section */}
        <section className="space-y-5">
          <div className={`flex items-center justify-between ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
            <h2 className="flex items-center gap-2 text-xl font-semibold">
              <Star className="size-5" />
              {t('product.reviews')}
            </h2>

            {isAuth ? (
              <Dialog open={addDialogOpen} onOpenChange={setAddDialogOpen}>
                <DialogTrigger asChild>
                  <Button size="sm" className="gap-1.5 rounded-full">
                    <MessageSquarePlus className="size-4" />
                    {t('product.writeReview')}
                  </Button>
                </DialogTrigger>
                <DialogContent className="sm:max-w-md">
                  <DialogHeader>
                    <DialogTitle>{t('product.writeReview')}</DialogTitle>
                    <DialogDescription>
                      {t('review.dialogDesc')}
                    </DialogDescription>
                  </DialogHeader>
                  <ReviewForm onSubmit={handleSubmitReview} />
                </DialogContent>
              </Dialog>
            ) : (
              <Button
                variant="outline"
                size="sm"
                className="gap-1.5 rounded-full"
                onClick={() => router.push('/login')}
              >
                <LogIn className="size-4" />
                {t('product.loginToReview')}
              </Button>
            )}
          </div>

          <Separator />

          {reviewsLoading ? (
            <p className="text-sm text-muted-foreground">{t('loading.products')}</p>
          ) : (
            <ReviewList
              reviews={reviews ?? []}
              currentUserId={currentUserId}
              averageRating={averageRating}
              totalReviews={reviewCount}
              onDeleteReview={handleDeleteReview}
            />
          )}

          {!reviewsLoading && (!reviews || reviews.length === 0) && (
            <div className="rounded-lg border border-dashed py-12 text-center">
              <Star className="mx-auto mb-2 size-8 text-muted-foreground/50" />
              <p className="text-sm text-muted-foreground">
                {t('review.empty')}
              </p>
            </div>
          )}
        </section>

        <RelatedProducts
          products={initialRelatedProducts}
          currentId={product.id}
          categoryId={product.categoryId}
        />
      </div>
    </div>
  );
}
