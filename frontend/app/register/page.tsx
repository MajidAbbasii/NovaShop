'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { toast } from '@/hooks/use-toast';
import { useLocale } from '@/lib/locale-context';
import { useAuth } from '@/lib/auth-context';
import { Store, Loader2, MessageSquareText } from 'lucide-react';
import { API_GATEWAY_URL } from '@/lib/config';

const RESEND_SECONDS = 60;

export default function RegisterPage() {
  const router = useRouter();
  const { t, dir } = useLocale();
  const { signIn } = useAuth();
  const [step, setStep] = useState<1 | 2>(1);
  const [username, setUsername] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [code, setCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [verifyLoading, setVerifyLoading] = useState(false);
  const [resendLoading, setResendLoading] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(0);

  useEffect(() => {
    if (step !== 2 || secondsLeft <= 0) return;
    const id = setInterval(() => setSecondsLeft(s => s - 1), 1000);
    return () => clearInterval(id);
  }, [step, secondsLeft]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await fetch(`${API_GATEWAY_URL}/api/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, phoneNumber: phone, password }),
      });
      if (!res.ok) {
        const text = await res.text().catch(() => '');
        throw new Error(text || 'Registration failed');
      }
      const data = await res.json();
      if (!data.pending) throw new Error('Unexpected response');
      setStep(2);
      setSecondsLeft(RESEND_SECONDS);
      toast({ title: t('auth.otpSent') });
    } catch (err: unknown) {
      toast({
        title: t('auth.registerFailed'),
        description: err instanceof Error ? err.message : t('error.generic'),
        variant: 'destructive',
      });
    } finally { setLoading(false); }
  };

  const handleResend = useCallback(async () => {
    if (secondsLeft > 0 || resendLoading) return;
    setResendLoading(true);
    try {
      const res = await fetch(`${API_GATEWAY_URL}/api/auth/register/resend`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ phoneNumber: phone }),
      });
      if (!res.ok) {
        const text = await res.text().catch(() => '');
        throw new Error(text || 'Resend failed');
      }
      setSecondsLeft(RESEND_SECONDS);
      toast({ title: t('auth.otpSent') });
    } catch (err: unknown) {
      toast({
        title: t('auth.registerFailed'),
        description: err instanceof Error ? err.message : t('error.generic'),
        variant: 'destructive',
      });
    } finally { setResendLoading(false); }
  }, [phone, secondsLeft, resendLoading, t]);

  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    setVerifyLoading(true);
    try {
      const res = await fetch(`${API_GATEWAY_URL}/api/auth/register/verify`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ phoneNumber: phone, code }),
      });
      if (!res.ok) {
        const text = await res.text().catch(() => '');
        throw new Error(text || 'Verification failed');
      }
      const data = await res.json();
      document.cookie = `token=${data.token};path=/;max-age=28800`;
      signIn(data.token);
      toast({ title: t('auth.registerSuccess') });
      router.push('/products');
    } catch (err: unknown) {
      toast({
        title: t('auth.registerFailed'),
        description: err instanceof Error ? err.message : t('auth.otpWrong'),
        variant: 'destructive',
      });
    } finally { setVerifyLoading(false); }
  };

  return (
    <div className="min-h-[80vh] flex items-center justify-center bg-gradient-to-br from-amber-50 to-orange-50 px-4">
      <Card className="w-full max-w-sm shadow-xl border-0 rounded-2xl">
        <CardHeader className="text-center">
          <div className="mx-auto mb-2 flex h-14 w-14 items-center justify-center rounded-full bg-amber-100 shadow-sm">
            <Store className="size-7 text-amber-700" />
          </div>
          <CardTitle>{t('auth.createAccount')}</CardTitle>
          <CardDescription>
            {step === 1 ? t('auth.createAccountDesc') : t('auth.otpDesc')}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {step === 1 ? (
            <form onSubmit={handleSubmit} className="space-y-4">
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
                <Label htmlFor="phone">{t('auth.phone')}</Label>
                <Input
                  id="phone"
                  type="tel"
                  inputMode="numeric"
                  dir="ltr"
                  placeholder="09123456789"
                  value={phone}
                  onChange={e => setPhone(e.target.value.replace(/[^\d]/g, ''))}
                  required
                  disabled={loading}
                  maxLength={11}
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
                {t('auth.createBtn')}
              </Button>
            </form>
          ) : (
            <form onSubmit={handleVerify} className="space-y-4">
              <div className="flex items-center justify-center gap-2 text-sm text-muted-foreground">
                <MessageSquareText className="size-4" />
                <span dir="ltr">{phone}</span>
              </div>
              <div className="space-y-2">
                <Label htmlFor="code">{t('auth.otpCode')}</Label>
                <Input
                  id="code"
                  inputMode="numeric"
                  maxLength={6}
                  value={code}
                  onChange={e => setCode(e.target.value.replace(/\D/g, ''))}
                  required
                  disabled={verifyLoading}
                  dir="ltr"
                  className="text-left tracking-widest"
                />
              </div>
              <Button
                type="submit"
                className="w-full gap-2 rounded-full"
                disabled={verifyLoading || code.length < 6}
                size="lg"
              >
                {verifyLoading && <Loader2 className="size-4 animate-spin" />}
                {t('auth.createBtn')}
              </Button>
              <button
                type="button"
                onClick={handleResend}
                disabled={verifyLoading || resendLoading || secondsLeft > 0}
                className="w-full text-center text-xs text-muted-foreground hover:text-primary disabled:opacity-50 disabled:hover:text-muted-foreground"
              >
                {resendLoading && <Loader2 className="mx-auto size-3.5 animate-spin" />}
                {!resendLoading && secondsLeft > 0
                  ? `${t('auth.otpResend')} (${secondsLeft})`
                  : t('auth.otpResend')}
              </button>
            </form>
          )}
          <p className="text-center text-sm text-muted-foreground">
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
