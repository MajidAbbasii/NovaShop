'use client';

import { useState } from 'react';
import type { ReviewDto } from '@/lib/review-api';
import { ReviewItem } from '@/components/ui/review-item';
import { StarRating } from '@/components/ui/star-rating';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { useLocale } from '@/lib/locale-context';
import { formatNumber } from '@/lib/formatters';

const PAGE_SIZE = 5;

interface ReviewListProps {
  reviews: ReviewDto[];
  currentUserId: number | null;
  averageRating: number;
  totalReviews: number;
  onDeleteReview?: (id: number) => Promise<void>;
}

export function ReviewList({
  reviews,
  currentUserId,
  averageRating,
  totalReviews,
  onDeleteReview,
}: ReviewListProps) {
  const { t, tva, locale } = useLocale();
  const [page, setPage] = useState(1);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  const totalPages = Math.max(1, Math.ceil(reviews.length / PAGE_SIZE));
  const paginated = reviews.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  const handleDelete = async (id: number) => {
    setDeletingId(id);
    try {
      await onDeleteReview?.(id);
    } finally {
      setDeletingId(null);
    }
  };

  if (totalReviews === 0) {
    return null;
  }

  return (
    <div className="space-y-4">
      {/* Average rating summary */}
      <div className="flex items-center gap-3 rounded-lg border bg-muted/30 px-4 py-3">
        <div className="text-3xl font-bold tabular-nums">{averageRating.toFixed(1)}</div>
        <div className="space-y-0.5">
          <StarRating rating={Math.round(averageRating)} size={14} />
          <p className="text-xs text-muted-foreground">
            {tva('review.count', { count: formatNumber(totalReviews, locale) })}
          </p>
        </div>
      </div>

      <Separator />

      {/* Review list */}
      <div className="space-y-3">
        {paginated.map((review) => (
          <ReviewItem
            key={review.id}
            review={review}
            currentUserId={currentUserId}
            onDelete={handleDelete}
            deleting={deletingId === review.id}
          />
        ))}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 pt-2">
          <Button
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
          >
            {t('pagination.previous')}
          </Button>
          <span className="text-xs text-muted-foreground tabular-nums">
            {formatNumber(page, locale)} / {formatNumber(totalPages, locale)}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            {t('pagination.next')}
          </Button>
        </div>
      )}
    </div>
  );
}
