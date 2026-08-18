'use client';

import { useState, useEffect, useSyncExternalStore } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { toast } from '@/hooks/use-toast';
import { useCart } from '@/lib/cart-context';
import { computeSubtotal, isAuthenticated, getOrderQuote } from '@/lib/cart-api';
import { useLocale } from '@/lib/locale-context';
import { formatCurrency } from '@/lib/formatters';
import { ShoppingBag, Loader2, ArrowLeft, CreditCard, Store, Truck, Mail, Banknote, Tag } from 'lucide-react';
import { API_GATEWAY_URL, resolveImageUrl } from '@/lib/config';
import { getShippingMethods, type ShippingMethods } from '@/lib/shipping-api';

interface CustomerInfo {
  fullName: string;
  email: string;
  phone: string;
  address: string;
  city: string;
  postalCode: string;
}

export default function CheckoutPage() {
  const router = useRouter();
  const { t, dir, locale } = useLocale();
  const { cart, isLoading, clearAll } = useCart();
  const [submitting, setSubmitting] = useState(false);
  const [shippingMethod, setShippingMethod] = useState<'POST' | 'COURIER' | 'PICKUP'>('POST');
  const [discountCode, setDiscountCode] = useState('');
  const [discountError, setDiscountError] = useState('');
  const [applyingDiscount, setApplyingDiscount] = useState(false);
  const [appliedDiscount, setAppliedDiscount] = useState<{ code: string; amount: number; type: string; value: number } | null>(null);
  const [info, setInfo] = useState<CustomerInfo>({
    fullName: '', email: '', phone: '', address: '', city: '', postalCode: '',
  });

  // Admin-managed shipping rates (source of truth = backend). Used only to
  // label the method cards; the order total still comes from the server quote.
  const [methods, setMethods] = useState<ShippingMethods | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const m = await getShippingMethods();
        if (!cancelled) setMethods(m);
      } catch {
        /* non-fatal: fall back to server quote in summary */
      }
    })();
    return () => { cancelled = true; };
  }, []);

  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false
  );

  useEffect(() => {
    if (!isAuthenticated()) {
      router.push('/login');
    }
  }, [router]);

  if (!mounted || isLoading || !cart) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-amber-50/30">
        <Loader2 className="size-8 animate-spin text-amber-600" />
        <p className="mt-2 text-sm text-muted-foreground">{t('loading.order')}</p>
      </div>
    );
  }

  const items = cart.items ?? [];
  const subtotal = computeSubtotal(items);
  // Shipping + total are AUTHORITATIVE from the backend. The client sends only
  // the selected method + discount code and displays the server-computed quote.
  const [quote, setQuote] = useState<{ shippingCost: number; grandTotal: number; discountAmount: number } | null>(null);
  const [quoteLoading, setQuoteLoading] = useState(false);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      setQuoteLoading(true);
      try {
        const q = await getOrderQuote(shippingMethod, appliedDiscount?.code ?? null);
        if (!cancelled) setQuote(q);
      } catch {
        if (!cancelled) setQuote(null);
      } finally {
        if (!cancelled) setQuoteLoading(false);
      }
    };
    void load();
    return () => { cancelled = true; };
  }, [shippingMethod, appliedDiscount]);

  const shipping = quote?.shippingCost ?? 0;
  const previewDiscountAmount = quote?.discountAmount ?? 0;
  const total = quote?.grandTotal ?? subtotal;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (items.length === 0) {
      toast({ title: t('error.cartEmpty'), variant: 'destructive' });
      return;
    }
    setSubmitting(true);
    try {
      const shippingAddr = `${info.address}, ${info.city}, ${info.postalCode}`;
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      const token = typeof document !== 'undefined'
        ? document.cookie.match(/(?:^|; )token=([^;]*)/)?.[1]
        : null;
      if (token) headers['Authorization'] = `Bearer ${token}`;

      const res = await fetch(`${API_GATEWAY_URL}/api/orders`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
          shippingAddress: shippingMethod === 'PICKUP' ? `${t('checkout.method.pickup')} — ${info.phone}` : shippingAddr,
          paymentMethod: 'CashOnDelivery',
          shippingMethod,
          phoneNumber: info.phone,
          discountCode: appliedDiscount?.code ?? null,
        }),
      });

      if (!res.ok) {
        const text = await res.text().catch(() => '');
        throw new Error(text || 'Failed to create order');
      }

      const order = await res.json();

      // Clear client cart cache so the header badge updates immediately
      // (backend already deletes the cart on order creation).
      clearAll();

      router.push(`/orders/${order.id}?name=${encodeURIComponent(info.fullName)}`);
    } catch (e: unknown) {
      toast({
        title: t('error.checkout'),
        description: e instanceof Error ? e.message : t('error.generic'),
        variant: 'destructive',
      });
    } finally { setSubmitting(false); }
  };

  const updateField = (field: keyof CustomerInfo) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setInfo(prev => ({ ...prev, [field]: e.target.value }));

  const handleApplyDiscount = async () => {
    const code = discountCode.trim();
    if (!code) return;
    setApplyingDiscount(true);
    setDiscountError('');
    try {
      const res = await fetch(`${API_GATEWAY_URL}/api/discounts/validate?code=${encodeURIComponent(code)}`);
      if (res.ok) {
        const d = await res.json();
        setAppliedDiscount({ code: d.code, amount: d.discountAmount, type: d.type, value: d.value });
        setDiscountCode('');
        toast({ title: t('checkout.discount.applied') });
      } else {
        const err = await res.json().catch(() => null);
        setDiscountError(err?.error ?? t('checkout.discount.invalid'));
      }
    } catch {
      setDiscountError(t('checkout.discount.invalid'));
    } finally {
      setApplyingDiscount(false);
    }
  };

  const handleRemoveDiscount = () => {
    setAppliedDiscount(null);
    setDiscountError('');
    setDiscountCode('');
    toast({ title: t('checkout.discount.removed') });
  };

  const ArrowIcon = ArrowLeft;

  return (
    <div className="min-h-screen bg-amber-50/30 py-8">
      <div className="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
        <div className={`mb-6 flex items-center gap-2 ${dir === 'rtl' ? 'flex-row-reverse justify-between' : ''}`}>
          <Button variant="ghost" size="sm" className="gap-1" onClick={() => router.push('/products')}>
            <ArrowIcon className="size-4" /> {t('cart.continue')}
          </Button>
        </div>

        <h1 className={`mb-6 text-2xl font-bold ${dir === 'rtl' ? 'text-right' : ''}`}>
          {t('checkout.title')}
        </h1>

        {/* Step indicator */}
        <div className="mb-8" aria-label={t('checkout.steps', '')}>
          <ol className={`flex items-center ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
            {[
              { key: 'checkout.step.shipping', state: 'done' as const },
              { key: 'checkout.step.delivery', state: 'done' as const },
              { key: 'checkout.step.review', state: 'current' as const },
            ].map((step, i, arr) => (
              <li key={step.key} className={`flex items-center ${i < arr.length - 1 ? 'flex-1' : ''}`}>
                <span
                  className={`flex items-center gap-1.5 text-xs font-medium ${
                    step.state === 'done'
                      ? 'text-primary'
                      : step.state === 'current'
                        ? 'text-primary'
                        : 'text-muted-foreground'
                  }`}
                >
                  <span
                    className={`flex size-6 items-center justify-center rounded-full text-[10px] font-bold ${
                      step.state === 'done'
                        ? 'bg-primary text-primary-foreground'
                        : step.state === 'current'
                          ? 'bg-primary text-primary-foreground ring-4 ring-primary/20'
                          : 'bg-muted text-muted-foreground'
                    }`}
                  >
                    {step.state === 'done' ? '✓' : i + 1}
                  </span>
                  {t(step.key)}
                </span>
                {i < arr.length - 1 && (
                  <span
                    aria-hidden
                    className={`mx-2 h-0.5 flex-1 rounded ${
                      step.state === 'done' ? 'bg-primary/40' : 'bg-muted'
                    }`}
                  />
                )}
              </li>
            ))}
          </ol>
        </div>

        {items.length === 0 ? (
          <Card className="border-0 shadow-md rounded-2xl">
            <CardContent className="py-12 text-center">
              <ShoppingBag className="mx-auto mb-4 size-12 text-muted-foreground/50" />
              <p className="text-lg font-medium">{t('cart.empty')}</p>
              <Button className="mt-4 rounded-full" asChild>
                <Link href="/products">{t('cart.continue')}</Link>
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className={`grid gap-8 lg:grid-cols-5 ${dir === 'rtl' ? 'direction-rtl' : ''}`}>
            {/* Customer Info */}
            <div className="lg:col-span-3">
              <Card className="border-0 shadow-md rounded-2xl">
                <CardHeader>
                  <CardTitle className={dir === 'rtl' ? 'text-right' : ''}>
                    {t('checkout.shipping')}
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <form id="checkout-form" onSubmit={handleSubmit} className="space-y-4">
                    <div className={`grid grid-cols-2 gap-4 ${dir === 'rtl' ? 'grid-cols-2' : ''}`}>
                      <div className="space-y-2">
                        <Label htmlFor="fullName">{t('checkout.fullName')}</Label>
                        <Input id="fullName" value={info.fullName} onChange={updateField('fullName')} required dir={dir} />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="email">{t('checkout.email')}</Label>
                        <Input id="email" type="email" value={info.email} onChange={updateField('email')} required dir={dir} />
                      </div>
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="phone">{t('checkout.phone')}</Label>
                      <Input id="phone" type="tel" value={info.phone} onChange={updateField('phone')} required dir={dir} />
                    </div>

                    {/* Delivery method */}
                    <div className="space-y-2 pt-2">
                      <Label>{t('checkout.method.title')}</Label>
                      <div className="grid gap-2 sm:grid-cols-3">
                        {([
                          { id: 'POST', icon: Mail, title: t('checkout.method.post'), desc: t('checkout.method.post.desc'), cost: methods?.methods.find(m => m.method === 'POST')?.isFree ? t('checkout.method.free') : formatCurrency(methods?.methods.find(m => m.method === 'POST')?.price ?? 59_900, locale) },
                          { id: 'COURIER', icon: Truck, title: t('checkout.method.courier'), desc: t('checkout.method.courier.desc'), cost: methods?.methods.find(m => m.method === 'COURIER')?.isFree ? t('checkout.method.free') : formatCurrency(methods?.methods.find(m => m.method === 'COURIER')?.price ?? 129_000, locale) },
                          { id: 'PICKUP', icon: Store, title: t('checkout.method.pickup'), desc: t('checkout.method.pickup.desc'), cost: t('checkout.method.free') },
                        ] as const).map((m) => (
                          <button
                            key={m.id}
                            type="button"
                            onClick={() => setShippingMethod(m.id)}
                            aria-pressed={shippingMethod === m.id}
                            className={`rounded-xl border-2 p-3 text-start transition-all ${
                              shippingMethod === m.id
                                ? 'border-primary bg-primary/5 shadow-sm'
                                : 'border-border/70 hover:border-primary/40'
                            }`}
                          >
                            <m.icon className={`mb-2 size-5 ${shippingMethod === m.id ? 'text-primary' : 'text-muted-foreground'}`} />
                            <p className="text-sm font-semibold">{m.title}</p>
                            <p className="mt-0.5 text-[11px] leading-snug text-muted-foreground">{m.desc}</p>
                            <p className={`mt-1.5 text-xs font-bold ${m.cost === t('checkout.method.free') ? 'text-green-600' : 'text-foreground'}`}>
                              {m.cost}
                            </p>
                          </button>
                        ))}
                      </div>
                      {shippingMethod === 'PICKUP' && (
                        <p className="rounded-lg bg-amber-50 p-2.5 text-xs text-amber-800">
                          {t('checkout.method.pickup.info')}
                        </p>
                      )}
                    </div>

                    {/* Payment method — temporarily fixed to InPerson (پرداخت حضوری) */}
                    <div className="space-y-2 pt-2">
                      <Label>{t('checkout.payment.title')}</Label>
                      <div className="rounded-xl border-2 border-primary/40 bg-primary/5 p-3">
                        <div className="flex items-start gap-3">
                          <Banknote className={`mt-0.5 size-5 ${dir === 'rtl' ? '' : ''} text-primary`} />
                          <div>
                            <p className="text-sm font-semibold">{t('checkout.payment.inPerson')}</p>
                            <p className="mt-0.5 text-[11px] leading-snug text-muted-foreground">
                              {t('checkout.payment.inPerson.desc')}
                            </p>
                          </div>
                        </div>
                      </div>
                    </div>
                    {shippingMethod !== 'PICKUP' ? (
                      <>
                        <div className="space-y-2">
                          <Label htmlFor="address">{t('checkout.address')}</Label>
                          <Input id="address" value={info.address} onChange={updateField('address')} required dir={dir} />
                        </div>
                        <div className="grid grid-cols-2 gap-4">
                          <div className="space-y-2">
                            <Label htmlFor="city">{t('checkout.city')}</Label>
                            <Input id="city" value={info.city} onChange={updateField('city')} required dir={dir} />
                          </div>
                          <div className="space-y-2">
                            <Label htmlFor="postalCode">{t('checkout.postalCode')}</Label>
                            <Input id="postalCode" value={info.postalCode} onChange={updateField('postalCode')} required dir={dir} />
                          </div>
                        </div>
                      </>
                    ) : (
                      <div className="rounded-lg bg-muted p-3 text-xs text-muted-foreground">
                        {t('checkout.method.pickup.info')}
                      </div>
                    )}
                  </form>
                </CardContent>
              </Card>
            </div>

            {/* Order Summary */}
            <div className="lg:col-span-2">
              <Card className="border-0 shadow-md rounded-2xl">
                <CardHeader>
                  <CardTitle className={dir === 'rtl' ? 'text-right' : ''}>
                    {t('checkout.summary')}
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-3">
                    {items.map((item) => (
                      <div key={item.id} className={`flex items-center gap-3 ${dir === 'rtl' ? 'flex-row' : ''}`}>
                        <div className="size-14 shrink-0 overflow-hidden rounded-lg bg-muted">
                          {item.imageUrl && <img src={resolveImageUrl(item.imageUrl)} alt={item.productName} className="h-full w-full object-cover" />}
                        </div>
                        <div className="min-w-0 flex-1">
                          <p className="text-sm font-medium truncate">{item.productName}</p>
                          {item.colorName && (
                            <p className="flex items-center gap-1 text-xs text-muted-foreground">
                              <span className="size-2 rounded-full border border-black/10 bg-current" aria-hidden="true" />
                              {t('product.color')}: {item.colorName}
                            </p>
                          )}
                          <p className="text-xs text-muted-foreground">{t('product.quantity')}: {item.quantity}</p>
                        </div>
                        <p className="text-sm font-semibold">{formatCurrency(item.unitPrice * item.quantity, locale)}</p>
                      </div>
                    ))}
                  </div>

                  <Separator />

                  {/* Discount code */}
                  <div className="space-y-2">
                    <p className="text-sm font-medium">{t('checkout.discount.title')}</p>
                    {appliedDiscount ? (
                      <div className="flex items-center justify-between rounded-lg bg-green-50 px-3 py-2 text-sm">
                        <span className="font-semibold text-green-700" dir="ltr">{appliedDiscount.code}</span>
                        <div className="flex items-center gap-2">
                          <span className="text-green-700">−{formatCurrency(previewDiscountAmount, locale)}</span>
                          <button
                            type="button"
                            onClick={handleRemoveDiscount}
                            className="text-xs text-muted-foreground underline hover:text-destructive"
                          >
                            {t('checkout.discount.remove')}
                          </button>
                        </div>
                      </div>
                    ) : (
                      <>
                        <div className={`flex gap-2 ${dir === 'rtl' ? 'flex-row-reverse' : ''}`}>
                          <Input
                            dir="ltr"
                            value={discountCode}
                            onChange={(e) => { setDiscountCode(e.target.value.toUpperCase()); setDiscountError(''); }}
                            placeholder={t('checkout.discount.placeholder')}
                            className="store-input h-9 font-mono text-sm uppercase"
                            disabled={applyingDiscount}
                          />
                          <Button
                            type="button"
                            variant="outline"
                            className="h-9 shrink-0 gap-1 rounded-full px-3 text-xs"
                            disabled={applyingDiscount || !discountCode.trim()}
                            onClick={handleApplyDiscount}
                          >
                            {applyingDiscount ? <Loader2 className="size-3.5 animate-spin" /> : <Tag className="size-3.5" />}
                            {t('checkout.discount.apply')}
                          </Button>
                        </div>
                        {discountError && (
                          <p role="alert" className="text-xs text-destructive">{discountError}</p>
                        )}
                      </>
                    )}
                    <p className="text-[11px] text-muted-foreground">{t('checkout.discount.hint')}</p>
                  </div>

                  <div className={`space-y-1.5 text-sm ${dir === 'rtl' ? 'text-right' : ''}`}>
                    <div className="flex justify-between">
                      <span>{t('cart.subtotal')}</span>
                      <span>{formatCurrency(subtotal, locale)}</span>
                    </div>
                    {appliedDiscount && previewDiscountAmount > 0 && (
                      <div className="flex justify-between text-green-600">
                        <span>{t('checkout.discount.label')} ({appliedDiscount.code})</span>
                        <span>−{formatCurrency(previewDiscountAmount, locale)}</span>
                      </div>
                    )}
                    <div className="flex justify-between">
                      <span>
                        {t('cart.shipping')}
                        {shippingMethod !== 'POST' && (
                          <span className="ms-1 text-xs text-muted-foreground">
                            ({shippingMethod === 'COURIER' ? t('checkout.method.courier') : t('checkout.method.pickup')})
                          </span>
                        )}
                      </span>
                      <span>{shipping === 0
                        ? <span className="text-green-600">{t('checkout.shippingFree')}</span>
                        : formatCurrency(shipping, locale)}
                      </span>
                    </div>
                    <Separator />
                    <div className="flex justify-between text-base font-semibold">
                      <span>{t('cart.total')}</span>
                      <span>{formatCurrency(total, locale)}</span>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      {t('checkout.payment.inPerson')}
                    </p>
                  </div>

                  <Button
                    type="submit"
                    form="checkout-form"
                    className="w-full gap-2 rounded-full"
                    size="lg"
                    disabled={submitting}
                  >
                    {submitting
                      ? <Loader2 className="size-4 animate-spin" />
                      : <CreditCard className="size-4" />}
                    {submitting ? t('checkout.processing') : `${t('checkout.placeOrder')} — ${formatCurrency(total, locale)}`}
                  </Button>
                </CardContent>
              </Card>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
