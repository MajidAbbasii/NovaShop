'use client';

import { useState, useEffect, useCallback } from 'react';
import Image from 'next/image';
import { cn } from '@/lib/utils';
import { useLocale } from '@/lib/locale-context';
import { ZoomIn, X, ChevronLeft, ChevronRight } from 'lucide-react';

interface ProductGalleryProps {
  images: string[];
  productName: string;
}

export function ProductGallery({ images, productName }: ProductGalleryProps) {
  const { t, dir } = useLocale();
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [zoomed, setZoomed] = useState(false);
  const [loaded, setLoaded] = useState(false);

  const allImages = images;

  const current = allImages[selectedIndex] ?? allImages[0];

  const prev = useCallback(() => {
    setSelectedIndex((i) => (i - 1 + allImages.length) % allImages.length);
  }, [allImages.length]);

  const next = useCallback(() => {
    setSelectedIndex((i) => (i + 1) % allImages.length);
  }, [allImages.length]);

  // Keyboard navigation when the main image has focus
  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowLeft') dir === 'rtl' ? next() : prev();
    if (e.key === 'ArrowRight') dir === 'rtl' ? prev() : next();
    if (e.key === 'Escape') setZoomed(false);
  };

  // Reset selection when the product or its images change (color switch)
  useEffect(() => {
    setSelectedIndex(0);
    setLoaded(false);
    setZoomed(false);
  }, [images, productName]);

  if (allImages.length === 0) {
    return (
      <div className="flex aspect-square items-center justify-center rounded-xl bg-muted">
        <p className="px-4 text-center text-sm text-muted-foreground">
          {t('product.noImagesForColor')}
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {/* Main image */}
      <div
        className="group relative aspect-square overflow-hidden rounded-xl bg-muted"
        role="img"
        aria-label={productName}
        tabIndex={0}
        onKeyDown={onKeyDown}
      >
        {!loaded && (
          <div className="absolute inset-0 animate-pulse bg-muted" />
        )}
        <Image
          key={current}
          src={current}
          alt={productName}
          fill
          onLoad={() => setLoaded(true)}
          className={cn(
            'object-cover transition-opacity duration-300',
            loaded ? 'opacity-100' : 'opacity-0'
          )}
          sizes="(max-width: 768px) 100vw, 50vw"
          priority
        />

        {/* Zoom button */}
        <button
          type="button"
          onClick={() => setZoomed(true)}
          aria-label={t('product.zoom')}
          className="absolute bottom-3 end-3 flex size-9 items-center justify-center rounded-full bg-white/85 text-foreground shadow-sm backdrop-blur transition hover:bg-white active:scale-90"
        >
          <ZoomIn className="size-4" />
        </button>

        {/* Prev/next arrows (multi-image) */}
        {allImages.length > 1 && (
          <>
            <button
              type="button"
              onClick={prev}
              aria-label={t('product.prev')}
              className="absolute start-3 top-1/2 hidden -translate-y-1/2 rounded-full bg-white/80 p-1.5 text-foreground shadow-sm backdrop-blur transition hover:bg-white active:scale-90 group-hover:block"
            >
              <ChevronLeft className="size-4" />
            </button>
            <button
              type="button"
              onClick={next}
              aria-label={t('product.next')}
              className="absolute end-3 top-1/2 hidden -translate-y-1/2 rounded-full bg-white/80 p-1.5 text-foreground shadow-sm backdrop-blur transition hover:bg-white active:scale-90 group-hover:block"
            >
              <ChevronRight className="size-4" />
            </button>
          </>
        )}
      </div>

      {/* Thumbnails */}
      {allImages.length > 1 && (
        <div className="flex gap-2 overflow-x-auto pb-1">
          {allImages.map((img, i) => (
            <button
              key={i}
              onClick={() => {
                setSelectedIndex(i);
                setLoaded(false);
              }}
              aria-label={`${productName} ${i + 1}`}
              aria-current={i === selectedIndex}
              className={cn(
                'relative size-16 shrink-0 overflow-hidden rounded-lg border-2 transition-all',
                i === selectedIndex
                  ? 'border-primary ring-2 ring-primary/20'
                  : 'border-transparent opacity-70 hover:opacity-100'
              )}
            >
              <Image
                src={img}
                alt=""
                fill
                className="object-cover"
                sizes="64px"
              />
            </button>
          ))}
        </div>
      )}

      {/* Zoom dialog (lightbox) */}
      {zoomed && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/85 p-4 backdrop-blur-sm"
          role="dialog"
          aria-modal="true"
          aria-label={productName}
          onClick={() => setZoomed(false)}
        >
          <button
            type="button"
            onClick={() => setZoomed(false)}
            aria-label={t('product.close')}
            className="absolute top-4 end-4 flex size-10 items-center justify-center rounded-full bg-white/10 text-white transition hover:bg-white/20"
          >
            <X className="size-5" />
          </button>
          <div className="relative h-full max-h-[85vh] w-full max-w-3xl">
            <Image
              src={current}
              alt={productName}
              fill
              className="object-contain"
              sizes="90vw"
            />
          </div>
          {allImages.length > 1 && (
            <>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  prev();
                }}
                aria-label={t('product.prev')}
                className="absolute start-4 top-1/2 flex size-10 -translate-y-1/2 items-center justify-center rounded-full bg-white/10 text-white transition hover:bg-white/20"
              >
                <ChevronLeft className="size-5" />
              </button>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  next();
                }}
                aria-label={t('product.next')}
                className="absolute end-4 top-1/2 flex size-10 -translate-y-1/2 items-center justify-center rounded-full bg-white/10 text-white transition hover:bg-white/20"
              >
                <ChevronRight className="size-5" />
              </button>
            </>
          )}
        </div>
      )}
    </div>
  );
}
