'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useLocale } from '@/lib/locale-context';
import { formatCurrency } from '@/lib/formatters';
import { authHeaders, isAuthenticated } from '@/lib/cart-api';
import { toast } from '@/hooks/use-toast';
import { Wallet as WalletIcon, ArrowDownLeft, ArrowUpRight, Loader2, Plus } from 'lucide-react';
import { API_GATEWAY_URL } from '@/lib/config';

interface WalletTransaction {
  id: number;
  amount: number;
  balanceBefore: number;
  balanceAfter: number;
  type: string;
  description: string;
  reference?: string;
  orderId?: number;
  status: string;
  createdAt: string;
}

interface WalletData {
  id: number;
  balance: number;
  currency: string;
  createdAt: string;
  updatedAt: string;
  transactions: WalletTransaction[];
}

const INCOME_TYPES = new Set(['DEPOSIT', 'REFUND', 'REVERSAL', 'ADJUSTMENT']);

function typeKey(type: string): string {
  if (type === 'DEPOSIT') return 'wallet.type.charge';
  if (type === 'PAYMENT') return 'wallet.type.payment';
  if (type === 'REFUND') return 'wallet.type.refund';
  return 'wallet.type.payment';
}

export default function WalletPage() {
  const router = useRouter();
  const { t, dir, locale } = useLocale();
  const [wallet, setWallet] = useState<WalletData | null>(null);
  const [error, setError] = useState(false);
  const [disabled, setDisabled] = useState(false);
  const [amount, setAmount] = useState('');
  const [charging, setCharging] = useState(false);

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace('/login');
      return;
    }
    (async () => {
      try {
        const res = await fetch(`${API_GATEWAY_URL}/api/wallet?pageSize=50`, {
          headers: authHeaders(),
        });
        if (res.status === 403) {
          setDisabled(true);
          return;
        }
        if (!res.ok) throw new Error('Failed');
        setWallet(await res.json());
      } catch {
        setError(true);
      }
    })();
  }, [router]);

  if (disabled) {
    return (
      <div className="min-h-screen bg-amber-50/30 py-10">
        <div className="mx-auto max-w-xl px-4">
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-14 text-center">
              <WalletIcon className="mx-auto mb-4 size-12 text-muted-foreground/50" />
              <h1 className="text-xl font-bold">{t('wallet.title')}</h1>
              <p className="mt-3 text-sm text-muted-foreground">{t('wallet.disabled')}</p>
              <div className="mt-6">
                <Button className="rounded-full" onClick={() => router.push('/products')}>
                  {t('wallet.goToStore')}
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  const handleCharge = async () => {
    const value = Number(amount);
    if (!value || value <= 0) {
      toast({ title: t('wallet.amountInvalid'), variant: 'destructive' });
      return;
    }
    setCharging(true);
    try {
      const res = await fetch(`${API_GATEWAY_URL}/api/wallet/charge`, {
        method: 'POST',
        headers: { ...authHeaders(), 'Content-Type': 'application/json' },
        body: JSON.stringify({ amount: value }),
      });
      const data = await res.json();
      if (res.ok && data.success) {
        // Local success (or gateway redirect)
        if (data.redirectUrl) {
          window.location.href = data.redirectUrl;
          return;
        }
        toast({ title: t('wallet.charged') });
        setAmount('');
      } else {
        toast({
          title: t('wallet.chargeFailed'),
          description: data.failureReason ?? '',
          variant: 'destructive',
        });
      }
    } catch {
      toast({ title: t('wallet.chargeFailed'), variant: 'destructive' });
    } finally {
      setCharging(false);
    }
  };

  if (wallet === null && !error) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="w-full max-w-2xl space-y-4 px-4">
          <div className="store-skeleton h-32" />
          <div className="store-skeleton h-24" />
          <div className="store-skeleton h-24" />
        </div>
      </div>
    );
  }

  const balance = wallet?.balance ?? 0;

  return (
    <div className="min-h-screen bg-amber-50/30 py-10">
      <div className="mx-auto max-w-2xl px-4 sm:px-6 lg:px-8">
        <h1 className={`mb-6 flex items-center gap-2 text-2xl font-bold ${dir === 'rtl' ? 'text-right' : ''}`}>
          <WalletIcon className="size-6 text-primary" />
          {t('wallet.title')}
        </h1>

        {error ? (
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-12 text-center text-muted-foreground">
              {t('wallet.loadError')}
              <div className="mt-4">
                <Button variant="outline" size="sm" onClick={() => router.push('/products')}>
                  {t('wallet.goToStore')}
                </Button>
              </div>
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-5">
            {/* Balance card */}
            <Card className="overflow-hidden rounded-2xl border-0 bg-gradient-to-br from-primary to-accent text-primary-foreground shadow-lg">
              <CardContent className="p-6">
                <p className="text-sm opacity-80">{t('wallet.balance')}</p>
                <p className="mt-1 text-4xl font-extrabold tracking-tight">
                  {formatCurrency(balance, locale)}
                </p>
                <p className="mt-6 text-xs uppercase tracking-wider opacity-70">
                  {t('wallet.credit')}
                </p>
              </CardContent>
            </Card>

            {/* Charge */}
            <Card className="rounded-2xl border-0 shadow-md">
              <CardHeader>
                <CardTitle className={`text-base ${dir === 'rtl' ? 'text-right' : ''}`}>
                  {t('wallet.chargeTitle')}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <form
                  onSubmit={(e) => {
                    e.preventDefault();
                    handleCharge();
                  }}
                  className={`flex gap-2 ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}
                >
                  <Input
                    type="number"
                    min="1"
                    placeholder={t('wallet.chargeAmount')}
                    value={amount}
                    onChange={(e) => setAmount(e.target.value)}
                    className="store-input"
                    dir={dir}
                  />
                  <Button type="submit" className="gap-1.5 rounded-full" disabled={charging}>
                    {charging ? (
                      <Loader2 className="size-4 animate-spin" />
                    ) : (
                      <Plus className="size-4" />
                    )}
                    {t('wallet.chargeBtn')}
                  </Button>
                </form>
              </CardContent>
            </Card>

            {/* Transactions */}
            <Card className="rounded-2xl border-0 shadow-md">
              <CardHeader>
                <CardTitle className={`text-base ${dir === 'rtl' ? 'text-right' : ''}`}>
                  {t('wallet.transactions')}
                </CardTitle>
              </CardHeader>
              <CardContent>
                {!wallet || wallet.transactions.length === 0 ? (
                  <div className="py-10 text-center">
                    <ArrowUpRight className="mx-auto mb-3 size-10 text-muted-foreground/40" />
                    <p className="font-medium">{t('wallet.noTransactions')}</p>
                    <p className="mt-1 text-sm text-muted-foreground">
                      {t('wallet.noTransactions.desc')}
                    </p>
                  </div>
                ) : (
                  <ul className="space-y-2">
                    {wallet.transactions.map((tx) => {
                      const income = INCOME_TYPES.has(tx.type);
                      return (
                        <li
                          key={tx.id}
                          className={`flex items-center gap-3 rounded-xl border p-3 transition-colors hover:bg-muted/50 ${
                            dir === 'rtl' ? 'flex-row' : ''
                          }`}
                        >
                          <span
                            className={`flex size-9 shrink-0 items-center justify-center rounded-full ${
                              income
                                ? 'bg-green-100 text-green-700'
                                : 'bg-rose-100 text-rose-600'
                            }`}
                          >
                            {income ? (
                              <ArrowDownLeft className="size-4" />
                            ) : (
                              <ArrowUpRight className="size-4" />
                            )}
                          </span>
                          <div className="min-w-0 flex-1">
                            <p className="truncate text-sm font-medium">
                              {tx.description || t(typeKey(tx.type))}
                            </p>
                            <p className="text-xs text-muted-foreground">
                              {new Intl.DateTimeFormat(locale, {
                                year: 'numeric',
                                month: 'short',
                                day: 'numeric',
                              }).format(new Date(tx.createdAt))}
                            </p>
                          </div>
                          <span
                            className={`text-sm font-bold tabular-nums ${
                              income ? 'text-green-700' : 'text-rose-600'
                            }`}
                          >
                            {income ? '+' : '−'}
                            {formatCurrency(Math.abs(tx.amount), locale)}
                          </span>
                        </li>
                      );
                    })}
                  </ul>
                )}
              </CardContent>
            </Card>
          </div>
        )}
      </div>
    </div>
  );
}