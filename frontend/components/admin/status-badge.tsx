'use client';

import { Badge } from "@/components/ui/badge"
import { statusKey } from "@/lib/admin-i18n"
import { useLocale } from "@/lib/locale-context"

type BadgeVariant = "default" | "secondary" | "destructive" | "outline"

const STATUS_VARIANT: Record<string, BadgeVariant> = {
  Pending: "outline",
  Confirmed: "secondary",
  Processing: "secondary",
  Paid: "default",
  Shipped: "default",
  Delivered: "default",
  Cancelled: "destructive",
  Failed: "destructive",
}

export function OrderStatusBadge({ status }: { status: string }) {
  const { t } = useLocale()
  return (
    <Badge variant={STATUS_VARIANT[status] || "outline"}>
      {t(statusKey(status))}
    </Badge>
  )
}

export function BooleanBadge({ value, trueLabel, falseLabel }: { value: boolean; trueLabel?: string; falseLabel?: string }) {
  const { t } = useLocale()
  return (
    <Badge variant={value ? "default" : "secondary"}>
      {value ? trueLabel ?? t('common.active') : falseLabel ?? t('common.inactive')}
    </Badge>
  )
}

export function StockBadge({ stock }: { stock: number }) {
  const { t, locale } = useLocale()
  const n = new Intl.NumberFormat(locale).format(stock)
  if (stock <= 0) return <Badge variant="destructive">{t('product.outOfStock')}</Badge>
  if (stock <= 5) return <Badge variant="outline">{t('admin.products.lowStock')} ({n})</Badge>
  return <Badge variant="default">{t('admin.products.available')} ({n})</Badge>
}