'use client';

import { Star } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useLocale } from '@/lib/locale-context';

interface StarRatingProps {
  rating: number;
  maxRating?: number;
  size?: number;
  interactive?: boolean;
  onChange?: (rating: number) => void;
  showValue?: boolean;
}

export function StarRating({
  rating,
  maxRating = 5,
  size = 16,
  interactive = false,
  onChange,
  showValue = false,
}: StarRatingProps) {
  const { tva } = useLocale();
  return (
    <div className="flex items-center gap-0.5">
      {Array.from({ length: maxRating }, (_, i) => {
        const starValue = i + 1;
        const filled = starValue <= rating;
        const half = !filled && starValue - 0.5 <= rating;
        return (
          <button
            key={i}
            type="button"
            disabled={!interactive}
            onClick={() => interactive && onChange?.(starValue)}
            className={cn(
              'transition-colors',
              interactive
                ? 'cursor-pointer hover:scale-110'
                : 'cursor-default',
              interactive && 'disabled:cursor-default'
            )}
            aria-label={tva('review.rateAria', { star: starValue })}
          >
            <Star
              size={size}
              className={cn(
                'transition-all',
                filled
                  ? 'fill-amber-400 text-amber-400'
                  : half
                    ? 'fill-amber-400/30 text-amber-400/50'
                    : 'fill-transparent text-muted-foreground/30'
              )}
            />
          </button>
        );
      })}
      {showValue && (
        <span className="ml-1.5 text-sm font-medium text-muted-foreground">
          {rating.toFixed(1)}
        </span>
      )}
    </div>
  );
}
