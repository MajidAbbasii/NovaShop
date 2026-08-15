'use client';

import type { ReviewDto } from '@/lib/review-api';
import { StarRating } from '@/components/ui/star-rating';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Trash2 } from 'lucide-react';
import { useLocale } from '@/lib/locale-context';

interface ReviewItemProps {
  review: ReviewDto;
  currentUserId: number | null;
  onDelete?: (id: number) => void;
  deleting?: boolean;
}

export function ReviewItem({ review, currentUserId, onDelete, deleting }: ReviewItemProps) {
  const { t, tva, locale } = useLocale();
  const isOwn = currentUserId === review.userId;

  const formatDate = (iso: string) => {
    const d = new Date(iso);
    if (isNaN(d.getTime())) return iso;
    return new Intl.DateTimeFormat(locale, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    }).format(d);
  };

  return (
    <div className="flex gap-3 rounded-lg border bg-card p-4 text-sm">
      <Avatar className="mt-0.5 size-9 shrink-0">
        <AvatarFallback>{`U${review.userId}`}</AvatarFallback>
      </Avatar>

      <div className="min-w-0 flex-1 space-y-1.5">
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <span className="font-medium text-foreground">
              {review.userName ?? tva('review.user', { id: review.userId })}
            </span>
            <span className="text-xs text-muted-foreground">
              {formatDate(review.createdAt)}
            </span>
          </div>

          {isOwn && onDelete && (
            <Button
              variant="ghost"
              size="icon-xs"
              onClick={() => onDelete(review.id)}
              disabled={deleting}
              aria-label={t('review.deleteAria')}
              className="text-muted-foreground hover:text-destructive"
            >
              <Trash2 className="size-3.5" />
            </Button>
          )}
        </div>

        <StarRating rating={review.rating} size={14} />

        {review.comment && (
          <p className="text-muted-foreground leading-relaxed">
            {review.comment}
          </p>
        )}
      </div>
    </div>
  );
}