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
import { Store, Loader2, MessageSquareText, KeyRound } from 'lucide-react';
import { apiFetch } from '@/lib/admin-api';
import { cn } from '@/lib/utils';

export default function LoginPage() {
  const router = useRouter();
  const { t, dir } = useLocale();
  const { signIn } = useAuth();
  const [mode, setMode] = useState<'password' | 'otp'>('password');

  // password mode
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);

  // otp mode
  const [phone, setPhone] = useState('');
  const [code, setCode] = useState('');
  const [sending, setSending] = useState(false);
  const [codeSent, setCodeSent] = useState(false);
  const [otpLoading, setOtpLoading] = useState(false);

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

  const sendOtp = async () => {
    setSending(true);
    try {
      await apiFetch('/api/auth/otp/request', {
        method: 'POST',
        body: JSON.stringify({ phoneNumber: phone }),
      });
      setCodeSent(true);
      toast({ title: t('auth.otpSent') });
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : '';
      toast({
        title: t('auth.loginFailed'),
        description: message.includes('404')
          ? t('auth.otpUserNotFound')
          : message.includes('429')
            ? t('auth.otpSent')
            : message.includes('Failed to fetch')
              ? t('auth.serverError')
              : t('error.generic'),
        variant: 'destructive',
      });
    } finally { setSending(false); }
  };

  const handleOtpSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setOtpLoading(true);
    try {
      const data = await apiFetch<{ token: string }>('/api/auth/otp/verify', {
        method: 'POST',
        body: JSON.stringify({ phoneNumber: phone, code }),
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
          ? t('auth.otpWrong')
          : message.includes('Failed to fetch')
            ? t('auth.serverError')
            : t('error.generic'),
        variant: 'destructive',
      });
    } finally { setOtpLoading(false); }
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
          {/* Mode tabs */}
          <div className="mb-5 grid grid-cols-2 gap-1 rounded-full bg-muted p-1">
            <button
              type="button"
              onClick={() => setMode('password')}
              className={cn(
                'flex items-center justify-center gap-1.5 rounded-full px-3 py-2 text-xs font-medium transition-all',
                mode === 'password' ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
              )}
            >
              <KeyRound className="size-3.5" />
              {t('auth.loginWithPassword')}
            </button>
            <button
              type="button"
              onClick={() => setMode('otp')}
              className={cn(
                'flex items-center justify-center gap-1.5 rounded-full px-3 py-2 text-xs font-medium transition-all',
                mode === 'otp' ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
              )}
            >
              <MessageSquareText className="size-3.5" />
              {t('auth.loginWithOtp')}
            </button>
          </div>

          {mode === 'password' ? (
            <form onSubmit={handlePasswordSubmit} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="username">{t('auth.username')}</Label>
                <Input
                  id="username"
                  value={username}
                  onChange={e => setUsername(e.target.value)}
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
                  onChange={e => setPassword(e.target.value)}
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
          ) : (
            <form onSubmit={handleOtpSubmit} className="space-y-4">
              <p className="text-xs text-muted-foreground">{t('auth.otpDesc')}</p>
              <div className="space-y-2">
                <Label htmlFor="phone">{t('auth.phone')}</Label>
                <Input
                  id="phone"
                  type="tel"
                  value={phone}
                  onChange={e => setPhone(e.target.value)}
                  required
                  pattern="09\d{9}"
                  placeholder="09123456789"
                  disabled={sending || otpLoading}
                  dir="ltr"
                  className="text-left"
                />
              </div>
              {codeSent && (
                <div className="space-y-2">
                  <Label htmlFor="code">{t('auth.otpCode')}</Label>
                  <Input
                    id="code"
                    inputMode="numeric"
                    maxLength={6}
                    value={code}
                    onChange={e => setCode(e.target.value.replace(/\D/g, ''))}
                    required
                    disabled={sending || otpLoading}
                    dir="ltr"
                    className="text-left tracking-widest"
                  />
                </div>
              )}
              {!codeSent ? (
                <Button
                  type="button"
                  onClick={sendOtp}
                  className="w-full gap-2 rounded-full"
                  disabled={sending || phone.length < 11}
                  size="lg"
                  variant="outline"
                >
                  {sending && <Loader2 className="size-4 animate-spin" />}
                  {t('auth.otpSend')}
                </Button>
              ) : (
                <Button
                  type="submit"
                  className="w-full gap-2 rounded-full"
                  disabled={otpLoading || code.length < 6}
                  size="lg"
                >
                  {otpLoading && <Loader2 className="size-4 animate-spin" />}
                  {t('auth.signInBtn')}
                </Button>
              )}
              {codeSent && (
                <button
                  type="button"
                  onClick={sendOtp}
                  disabled={sending}
                  className="w-full text-center text-xs text-muted-foreground hover:text-primary disabled:opacity-50"
                >
                  {t('auth.otpResend')}
                </button>
              )}
            </form>
          )}

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