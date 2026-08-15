'use client';

import type { Product } from '@/lib/api';
import { useLocale } from '@/lib/locale-context';
import { StoreProductCard } from '@/components/store-product-card';

interface RelatedProductsProps {
  products: Product[];
  currentId: number;
  categoryId?: number;
}

export function RelatedProducts({
  products,
  currentId,
  categoryId,
}: RelatedProductsProps) {
  const { t } = useLocale();
  let related = products.filter((p) => p.id !== currentId);

  if (categoryId) {
    const sameCat = related.filter(
      (p) => p.categoryId === categoryId
    );
    if (sameCat.length > 0) related = sameCat;
  }

  related = related.slice(0, 4);

  if (related.length === 0) return null;

  return (
    <section className="space-y-4">
      <h2 className="text-xl font-semibold">{t('product.related')}</h2>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
        {related.map((p) => (
          <StoreProductCard key={p.id} product={p} />
        ))}
      </div>
    </section>
  );
}
