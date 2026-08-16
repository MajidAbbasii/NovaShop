'use client';

import { useEffect, useState, useSyncExternalStore } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useLocale } from '@/lib/locale-context';
import { useAuth } from '@/lib/auth-context';
import { isAuthenticated } from '@/lib/cart-api';
import { getCurrentUser, updateProfile, type UserDto } from '@/lib/admin-api';
import { UserRound, Package, Heart, LogOut, ChevronLeft, Camera, Loader2 } from 'lucide-react';
import { toast } from '@/hooks/use-toast';

export default function ProfilePage() {
  const router = useRouter();
  const { t, dir } = useLocale();
  const { user, signOut } = useAuth();
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false
  );

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    address: '',
    city: '',
    postalCode: '',
  });
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace('/login');
    }
  }, [router]);

  useEffect(() => {
    if (!mounted) return;
    getCurrentUser()
      .then((u: UserDto) => {
        setForm({
          firstName: u.firstName ?? '',
          lastName: u.lastName ?? '',
          email: u.email ?? '',
          phoneNumber: (u.phoneNumber ?? '').startsWith('unset_') ? '' : (u.phoneNumber ?? ''),
          address: u.address ?? '',
          city: u.city ?? '',
          postalCode: u.postalCode ?? '',
        });
      })
      .catch(() => {});
  }, [mounted]);

  const handleLogout = () => {
    signOut();
    router.push(`/login${dir === 'rtl' ? '?lang=fa' : ''}`);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await updateProfile({
        firstName: form.firstName || undefined,
        lastName: form.lastName || undefined,
        email: form.email || undefined,
        phoneNumber: form.phoneNumber || undefined,
        address: form.address || undefined,
        city: form.city || undefined,
        postalCode: form.postalCode || undefined,
      });
      toast({ title: t('profile.saved') });
    } catch (err: unknown) {
      toast({
        title: t('profile.saveError'),
        description: err instanceof Error ? err.message : t('error.generic'),
        variant: 'destructive',
      });
    } finally { setSaving(false); }
  };

  if (!mounted) return null;

  const links = [
    { href: '/orders', icon: Package, titleKey: 'profile.orders', descKey: 'profile.orders.desc' },
    { href: '/wishlist', icon: Heart, titleKey: 'profile.wishlist', descKey: 'profile.wishlist.desc' },
    { href: '/custom-doll-requests', icon: Camera, titleKey: 'customDoll.myRequests', descKey: 'customDoll.profileDesc' },
  ];

  const set = (k: keyof typeof form, v: string) => setForm((f) => ({ ...f, [k]: v }));

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

        <Card className="mb-4 rounded-2xl border-0 shadow-sm">
          <CardHeader>
            <CardTitle className={`text-base ${dir === 'rtl' ? 'text-right' : ''}`}>{t('profile.editInfo')}</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSave} className="space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="firstName">{t('auth.firstName')}</Label>
                  <Input id="firstName" value={form.firstName} onChange={(e) => set('firstName', e.target.value)} disabled={saving} dir={dir} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="lastName">{t('auth.lastName')}</Label>
                  <Input id="lastName" value={form.lastName} onChange={(e) => set('lastName', e.target.value)} disabled={saving} dir={dir} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="email">{t('auth.email')}</Label>
                  <Input id="email" type="email" value={form.email} onChange={(e) => set('email', e.target.value)} disabled={saving} dir={dir} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="phoneNumber">{t('auth.phone')}</Label>
                  <Input id="phoneNumber" type="tel" inputMode="numeric" dir="ltr" placeholder="09123456789" value={form.phoneNumber} onChange={(e) => set('phoneNumber', e.target.value.replace(/[^0-9]/g, ''))} disabled={saving} maxLength={11} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="city">{t('auth.city')}</Label>
                  <Input id="city" value={form.city} onChange={(e) => set('city', e.target.value)} disabled={saving} dir={dir} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="postalCode">{t('auth.postalCode')}</Label>
                  <Input id="postalCode" inputMode="numeric" dir="ltr" value={form.postalCode} onChange={(e) => set('postalCode', e.target.value.replace(/[^0-9]/g, ''))} disabled={saving} maxLength={20} />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="address">{t('auth.address')}</Label>
                <Input id="address" value={form.address} onChange={(e) => set('address', e.target.value)} disabled={saving} dir={dir} />
              </div>
              <Button type="submit" className="w-full gap-2 rounded-full" disabled={saving} size="lg">
                {saving && <Loader2 className="size-4 animate-spin" />}
                {t('common.saveChanges')}
              </Button>
            </form>
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
