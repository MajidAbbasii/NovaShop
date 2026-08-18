'use client';

import { API_GATEWAY_URL } from '@/lib/config';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ShoppingCart, Bell, X, Menu, Heart, Search, Store, Camera, LogIn, CircleUser } from 'lucide-react';
import { useCart } from '@/lib/cart-context';
import { useWishlist } from '@/lib/wishlist-context';
import { useLocale } from '@/lib/locale-context';
import { useAuth } from '@/lib/auth-context';
import { LanguageSwitcher } from '@/components/language-switcher';
import { ThemeToggle } from '@/components/theme-toggle';
import { cn } from '@/lib/utils';

// Shared badge classes for icon buttons (notification / wishlist / cart).
const iconBadge =
  'absolute -top-0.5 -end-0.5 flex size-4 items-center justify-center rounded-full bg-primary text-[10px] font-bold leading-none text-primary-foreground';
const iconBtn = (extra = '') =>
  `relative inline-flex size-9 items-center justify-center whitespace-nowrap rounded-full border-transparent bg-transparent px-0 py-0 text-muted-foreground hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 ${extra}`;

// --- Notification count hook ---
function useUnreadNotificationCount() {
  const [count, setCount] = useState<number | null>(null);
  const { user } = useAuth();

  useEffect(() => {
    if (!user) return;

    let cancelled = false;
    fetch(`${API_GATEWAY_URL}/api/notifications/unread-count`, {
      headers: {
        Authorization: `Bearer ${typeof window !== 'undefined' ? localStorage.getItem('token') || document.cookie.match(/(?:^|;\s*)token=([^;]*)/)?.[1] : null}`
      }
    })
      .then((res) => {
        if (!res.ok) throw new Error('network');
        return res.json();
      })
      .then((data) => { if (!cancelled) setCount(data.count); })
      .catch(() => { if (!cancelled) setCount(null); });

    return () => { cancelled = true; };
  }, [user]);

  return user ? count : null;
}

// Extend global for Next.js Env (same as lib/config.ts convention)

export function SiteHeader() {
  const { itemCount, openSheet } = useCart();
  const { wishlistCount } = useWishlist();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const { t, dir } = useLocale();
  const { isAuthenticated } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const unreadCount = useUnreadNotificationCount();

  // Hide the store header inside the admin panel (it has its own chrome).
  if (pathname?.startsWith('/admin')) return null;

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const q = searchQuery.trim();
    router.push(q ? `/products?search=${encodeURIComponent(q)}` : '/products');
  };

  return (
    <>
      {/* Announcement bar */}
      <div className="bg-gradient-to-r from-primary via-primary/90 to-accent text-primary-foreground">
        <p className="mx-auto max-w-7xl px-3 py-1.5 text-center text-xs font-medium sm:px-6 sm:text-sm">
          {t('header.announcement')}
        </p>
      </div>
      <header className="sticky top-0 z-40 w-full border-b border-border/70 bg-card/95 shadow-sm backdrop-blur supports-[backdrop-filter]:bg-card/80">
        {/* Top bar: logo | search | actions */}
        <div className="mx-auto flex h-16 max-w-7xl items-center gap-2 px-3 sm:gap-4 sm:px-6 lg:px-8">
          {/* Mobile menu button */}
          <Button
            variant="ghost"
            size="icon"
            className="shrink-0 lg:hidden"
            onClick={() => setMobileOpen(!mobileOpen)}
            aria-label={mobileOpen ? t('header.closeMenu') : t('header.openMenu')}
          >
            {mobileOpen ? <X className="size-5" /> : <Menu className="size-5" />}
          </Button>

          {/* Logo */}
          <Link href="/" className="flex shrink-0 items-center gap-2">
            <div className="flex size-9 items-center justify-center rounded-xl bg-gradient-to-br from-primary to-accent text-primary-foreground shadow-sm">
              <Store className="size-5" />
            </div>
            <span className="hidden text-base font-extrabold tracking-tight sm:inline">
              {t('site.name')}
            </span>
          </Link>

          {/* Search -- prominent on md+ */}
          <form
            onSubmit={handleSearch}
            className="relative hidden max-w-xl flex-1 md:block"
            role="search"
          >
            <Search className="absolute start-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder={t('shop.searchPlaceholder')}
              aria-label={t('shop.searchPlaceholder')}
              className="store-input h-10 rounded-full pe-12 ps-9"
              dir={dir}
            />
            <Button
              type="submit"
              size="sm"
              className="absolute end-1.5 top-1/2 -translate-y-1/2 rounded-full px-3"
            >
              {t('shop.search')}
            </Button>
          </form>

          {/* Actions */}
          <div className="ms-auto flex shrink-0 items-center gap-0.5 sm:gap-1">
            {/* Notifications */}
            {isAuthenticated && (
              <Button
                variant="ghost"
                size="icon"
                className={cn(iconBtn(), 'relative text-muted-foreground hover:text-foreground')}
                aria-label={t('notifications.title')}
              >
                <Link href="/notifications">
                  <Bell className="size-5" />
                  {unreadCount !== null && unreadCount > 0 && (
                    <span className={iconBadge}>
                      {unreadCount > 99 ? '99+' : unreadCount}
                    </span>
                  )}
                </Link>
              </Button>
            )}

            {/* Theme */}
            <ThemeToggle />

            {/* Language */}
            <LanguageSwitcher />

            {/* Login (logged-out only) */}
            {!isAuthenticated && (
              <Button
                asChild
                variant="ghost"
                size="icon"
                className="text-muted-foreground hover:text-foreground"
                aria-label={t('header.login')}
              >
                <Link href="/login">
                  <LogIn className="size-5" />
                </Link>
              </Button>
            )}

            {/* Wishlist (desktop) */}
            {isAuthenticated && (
              <Button
                variant="ghost"
                size="icon"
                className="relative hidden text-muted-foreground hover:text-foreground md:inline-flex"
                aria-label={`${t('wishlist.title')} ${wishlistCount > 0 ? `(${wishlistCount})` : ''}`}
                asChild
              >
                <Link href="/wishlist">
                  <Heart className="size-5" />
                  {wishlistCount > 0 && (
                    <span className={iconBadge}>
                      {wishlistCount > 99 ? '99+' : wishlistCount}
                    </span>
                  )}
                </Link>
              </Button>
            )}

            {/* Custom doll requests (desktop) */}
            {isAuthenticated && (
              <Button
                variant="ghost"
                size="icon"
                className="hidden text-muted-foreground hover:text-foreground md:inline-flex"
                aria-label={t('customDoll.myRequests')}
                asChild
              >
                <Link href="/custom-doll-requests">
                  <Camera className="size-5" />
                </Link>
              </Button>
            )}

            {/* Cart */}
            <Button
              variant="ghost"
              size="icon"
              className="relative text-muted-foreground hover:text-foreground"
              onClick={openSheet}
              aria-label={`${t('header.cart')} ${itemCount > 0 ? `(${itemCount})` : ''}`}
            >
              <ShoppingCart className="size-5" />
              {itemCount > 0 && (
                <span className={iconBadge}>
                  {itemCount > 99 ? '99+' : itemCount}
                </span>
              )}
            </Button>

            {/* Profile (authenticated users) */}
            {isAuthenticated && (
              <Button
                asChild
                variant="ghost"
                size="icon"
                className="text-muted-foreground hover:text-foreground"
                aria-label={t('header.profile')}
              >
                <Link href="/profile">
                  <CircleUser className="size-5" />
                </Link>
              </Button>
            )}
          </div>
        </div>
      </header>
    </>
  );
}
