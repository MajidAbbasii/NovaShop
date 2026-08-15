'use client';

import { useEffect, useSyncExternalStore } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { useLocale } from '@/lib/locale-context';
import { useAuth } from '@/lib/auth-context';
import { isAuthenticated } from '@/lib/cart-api';
import { UserRound, Package, Heart, LogOut, ChevronLeft, Camera } from 'lucide-react';

export default function ProfilePage() {
  const router = useRouter();
  const { t, dir } = useLocale();
  const { user, signOut } = useAuth();
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false
  );

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace('/login');
    }
  }, [router]);

  const handleLogout = () => {
    signOut();
    router.push(`/login${dir === 'rtl' ? '?lang=fa' : ''}`);
  };

  if (!mounted) return null;

  const links = [
    { href: '/orders', icon: Package, titleKey: 'profile.orders', descKey: 'profile.orders.desc' },
    { href: '/wishlist', icon: Heart, titleKey: 'profile.wishlist', descKey: 'profile.wishlist.desc' },
    { href: '/custom-doll-requests', icon: Camera, titleKey: 'customDoll.myRequests', descKey: 'customDoll.profileDesc' },
  ];

  return (
    <div className="min-h-screen bg-amber-50/30 py-10">
      <div className="mx-auto max-w-lg px-4 sm:px-6 lg:px-8">
        <h1 className={`mb-6 text-2xl font-bold ${dir === 'rtl' ? 'text-right' : ''}`}>
          {t('profile.title')}
        </h1>

        <Card className="mb-4 rounded-2xl border-0 shadow-md">
          <CardContent className={`flex items-center gap-4 p-5 ${dir === 'rtl' ? 'flex-row-reverse text-right' : ''}`}>
            <div className="flex size-14 shrink-0 items-center justify-center rounded-full bg-amber-100">
              <UserRound className="size-7 text-amber-600" />
            </div>
            <div className="min-w-0">
              <p className="text-base font-semibold">
                {t('profile.welcome')}
                {user?.username ? `، ${user.username}` : ''}
              </p>
              <p className="mt-1 text-sm text-muted-foreground">{t('profile.subtitle')}</p>
            </div>
          </CardContent>
        </Card>

        <div className="space-y-3">
          {links.map(({ href, icon: Icon, titleKey, descKey }) => (
            <Link key={href} href={href} className="block">
              <Card className="transition-shadow hover:shadow-lg rounded-2xl border-0 shadow-sm">
                <CardContent className={`flex items-center justify-between gap-3 p-4 ${dir === 'rtl' ? 'text-right' : ''}`}>
                  <div className="flex items-center gap-3">
                    <div className="flex size-11 shrink-0 items-center justify-center rounded-full bg-muted">
                      <Icon className="size-5 text-amber-600" />
                    </div>
                    <div>
                      <p className="text-sm font-medium">{t(titleKey)}</p>
                      <p className="mt-0.5 text-xs text-muted-foreground">{t(descKey)}</p>
                    </div>
                  </div>
                  <ChevronLeft className={`size-4 text-muted-foreground ${dir === 'rtl' ? 'rotate-180' : ''}`} />
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>

        <Button
          variant="outline"
          className={`mt-4 w-full gap-2 rounded-full border-destructive/30 text-destructive hover:bg-destructive/5 hover:text-destructive ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}
          onClick={handleLogout}
        >
          <LogOut className="size-4" />
          {t('profile.logout')}
        </Button>
      </div>
    </div>
  );
}