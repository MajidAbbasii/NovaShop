"use client"

import * as React from "react"
import { getInventoryTransactions, type InventoryTransactionDto } from "@/lib/admin-api"
import { PageHeader } from "@/components/admin/page-header"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { useAdminFormatters, toPersianDigits } from "@/lib/admin-i18n"
import { Loader2Icon } from "lucide-react"
import { ErrorState } from "@/components/admin/states"

const TYPE_LABELS: Record<string, { label: string; variant: "default" | "outline" | "destructive" | "secondary" }> = {
  Reserve: { label: "رزرو", variant: "secondary" },
  Confirm: { label: "تأیید (کسر قطعی)", variant: "default" },
  Release: { label: "بازگشت", variant: "outline" },
}

export default function AdminInventoryPage() {
  const [items, setItems] = React.useState<InventoryTransactionDto[] | null>(null)
  const [error, setError] = React.useState("")
  const { date } = useAdminFormatters()

  const fetch = React.useCallback(() => {
    getInventoryTransactions({ pageSize: 100 })
      .then((r) => { setItems(r.items); setError("") })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [])

  React.useEffect(() => { fetch() }, [fetch])

  return (
    <div className="space-y-6">
      <PageHeader
        title="مدیریت موجودی"
        description="تراکنش‌های تغییر موجودی (رزرو، تأیید، بازگشت)"
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "موجودی" }]}
      />
      <Card>
        <CardContent className="p-0">
          {error ? (
            <div className="p-6"><ErrorState message={error} onRetry={fetch} /></div>
          ) : !items ? (
            <div className="flex items-center justify-center gap-2 p-12 text-muted-foreground">
              <Loader2Icon className="size-5 animate-spin" /> در حال بارگذاری
            </div>
          ) : items.length === 0 ? (
            <p className="p-12 text-center text-sm text-muted-foreground">تراکنشی ثبت نشده است</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>تاریخ</TableHead>
                  <TableHead>محصول</TableHead>
                  <TableHead>نوع</TableHead>
                  <TableHead className="text-center">مقدار</TableHead>
                  <TableHead className="text-center">موجودی قبل</TableHead>
                  <TableHead className="text-center">موجودی بعد</TableHead>
                  <TableHead>سفارش</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((t) => {
                  const meta = TYPE_LABELS[t.type] ?? { label: t.type, variant: "outline" as const }
                  return (
                    <TableRow key={t.id}>
                      <TableCell className="whitespace-nowrap text-xs text-muted-foreground">{date(t.createdAt)}</TableCell>
                      <TableCell className="max-w-48 truncate text-sm font-medium">{t.productName}</TableCell>
                      <TableCell><Badge variant={meta.variant}>{meta.label}</Badge></TableCell>
                      <TableCell className="text-center tabular-nums">{toPersianDigits(t.quantity)}</TableCell>
                      <TableCell className="text-center tabular-nums text-muted-foreground">{toPersianDigits(t.stockBefore)}</TableCell>
                      <TableCell className="text-center tabular-nums font-semibold">{toPersianDigits(t.stockAfter)}</TableCell>
                      <TableCell className="text-xs text-muted-foreground">
                        {t.orderId ? `#${toPersianDigits(t.orderId)}` : "—"}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
