'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useCart } from '@/lib/cart-context';
import { useLocale } from '@/lib/locale-context';
import { useAuth } from '@/lib/auth-context';
import { useCategories } from '@/lib/categories-context';
import {
  ShoppingCart,
  Store,
  Menu,
  X,
  Heart,
  User,
  LogOut,
  Search,
  Bell,
  ChevronDown,
  Camera,
} from 'lucide-react';
import { LanguageSwitcher } from '@/components/language-switcher';
import { ThemeToggle } from '@/components/theme-toggle';

export function SiteHeader() {
  const { itemCount, openSheet } = useCart();
  const { t, dir } = useLocale();
  const { user, isAuthenticated, signOut } = useAuth();
  const { categories } = useCategories();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const pathname = usePathname();
  const router = useRouter();

  // Hide the store header inside the admin panel (it has its own chrome).
  if (pathname?.startsWith('/admin')) return null;

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const q = searchQuery.trim();
    router.push(q ? `/products?search=${encodeURIComponent(q)}` : '/products');
  };

  const handleLogout = () => {
    setMobileOpen(false);
    signOut(); // clears cookie + localStorage + user state
    router.push('/login');
  };

  const navLinkCls =
    'rounded-lg px-3 py-2 text-sm font-medium transition-colors hover:bg-muted';

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

          {/* Search — prominent on md+ */}
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
              className="text-muted-foreground hover:text-foreground"
              aria-label={t('notifications.title')}
              asChild
            >
              <Link href="/notifications">
                <Bell className="size-5" />
              </Link>
            </Button>
            )}

            {/* Theme */}
            <ThemeToggle />

            {/* Language */}
            <LanguageSwitcher />

            {/* Wishlist (desktop) */}
            <Button
              variant="ghost"
              size="icon"
              className="hidden text-muted-foreground hover:text-foreground md:inline-flex"
              aria-label={t('wishlist.title')}
              asChild
            >
              <Link href="/wishlist">
                <Heart className="size-5" />
              </Link>
            </Button>

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
                <span className="absolute -top-0.5 -end-0.5 flex size-4 items-center justify-center rounded-full bg-primary text-[10px] font-bold leading-none text-primary-foreground">
                  {itemCount > 99 ? '99+' : itemCount}
                </span>
              )}
            </Button>
          </div>
        </div>

        {/* Mobile search */}
        <form
          onSubmit={handleSearch}
          className="relative border-t border-border/60 px-3 py-2 md:hidden"
          role="search"
        >
          <Search className="absolute start-6 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={t('shop.searchPlaceholder')}
            aria-label={t('shop.searchPlaceholder')}
            className="store-input h-9 rounded-full ps-9 text-sm"
            dir={dir}
          />
        </form>

        {/* Nav bar (desktop) */}
        <nav className="hidden border-t border-border/60 lg:block" aria-label={t('header.categories')}>
          <div className="mx-auto flex h-11 max-w-7xl items-center gap-1 px-4 sm:px-6 lg:px-8">
            {/* Categories dropdown */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="gap-1 font-medium">
                  {t('header.categories')}
                  <ChevronDown className="size-3.5" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-56">
                <DropdownMenuLabel>{t('header.categories')}</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem asChild>
                  <Link href="/products">{t('shop.allCategories')}</Link>
                </DropdownMenuItem>
                {categories.map((cat) => (
                  <DropdownMenuItem key={cat.id} asChild>
                    <Link href={`/products?category=${cat.id}`}>{cat.name}</Link>
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>

            {[
              { href: '/', label: t('header.home') },
              { href: '/products', label: t('header.shop') },
            ].map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={`relative rounded-lg px-3 py-1.5 text-sm font-medium transition-colors hover:bg-muted ${
                  pathname === link.href ? 'bg-muted text-primary' : ''
                }`}
              >
                {link.label}
                {pathname === link.href && (
                  <span className="absolute inset-x-3 -bottom-[13px] h-0.5 rounded-full bg-primary" />
                )}
              </Link>
            ))}

            <div className="ms-auto flex items-center gap-1">
              {isAuthenticated && user ? (
                <>
                  <Button variant="ghost" size="sm" asChild>
                    <Link href="/profile">
                      <User className="size-4" />
                      <span className="max-w-28 truncate">{user.username}</span>
                    </Link>
                  </Button>
                  <Button variant="ghost" size="sm" onClick={handleLogout}>
                    <LogOut className="size-4" />
                    {t('header.logout')}
                  </Button>
                </>
              ) : (
                <>
                  <Button variant="ghost" size="sm" asChild>
                    <Link href="/register">{t('header.register')}</Link>
                  </Button>
                  <Button size="sm" className="rounded-full px-4" asChild>
                    <Link href="/login">{t('header.login')}</Link>
                  </Button>
                </>
              )}
            </div>
          </div>
        </nav>
      </header>

      {/* Mobile nav drawer */}
      {mobileOpen && (
        <div className="fixed inset-x-0 top-16 z-30 border-b border-border bg-card shadow-lg lg:hidden">
          <div className="space-y-1 px-4 py-3">
            {isAuthenticated && user && (
              <div className="mb-2 flex items-center gap-3 rounded-xl bg-muted/60 px-3 py-2.5">
                <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary/15 text-primary">
                  <User className="size-5" />
                </div>
                <div className="min-w-0">
                  <p className="truncate text-sm font-semibold">{user.username}</p>
                  <p className="text-xs text-muted-foreground">{t('header.myAccount')}</p>
                </div>
                <Link
                  href="/profile"
                  className="ms-auto rounded-lg px-2 py-1 text-xs font-medium text-primary hover:bg-primary/10"
                  onClick={() => setMobileOpen(false)}
                >
                  {t('header.myAccount')}
                </Link>
              </div>
            )}
            {[
              { href: '/', label: t('header.home') },
              { href: '/products', label: t('header.shop') },
              { href: '/wishlist', label: t('wishlist.title') },
              { href: '/orders', label: t('header.myOrders') },
              { href: '/custom-doll-request', label: t('customDoll.title') },
              { href: '/custom-doll-requests', label: t('customDoll.myRequests') },
              { href: '/notifications', label: t('notifications.title') },
            ].map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={navLinkCls}
                onClick={() => setMobileOpen(false)}
              >
                {link.label}
              </Link>
            ))}
            <div className="pt-1">
              <p className="px-3 pb-1 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                {t('header.categories')}
              </p>
              {categories.map((cat) => (
                <Link
                  key={cat.id}
                  href={`/products?category=${cat.id}`}
                  className={navLinkCls}
                  onClick={() => setMobileOpen(false)}
                >
                  {cat.name}
                </Link>
              ))}
            </div>
            {isAuthenticated && user ? (
              <button
                type="button"
                onClick={handleLogout}
                className={`${navLinkCls} w-full text-left text-destructive`}
              >
                <LogOut className="me-1 inline size-4" />
                {t('header.logout')}
              </button>
            ) : (
              <>
                <Link href="/login" className={navLinkCls} onClick={() => setMobileOpen(false)}>
                  {t('header.login')}
                </Link>
                <Link href="/register" className={navLinkCls} onClick={() => setMobileOpen(false)}>
                  {t('header.register')}
                </Link>
              </>
            )}
          </div>
        </div>
      )}
    </>
  );
}