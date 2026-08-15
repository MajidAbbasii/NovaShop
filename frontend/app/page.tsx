'use client';

import { useState, useEffect, useCallback } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { useLocale } from '@/lib/locale-context';
import { resolveImageUrl, API_GATEWAY_URL } from '@/lib/config';
import { ArrowLeft, ChevronLeft, ChevronRight, Sparkles, Heart, ShieldCheck, Truck, Star } from 'lucide-react';
import { StoreProductCard } from '@/components/store-product-card';

interface Product {
  id: number; name: string; price: number; imageUrl: string; rating: number; categoryId?: number;
}
interface Category {
  id: number; name: string; imageUrl: string; description?: string;
}
interface Banner {
  id: number; title: string; subtitle: string; imageUrl: string; linkUrl: string;
}

export default function HomePage() {
  const { t, dir, locale } = useLocale();
      const [featured, setFeatured] = useState<Product[]>([]);
      const [categories, setCategories] = useState<Category[]>([]);
      const [banners, setBanners] = useState<Banner[]>([]);
      const [bannerIndex, setBannerIndex] = useState(0);
      const [loading, setLoading] = useState(true);

      useEffect(() => {
      async function load() {
        try {
          const [prodRes, catRes, banRes] = await Promise.all([
            fetch(`${API_GATEWAY_URL}/api/products?pageSize=8&onlyAvailable=true`),
            fetch(`${API_GATEWAY_URL}/api/categories`),
            fetch(`${API_GATEWAY_URL}/api/banners`),
          ]);
          if (prodRes.ok) {
            const data = await prodRes.json();
            setFeatured(data.items ?? []);
          }
          if (catRes.ok) {
            const data = await catRes.json();
            setCategories(data.items ?? data ?? []);
          }
          if (banRes.ok) {
            const data = await banRes.json();
            setBanners(data.items ?? []);
          }
        } catch (e) { console.error('Home load error', e); }
        finally { setLoading(false); }
      }
      load();
    }, []);

    const nextBanner = useCallback(() => {
      setBannerIndex((i) => (banners.length > 1 ? (i + 1) % banners.length : 0));
    }, [banners.length]);

    useEffect(() => {
      if (banners.length <= 1) return;
      const timer = setInterval(nextBanner, 5000);
      return () => clearInterval(timer);
    }, [banners.length, nextBanner]);

  const ArrowIcon = dir === 'rtl' ? ChevronLeft : ArrowLeft;

  return (
      <div>
        {/* Banner Slider (admin-managed) */}
        {banners.length > 0 && (
          <section className="relative overflow-hidden bg-slate-900">
            <div className="relative mx-auto max-w-7xl px-6 lg:px-8">
              <div className="relative h-56 overflow-hidden rounded-b-3xl md:h-72">
                {banners.map((b, i) => {
                  const active = i === bannerIndex;
                  return (
                    <div
                      key={b.id}
                      className={`absolute inset-0 transition-opacity duration-700 ${active ? 'opacity-100' : 'opacity-0 pointer-events-none'}`}
                    >
                      <Link href={b.linkUrl || '/products'} className="block h-full w-full">
                        {/* eslint-disable-next-line @next/next/no-img-element */}
                        <img
                          src={resolveImageUrl(b.imageUrl) || `https://picsum.photos/seed/banner${b.id}/1600/500`}
                          alt={b.title}
                          className="h-full w-full object-cover"
                          loading={active ? 'eager' : 'lazy'}
                        />
                        <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/20 to-transparent" />
                        <div className={`absolute inset-x-0 bottom-0 px-6 pb-6 md:px-10 md:pb-8 ${dir === 'rtl' ? 'text-right' : 'text-left'}`}>
                          <h2 className="text-2xl font-bold text-white drop-shadow md:text-4xl">{b.title}</h2>
                          {b.subtitle && (
                            <p className="mt-1 text-sm text-white/90 drop-shadow md:text-base">{b.subtitle}</p>
                          )}
                        </div>
                      </Link>
                    </div>
                  )
                })}

                {/* Dots */}
                {banners.length > 1 && (
                  <div className="absolute bottom-3 left-1/2 flex -translate-x-1/2 items-center gap-1.5">
                    {banners.map((b, i) => (
                      <button
                        key={b.id}
                        type="button"
                        aria-label={`${b.title} - slide ${i + 1}`}
                        onClick={() => setBannerIndex(i)}
                        className={`h-2 rounded-full transition-all ${i === bannerIndex ? 'w-6 bg-white' : 'w-2 bg-white/50 hover:bg-white/80'}`}
                      />
                    ))}
                  </div>
                )}

                {/* Arrows */}
                {banners.length > 1 && (
                  <>
                    <button
                      type="button"
                      aria-label="previous"
                      onClick={() => setBannerIndex((i) => (i - 1 + banners.length) % banners.length)}
                      className="absolute left-3 top-1/2 -translate-y-1/2 rounded-full bg-white/20 p-2 text-white backdrop-blur transition hover:bg-white/40"
                    >
                      <ChevronLeft className="size-5" />
                    </button>
                    <button
                      type="button"
                      aria-label="next"
                      onClick={nextBanner}
                      className="absolute right-3 top-1/2 -translate-y-1/2 rounded-full bg-white/20 p-2 text-white backdrop-blur transition hover:bg-white/40"
                    >
                      <ChevronRight className="size-5" />
                    </button>
                  </>
                )}
              </div>
            </div>
          </section>
        )}

        {/* Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-br from-amber-50 via-orange-50 to-rose-50 py-24 md:py-32">
        <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNjAiIGhlaWdodD0iNjAiIHZpZXdCb3g9IjAgMCA2MCA2MCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48ZyBmaWxsPSJub25lIiBmaWxsLXJ1bGU9ImV2ZW5vZGQiPjxnIGZpbGw9IiNmZmYiIGZpbGwtb3BhY2l0eT0iMC40Ij48Y2lyY2xlIGN4PSIzMCIgY3k9IjMwIiByPSIxIi8+PC9nPjwvZz48L3N2Zz4=')] opacity-50" />
        <div className="relative mx-auto max-w-7xl px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center">
            <div className="mb-6 inline-flex items-center gap-2 rounded-full border bg-white/80 px-4 py-1.5 text-sm shadow-sm">
                          <Sparkles className="size-4 text-amber-500" />
                          <span>{t('hero.badge')}</span>
                        </div>
                        <h1 className="mt-4 text-4xl font-bold leading-tight md:text-5xl whitespace-pre-line">
                          {t('hero.title')}
                        </h1>
                        <p className="mt-3 text-base font-medium text-amber-700">{t('hero.subtitle')}</p>
                        <p className="mt-6 text-lg leading-8 text-muted-foreground">
                          {t('hero.description')}
                        </p>
            <div className={`mt-10 flex items-center justify-center gap-4 ${dir === 'rtl' ? 'flex-row' : ''}`}>
              <Button size="lg" className="gap-2 rounded-full shadow-md" asChild>
                <Link href="/products">
                  {t('hero.cta')}
                  <ArrowIcon className="size-4" />
                </Link>
              </Button>
              <Button size="lg" variant="outline" className="rounded-full" asChild>
                <Link href="#categories">{t('hero.explore')}</Link>
              </Button>
            </div>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="border-b bg-card py-12">
        <div className="mx-auto max-w-7xl px-6 lg:px-8">
          <div className={`grid grid-cols-1 gap-8 sm:grid-cols-3 ${dir === 'rtl' ? 'text-right' : ''}`}>
            {[
              { icon: Heart, titleKey: 'feature.handmade', descKey: 'feature.handmade.desc' },
              { icon: ShieldCheck, titleKey: 'feature.premium', descKey: 'feature.premium.desc' },
              { icon: Truck, titleKey: 'feature.shipping', descKey: 'feature.shipping.desc' },
            ].map((feat) => (
              <div key={feat.titleKey} className={`flex items-start gap-4 ${dir === 'rtl' ? 'flex-row' : ''}`}>
                <div className="shrink-0 rounded-full bg-amber-100 p-3">
                  <feat.icon className="size-5 text-amber-700" />
                </div>
                <div>
                  <h3 className="font-semibold">{t(feat.titleKey)}</h3>
                  <p className="mt-1 text-sm text-muted-foreground">{t(feat.descKey)}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Featured Products */}
      <section className="py-16">
        <div className="mx-auto max-w-7xl px-6 lg:px-8">
          <div className={`flex items-center ${dir === 'rtl' ? 'flex-row-reverse justify-between' : 'justify-between'} mb-8`}>
            <h2 className="text-2xl font-bold">{t('featured.title')}</h2>
            <Button variant="ghost" className="gap-1" asChild>
              <Link href="/products">
                {t('featured.viewAll')}
                <ArrowIcon className="size-4" />
              </Link>
            </Button>
          </div>
          {loading ? (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
              {Array.from({length:4}).map((_,i) => (
                <div key={i} className="store-skeleton h-72" />
              ))}
            </div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
              {featured.map((p) => (
                <StoreProductCard key={p.id} product={p} />
              ))}
            </div>
          )}
        </div>
      </section>

      {/* Categories */}
      <section id="categories" className="bg-amber-50/50 py-16">
        <div className="mx-auto max-w-7xl px-6 lg:px-8">
          <h2 className="text-2xl font-bold text-center mb-10">{t('categories.title')}</h2>
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            {(categories.length > 0 ? categories : []).map((cat) => (
              <Link key={cat.id} href={`/products?category=${cat.id}`}>
                <div className="group cursor-pointer rounded-2xl bg-card p-4 text-center shadow-sm transition-all hover:shadow-md">
                  <div className="mx-auto mb-3 h-24 w-24 overflow-hidden rounded-full bg-amber-50">
                    <img src={resolveImageUrl(cat.imageUrl) || 'https://picsum.photos/seed/cat/400/400'} alt={cat.name} className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-110" loading="lazy" />
                  </div>
                  <p className="text-sm font-medium">{cat.name}</p>
                </div>
              </Link>
            ))}
            {categories.length === 0 && !loading && (
              <div className="col-span-full text-center py-8">
                <p className="text-muted-foreground">{t('categories.loading')}</p>
              </div>
            )}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-20 text-center">
        <div className="mx-auto max-w-2xl px-6">
          <h2 className="text-3xl font-bold">{t('cta.title')}</h2>
          <p className="mt-4 text-muted-foreground">{t('cta.desc')}</p>
          <Button size="lg" className="mt-8 gap-2 rounded-full shadow-md" asChild>
            <Link href="/products">
              {t('cta.button')}
              <ArrowIcon className="size-4" />
            </Link>
          </Button>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t bg-amber-50/30 py-8">
        <div className="mx-auto max-w-7xl px-6 text-center text-sm text-muted-foreground">
          <p className={dir === 'rtl' ? 'text-right' : ''}>
                      {t('footer.copyright')}
                    </p>
        </div>
      </footer>
    </div>
  );
}
