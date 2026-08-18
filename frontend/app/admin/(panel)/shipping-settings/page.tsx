'use client';

import { useState, useEffect } from 'react';
import { useLocale } from '@/lib/locale-context';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { toast } from '@/hooks/use-toast';
import { Truck, Mail, Store, Loader2 } from 'lucide-react';
import {
  getShippingSettings,
  updateShippingSettings,
  type ShippingSettings,
} from '@/lib/shipping-api';

export default function AdminShippingSettingsPage() {
  const { t, dir } = useLocale();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<ShippingSettings>({
    courierPrice: 0,
    postPrice: 0,
    postFreeShippingThreshold: 0,
    pickupPrice: 0,
  });

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const s = await getShippingSettings();
        if (!cancelled) setForm(s);
      } catch {
        if (!cancelled) toast({ title: t('admin.loadError'), variant: 'destructive' });
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [t]);

  const update = (key: keyof ShippingSettings) => (e: React.ChangeEvent<HTMLInputElement>) => {
    const v = Number(e.target.value.replace(/[^0-9]/g, '')) || 0;
    setForm((prev) => ({ ...prev, [key]: v }));
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const saved = await updateShippingSettings(form);
      setForm(saved);
      toast({ title: t('common.saved') });
    } catch (err) {
      toast({
        title: t('common.saveError') ?? 'Save failed',
        description: err instanceof Error ? err.message : undefined,
        variant: 'destructive',
      });
    } finally {
      setSaving(false);
    }
  };

  const fields: {
    key: keyof ShippingSettings;
    labelKey: string;
    icon: typeof Truck;
  }[] = [
    { key: 'courierPrice', labelKey: 'courierCost', icon: Truck },
    { key: 'postPrice', labelKey: 'postalCost', icon: Mail },
    { key: 'pickupPrice', labelKey: 'checkout.method.pickup', icon: Store },
    { key: 'postFreeShippingThreshold', labelKey: 'checkout.shippingFreeThreshold', icon: Mail },
  ];

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="size-8 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div className={`mx-auto max-w-2xl ${dir === 'rtl' ? 'text-right' : ''}`}>
      <h1 className="mb-6 text-2xl font-bold">{t('shippingSettings')}</h1>

      <form onSubmit={handleSave}>
        <Card className="border-0 shadow-md rounded-2xl">
          <CardHeader>
            <CardTitle>{t('shipping')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            {fields.map((f) => (
              <div key={f.key} className="space-y-2">
                <Label htmlFor={f.key} className="flex items-center gap-2">
                  <f.icon className="size-4 text-muted-foreground" />
                  {t(f.labelKey)}
                </Label>
                <div className="flex items-center gap-2">
                  <Input
                    id={f.key}
                    type="text"
                    inputMode="numeric"
                    value={form[f.key].toLocaleString('fa-IR')}
                    onChange={update(f.key)}
                    dir="ltr"
                    className="store-input"
                  />
                  <span className="shrink-0 text-sm text-muted-foreground">تومان</span>
                </div>
              </div>
            ))}
          </CardContent>
        </Card>

        <Separator className="my-6" />

        <Button type="submit" size="lg" className="w-full gap-2 rounded-full" disabled={saving}>
          {saving ? <Loader2 className="size-4 animate-spin" /> : null}
          {saving ? t('common.saving') ?? 'Saving…' : t('common.save')}
        </Button>
      </form>
    </div>
  );
}
