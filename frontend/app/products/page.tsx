'use client';

import { Suspense, useState, useEffect, useCallback, useTransition } from 'react';
import { useSearchParams, useRouter, usePathname } from 'next/navigation';
import { getProducts, type Product } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { CategoryFilter } from '@/components/ui/category-filter';
import { useLocale } from '@/lib/locale-context';
import { formatNumber } from '@/lib/formatters';
import { Search, Package } from 'lucide-react';
import Link from 'next/link';
import { StoreProductCard } from '@/components/store-product-card';

function ProductsContent() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { t, dir, locale } = useLocale();
  const [isPending, startTransition] = useTransition();

  const [products, setProducts] = useState<Product[]>([]);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const [searchInput, setSearchInput] = useState(searchParams.get('search') ?? '');
  const page = Number(searchParams.get('page')) || 1;
  const selectedCategoryId = searchParams.get('category')
    ? Number(searchParams.get('category'))
    : null;

  const updateParam = useCallback(
    (key: string, value: string | null) => {
      const params = new URLSearchParams(searchParams.toString());
      if (value === null) params.delete(key);
      else params.set(key, value);
      if (key !== 'page') params.set('page', '1');
      startTransition(() => {
        router.push(`${pathname}?${params.toString()}`, { scroll: false });
      });
    },
    [router, pathname, searchParams]
  );

  useEffect(() => {
    getProducts({
      searchTerm: searchParams.get('search') || undefined,
      categoryId: selectedCategoryId ?? undefined,
      pageNumber: page,
      pageSize: 12,
    })
      .then((data) => {
        setProducts(data.items);
        setTotalPages(data.totalPages);
        setTotalCount(data.totalCount);
      })
      .catch(() => {
        setProducts([]);
        setTotalPages(1);
        setTotalCount(0);
      });
  }, [searchParams, selectedCategoryId, page]);

  const handleSearch = () => {
    updateParam('search', searchInput.trim() || null);
  };

  const handleCategorySelect = (id: number | null) => {
    updateParam('category', id !== null ? String(id) : null);
  };

  return (
    <div className="min-h-screen bg-amber-50/30 py-12">
      <div className="max-w-7xl mx-auto px-6">
        <h1 className="text-3xl font-bold mb-6">{t('header.shop')}</h1>

        {/* Search bar */}
        <div className="bg-card p-6 rounded-2xl shadow-sm mb-6">
          <div className={`flex items-center gap-3 ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
            <Search className="size-4 text-muted-foreground shrink-0" />
            <Input
              placeholder={t('search.placeholder')}
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
              className="max-w-md"
            />
            <Button variant="outline" size="sm" onClick={handleSearch}>
              {t('search.button')}
            </Button>
          </div>
        </div>

        <div className={`flex gap-6 ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
          {/* Sidebar */}
          <aside className="hidden w-56 shrink-0 md:block">
            <div className="sticky top-4 space-y-4">
              <div className="rounded-xl border bg-card p-4 shadow-sm">
                <CategoryFilter
                  selectedId={selectedCategoryId}
                  onSelect={handleCategorySelect}
                />
              </div>
            </div>
          </aside>

          {/* Main content */}
          <main className="flex-1 min-w-0">
            <div className={`mb-4 flex flex-wrap items-center justify-between gap-2 ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
              <span className="text-sm text-muted-foreground">
                {formatNumber(totalCount, locale)} {t('cart.items')}
              </span>
            </div>

            {isPending && (
              <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                {Array.from({ length: 8 }).map((_, i) => (
                  <div key={i} className="animate-pulse rounded-xl bg-card p-4 shadow-sm">
                    <div className="mb-2 h-40 rounded-lg bg-muted" />
                    <div className="mb-1 h-4 w-3/4 rounded bg-muted" />
                    <div className="h-5 w-1/3 rounded bg-muted" />
                  </div>
                ))}
              </div>
            )}

            {!isPending && products.length === 0 && (
              <div className="rounded-xl border border-dashed bg-card py-20 text-center shadow-sm">
                <Package className="mx-auto mb-2 size-10 text-muted-foreground/40" />
                <p className="text-sm text-muted-foreground">{t('search.noResults')}</p>
                {selectedCategoryId && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="mt-2"
                    onClick={() => handleCategorySelect(null)}
                  >
                    {t('search.clearFilter')}
                  </Button>
                )}
              </div>
            )}

            {!isPending && products.length > 0 && (
              <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                {products.map((product) => (
                  <StoreProductCard key={product.id} product={product} />
                ))}
              </div>
            )}

            {/* Pagination */}
            {!isPending && totalPages > 1 && (
              <div className={`mt-12 flex items-center justify-center gap-4 ${dir === 'rtl' ? 'flex-row' : ''}`}>
                <Button
                  variant="outline"
                  disabled={page <= 1}
                  onClick={() => updateParam('page', String(page - 1))}
                >
                  {t('pagination.previous')}
                </Button>
                <span className="text-sm">
                  {t('pagination.page')} {formatNumber(page, locale)} {t('pagination.of')} {formatNumber(totalPages, locale)}
                </span>
                <Button
                  variant="outline"
                  disabled={page >= totalPages}
                  onClick={() => updateParam('page', String(page + 1))}
                >
                  {t('pagination.next')}
                </Button>
              </div>
            )}
          </main>
        </div>
      </div>
    </div>
  );
}

export default function ProductsPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-amber-50/30 py-12">
          <div className="max-w-7xl mx-auto px-6">
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {Array.from({ length: 8 }).map((_, i) => (
                <div key={i} className="animate-pulse rounded-xl bg-card p-4 shadow-sm">
                  <div className="mb-2 h-40 rounded-lg bg-muted" />
                  <div className="mb-1 h-4 w-3/4 rounded bg-muted" />
                  <div className="h-5 w-1/3 rounded bg-muted" />
                </div>
              ))}
            </div>
          </div>
        </div>
      }
    >
      <ProductsContent />
    </Suspense>
  );
}
