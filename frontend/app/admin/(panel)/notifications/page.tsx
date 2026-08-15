"use client"

import * as React from "react"
import { getSmsNotifications, type SmsNotificationDto } from "@/lib/admin-api"
import { PageHeader } from "@/components/admin/page-header"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { useAdminFormatters, toPersianDigits } from "@/lib/admin-i18n"
import { Loader2Icon, MessageSquareIcon } from "lucide-react"
import { ErrorState } from "@/components/admin/states"

const EVENT_LABELS: Record<string, string> = {
  OrderPlaced: "ثبت سفارش",
  PaymentSuccessful: "پرداخت موفق",
  Status_Pending: "در انتظار پرداخت",
  Status_Confirmed: "تأیید سفارش",
  Status_Processing: "آماده‌سازی",
  Status_Paid: "پرداخت موفق",
  Status_Shipped: "ارسال",
  Status_Delivered: "تحویل",
  Status_Cancelled: "لغو سفارش",
  Status_Failed: "ناموفق",
}

export default function AdminSmsPage() {
  const [items, setItems] = React.useState<SmsNotificationDto[] | null>(null)
  const [error, setError] = React.useState("")
  const { date } = useAdminFormatters()

  const fetch = React.useCallback(() => {
    getSmsNotifications({ pageSize: 100 })
      .then((r) => { setItems(r.items); setError("") })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [])

  React.useEffect(() => { fetch() }, [fetch])

  return (
    <div className="space-y-6">
      <PageHeader
        title="پیامک‌ها"
        description="گزارش پیامک‌های ارسال‌شده برای رویدادهای سفارش"
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "پیامک‌ها" }]}
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
            <p className="p-12 text-center text-sm text-muted-foreground">پیامکی ثبت نشده است</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>تاریخ</TableHead>
                  <TableHead>رویداد</TableHead>
                  <TableHead>گیرنده</TableHead>
                  <TableHead>وضعیت</TableHead>
                  <TableHead>سرویس‌دهنده</TableHead>
                  <TableHead>متن پیامک</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((n) => (
                  <TableRow key={n.id}>
                    <TableCell className="whitespace-nowrap text-xs text-muted-foreground">{date(n.createdAt)}</TableCell>
                    <TableCell>
                      <Badge variant="outline">{EVENT_LABELS[n.eventType] ?? n.eventType}</Badge>
                    </TableCell>
                    <TableCell className="font-mono text-xs" dir="ltr">{n.phoneNumber}</TableCell>
                    <TableCell>
                      <Badge variant={n.status === "Sent" ? "default" : n.status === "Failed" ? "destructive" : "secondary"}>
                        {n.status === "Sent" ? "ارسال شد" : n.status === "Failed" ? "ناموفق" : n.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground">{n.provider}</TableCell>
                    <TableCell className="max-w-72">
                      <div className="flex items-start gap-2">
                        <MessageSquareIcon className="mt-0.5 size-3.5 shrink-0 text-muted-foreground" />
                        <p className="line-clamp-2 text-xs leading-relaxed">{n.message}</p>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
