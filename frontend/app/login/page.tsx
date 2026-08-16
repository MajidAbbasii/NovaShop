'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { toast } from '@/hooks/use-toast';
import { useLocale } from '@/lib/locale-context';
import { useAuth } from '@/lib/auth-context';
import { Store, Loader2, KeyRound } from 'lucide-react';
import { apiFetch } from '@/lib/admin-api';

export default function LoginPage() {
  const router = useRouter();
  const { t, dir } = useLocale();
  const { signIn } = useAuth();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const handlePasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const data = await apiFetch<{ token: string }>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ username, password }),
      });
      signIn(data.token);
      toast({ title: t('auth.loginSuccess') });
      router.push('/products');
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : '';
      const isAuthFailure = message.includes('401') || message.includes('403');
      toast({
        title: t('auth.loginFailed'),
        description: isAuthFailure
          ? t('auth.unauthorized')
          : message.includes('Failed to fetch')
            ? t('auth.serverError')
            : t('error.generic'),
        variant: 'destructive',
      });
    } finally { setLoading(false); }
  };

  return (
    <div className="min-h-[80vh] flex items-center justify-center bg-gradient-to-br from-amber-50 to-orange-50 px-4">
      <Card className="w-full max-w-sm shadow-xl border-0 rounded-2xl">
        <CardHeader className="text-center">
          <div className="mx-auto mb-2 flex h-14 w-14 items-center justify-center rounded-full bg-amber-100 shadow-sm">
            <Store className="size-7 text-amber-700" />
          </div>
          <CardTitle>{t('auth.welcomeBack')}</CardTitle>
          <CardDescription>{t('auth.signInDesc')}</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handlePasswordSubmit} className="space-y-4">
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
              <Input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                disabled={loading}
                dir={dir}
              />
            </div>
            <Button
              type="submit"
              className="w-full gap-2 rounded-full"
              disabled={loading}
              size="lg"
            >
              {loading && <Loader2 className="size-4 animate-spin" />}
              {t('auth.signInBtn')}
            </Button>
          </form>

          <p className="mt-4 text-center text-sm text-muted-foreground">
            {t('auth.noAccount')}{' '}
            <Link href="/register" className="font-medium text-primary hover:underline">
              {t('auth.register')}
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
