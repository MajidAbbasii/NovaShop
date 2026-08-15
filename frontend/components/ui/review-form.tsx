'use client';

import { useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Loader2, MessageSquarePlus } from 'lucide-react';
import { useLocale } from '@/lib/locale-context';

type ReviewFormProps = {
  onSubmit: (rating: number, comment: string) => Promise<void>;
};

export function ReviewForm({ onSubmit }: ReviewFormProps) {
  const { t, tva, locale } = useLocale();
  const [rating, setRating] = useState(0);
  const [hoverRating, setHoverRating] = useState(0);
  const [submitting, setSubmitting] = useState(false);

  const reviewSchema = useMemo(
    () =>
      z.object({
        comment: z
          .string()
          .min(3, tva('review.minLength', { count: 3 }))
          .max(1000, tva('review.maxLength', { count: 1000 })),
      }),
    [tva]
  );

  type ReviewFormData = z.infer<typeof reviewSchema>;

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<ReviewFormData>({
    resolver: zodResolver(reviewSchema),
  });

  const handleFormSubmit = async (data: ReviewFormData) => {
    if (rating === 0) return;
    setSubmitting(true);
    try {
      await onSubmit(rating, data.comment);
      reset();
      setRating(0);
    } finally {
      setSubmitting(false);
    }
  };

  const displayRating = hoverRating || rating;

  const ratingLabel =
    rating === 1 ? t('review.poor')
      : rating === 2 ? t('review.fair')
      : rating === 3 ? t('review.good')
      : rating === 4 ? t('review.veryGood')
      : t('review.excellent');

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4" dir={locale === 'en' ? 'ltr' : 'rtl'}>
      <div className="space-y-1.5">
        <Label>{t('review.rating')}</Label>
        <div className="flex items-center gap-2">
          <div onMouseLeave={() => setHoverRating(0)} className="flex">
            {[1, 2, 3, 4, 5].map((star) => (
              <button
                key={star}
                type="button"
                onMouseEnter={() => setHoverRating(star)}
                onClick={() => setRating(star)}
                className="cursor-pointer p-0.5 transition-transform hover:scale-110"
                aria-label={tva('review.rateAria', { star })}
              >
                <svg
                  width="28" height="28" viewBox="0 0 24 24"
                  fill={star <= displayRating ? '#f59e0b' : 'none'}
                  stroke={star <= displayRating ? '#f59e0b' : '#d4d4d8'}
                  strokeWidth="1.5" className="transition-colors"
                >
                  <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" />
                </svg>
              </button>
            ))}
          </div>
          {rating > 0 && <span className="text-sm text-muted-foreground">{ratingLabel}</span>}
        </div>
        {rating === 0 && <p className="text-xs text-muted-foreground">{t('review.selectHint')}</p>}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="comment">{t('review.comment')}</Label>
        <textarea
          id="comment"
          rows={4}
          className="flex w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm transition-colors placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:opacity-50 resize-y"
          placeholder={t('review.commentPlaceholder')}
          {...register('comment')}
        />
        {errors.comment && <p className="text-xs text-destructive">{errors.comment.message}</p>}
      </div>

      <Button type="submit" disabled={rating === 0 || submitting} size="lg">
        {submitting ? <Loader2 className="size-4 animate-spin" /> : <MessageSquarePlus className="size-4" />}
        {submitting ? t('review.submitting') : t('review.submit')}
      </Button>
    </form>
  );
}