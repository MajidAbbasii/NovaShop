'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { toast } from '@/hooks/use-toast';
import { useLocale } from '@/lib/locale-context';
import { useAuth } from '@/lib/auth-context';
import { Store, Loader2, Eye, EyeOff } from 'lucide-react';
import { API_GATEWAY_URL } from '@/lib/config';

type Profile = {
  firstName?: string;
  lastName?: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
  city?: string;
  postalCode?: string;
};

export default function RegisterPage() {
  const router = useRouter();
  const { t, dir } = useLocale();
  const { signIn } = useAuth();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [profile, setProfile] = useState<Profile>({});
  const [loading, setLoading] = useState(false);

  const set = (k: keyof Profile, v: string) =>
    setProfile((p) => ({ ...p, [k]: v }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await fetch(`${API_GATEWAY_URL}/api/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          username,
          password,
          firstName: profile.firstName || undefined,
          lastName: profile.lastName || undefined,
          email: profile.email || undefined,
          phoneNumber: profile.phoneNumber || undefined,
          address: profile.address || undefined,
          city: profile.city || undefined,
          postalCode: profile.postalCode || undefined,
        }),
      });
      if (!res.ok) {
        const text = await res.text().catch(() => '');
        throw new Error(text || 'Registration failed');
      }
      const data = await res.json();
      toast({ title: t('auth.registerSuccess') });
      if (data.token) {
        signIn(data.token);
        router.push('/products');
      } else {
        router.push('/login');
      }
    } catch (err: unknown) {
      toast({
        title: t('auth.registerFailed'),
        description: err instanceof Error ? err.message : t('error.generic'),
        variant: 'destructive',
      });
    } finally { setLoading(false); }
  };

  return (
    <div className="min-h-[80vh] flex items-center justify-center bg-gradient-to-br from-amber-50 to-orange-50 px-4">
      <Card className="w-full max-w-md shadow-xl border-0 rounded-2xl">
        <CardHeader className="text-center">
          <div className="mx-auto mb-2 flex h-14 w-14 items-center justify-center rounded-full bg-amber-100 shadow-sm">
            <Store className="size-7 text-amber-700" />
          </div>
          <CardTitle>{t('auth.createAccount')}</CardTitle>
          <CardDescription>{t('auth.createAccountDesc')}</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="username">{t('auth.username')}</Label>
              <Input
                id="username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                required
                disabled={loading}
                dir={dir}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">{t('auth.password')}</Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  disabled={loading}
                  dir={dir}
                  className="pe-10"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="absolute end-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  aria-label={showPassword ? t('auth.hidePassword') : t('auth.showPassword')}
                  tabIndex={-1}
                >
                  {showPassword ? <EyeOff className="size-5" /> : <Eye className="size-5" />}
                </button>
              </div>
            </div>

            <div className="my-2 border-t border-dashed pt-3 text-xs font-medium text-muted-foreground">
              {t('auth.optionalInfo')}
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="firstName">{t('auth.firstName')}</Label>
                <Input id="firstName" value={profile.firstName ?? ''} onChange={(e) => set('firstName', e.target.value)} disabled={loading} dir={dir} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">{t('auth.lastName')}</Label>
                <Input id="lastName" value={profile.lastName ?? ''} onChange={(e) => set('lastName', e.target.value)} disabled={loading} dir={dir} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">{t('auth.email')}</Label>
                <Input id="email" type="email" value={profile.email ?? ''} onChange={(e) => set('email', e.target.value)} disabled={loading} dir={dir} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="phone">{t('auth.phone')}</Label>
                <Input id="phone" type="tel" inputMode="numeric" dir="ltr" placeholder="09123456789" value={profile.phoneNumber ?? ''} onChange={(e) => set('phoneNumber', e.target.value.replace(/[^0-9]/g, ''))} disabled={loading} maxLength={11} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="city">{t('auth.city')}</Label>
                <Input id="city" value={profile.city ?? ''} onChange={(e) => set('city', e.target.value)} disabled={loading} dir={dir} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="postalCode">{t('auth.postalCode')}</Label>
                <Input id="postalCode" inputMode="numeric" dir="ltr" value={profile.postalCode ?? ''} onChange={(e) => set('postalCode', e.target.value.replace(/[^0-9]/g, ''))} disabled={loading} maxLength={20} />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="address">{t('auth.address')}</Label>
              <Input id="address" value={profile.address ?? ''} onChange={(e) => set('address', e.target.value)} disabled={loading} dir={dir} />
            </div>

            <Button
              type="submit"
              className="w-full gap-2 rounded-full"
              disabled={loading}
              size="lg"
            >
              {loading && <Loader2 className="size-4 animate-spin" />}
              {t('auth.createBtn')}
            </Button>
          </form>
          <p className="mt-4 text-center text-sm text-muted-foreground">
            {t('auth.hasAccount')}{' '}
            <Link href="/login" className="font-medium text-primary hover:underline">
              {t('auth.signInBtn')}
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
