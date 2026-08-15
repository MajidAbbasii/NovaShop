'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Store, Heart, ShieldCheck, Headset, Mail, Phone } from 'lucide-react';
import { useLocale } from '@/lib/locale-context';
import { useCategories } from '@/lib/categories-context';

export function StoreFooter() {
  const { t, dir } = useLocale();
  const pathname = usePathname();
  const { categories } = useCategories();

  // Admin panel has its own chrome.
  if (pathname?.startsWith('/admin')) return null;

  const accountLinks = [
    { href: '/profile', label: t('footer.profile') },
    { href: '/orders', label: t('footer.orders') },
    { href: '/wishlist', label: t('wishlist.title') },
    { href: '/notifications', label: t('notifications.title') },
  ] as const; // wallet link intentionally hidden while WalletEnabled=false

  const supportLinks = [
    { href: '/products', label: t('footer.shippingInfo') },
    { href: '/products', label: t('footer.returns') },
    { href: '/products', label: t('footer.privacy') },
  ];

  return (
    <footer className="border-t border-border/70 bg-card">
      <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        {/* Trust badges */}
        <div className="mb-10 grid grid-cols-1 gap-4 sm:grid-cols-3">
          {[
            { icon: Heart, title: t('footer.handmadeBadge'), desc: t('footer.handmadeBadge.desc') },
            { icon: ShieldCheck, title: t('footer.secureBadge'), desc: t('footer.secureBadge.desc') },
            { icon: Headset, title: t('footer.supportBadge'), desc: t('footer.supportBadge.desc') },
          ].map((b) => (
            <div
              key={b.title}
              className="flex items-center gap-3 rounded-2xl border border-border/70 bg-background/60 p-4"
            >
              <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary/10">
                <b.icon className="size-5 text-primary" />
              </div>
              <div>
                <p className="text-sm font-semibold">{b.title}</p>
                <p className="text-xs text-muted-foreground">{b.desc}</p>
              </div>
            </div>
          ))}
        </div>

        <div className="grid grid-cols-2 gap-8 md:grid-cols-4">
          {/* Brand */}
          <div className={dir === 'rtl' ? 'text-right' : ''}>
            <Link href="/" className="flex items-center gap-2">
              <div className="flex size-8 items-center justify-center rounded-lg bg-gradient-to-br from-primary to-accent text-primary-foreground">
                <Store className="size-4" />
              </div>
              <span className="font-extrabold">{t('site.name')}</span>
            </Link>
            <p className="mt-3 text-sm leading-6 text-muted-foreground">
              {t('footer.tagline')}
            </p>
            <div className="mt-4 space-y-2 text-sm text-muted-foreground">
              <p className="flex items-center gap-2">
                <Phone className="size-3.5 text-primary" />
                <span dir="ltr">021-12345678</span>
              </p>
              <p className="flex items-center gap-2">
                <Mail className="size-3.5 text-primary" />
                <span dir="ltr">hello@novashop.ir</span>
              </p>
            </div>
          </div>

          {/* Shop */}
          <div>
            <h3 className="mb-3 text-sm font-bold">{t('footer.shop')}</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <Link href="/products" className="text-muted-foreground transition-colors hover:text-primary">
                  {t('shop.title')}
                </Link>
              </li>
              <li>
                <Link href="/products" className="text-muted-foreground transition-colors hover:text-primary">
                  {t('home.popular')}
                </Link>
              </li>
              <li>
                <Link href="/products" className="text-muted-foreground transition-colors hover:text-primary">
                  {t('home.offers')}
                </Link>
              </li>
            </ul>
          </div>

          {/* Categories */}
          <div>
            <h3 className="mb-3 text-sm font-semibold">{t('footer.categories')}</h3>
            <ul className="space-y-2 text-sm">
              {(categories.length > 0 ? categories.slice(0, 6) : [null, null, null, null]).map(
                (cat, i) =>
                  cat ? (
                    <li key={cat.id}>
                      <Link
                        href={`/products?category=${cat.id}`}
                        className="text-muted-foreground transition-colors hover:text-primary"
                      >
                        {cat.name}
                      </Link>
                    </li>
                  ) : (
                    <li key={i} className="store-skeleton h-4 w-24" />
                  )
              )}
            </ul>
          </div>

          {/* Links */}
          <div>
            <h3 className="mb-3 text-sm font-semibold">{t('footer.account')}</h3>
            <ul className="space-y-2 text-sm">
              {accountLinks.map((l) => (
                <li key={l.href}>
                  <Link href={l.href} className="text-muted-foreground transition-colors hover:text-primary">
                    {l.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>

      <div className="border-t border-border/70 py-5">
        <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-2 px-4 text-xs text-muted-foreground sm:flex-row sm:px-6 lg:px-8">
          <p>{t('footer.copyright')}</p>
          <p>
            {t('site.tagline')} — {t('footer.rights')}
          </p>
        </div>
      </div>
    </footer>
  );
}