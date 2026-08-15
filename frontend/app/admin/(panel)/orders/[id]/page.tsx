"use client"

import * as React from "react"
import { useParams, useRouter } from "next/navigation"
import { getAdminOrder, updateOrderStatus, type OrderDto } from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"
import { PageHeader } from "@/components/admin/page-header"
import { OrderStatusBadge } from "@/components/admin/status-badge"
import { VALID_TRANSITIONS, formatCurrency, formatDate, statusKey, ORDER_STATUS_LABELS } from "@/lib/admin-i18n"
import { Loader2Icon, ArrowRightIcon } from "lucide-react"
import { ErrorState } from "@/components/admin/states"

const STATUS_FLOW = ["Pending", "Confirmed", "Processing", "Paid", "Shipped", "Delivered"]

function StatusTimeline({ status }: { status: string }) {
  const currentIdx = STATUS_FLOW.indexOf(status)

  return (
    <ol className="space-y-1">
      {STATUS_FLOW.map((s, i) => {
        const isDone = currentIdx >= i
        const isCurrent = currentIdx === i
        return (
          <li key={s} className="flex items-center gap-3" aria-current={isCurrent ? "step" : undefined}>
            <span className="flex flex-col items-center self-stretch">
              <span
                className={`mt-1 flex size-3 shrink-0 rounded-full border-2 ${
                  isDone ? "border-primary bg-primary" : "border-muted-foreground/40"
                } ${isCurrent ? "ring-4 ring-primary/20" : ""}`}
              />
              {i < STATUS_FLOW.length - 1 && (
                <span className={`w-0.5 flex-1 ${isDone && i < currentIdx ? "bg-primary" : "bg-border"}`} />
              )}
            </span>
            <span className={`text-xs ${isCurrent ? "font-semibold text-foreground" : "text-muted-foreground"}`}>
              {s}
            </span>
          </li>
        )
      })}
      {(status === "Cancelled" || status === "Failed") && (
        <li className="mt-2">
          <Badge variant="destructive">{status === "Cancelled" ? "سفارش لغو شد" : "پرداخت ناموفق"}</Badge>
        </li>
      )}
    </ol>
  )
}

export default function AdminOrderDetailPage() {
  const params = useParams<{ id: string }>()
  const router = useRouter()
  const id = Number(params.id)

  const [order, setOrder] = React.useState<OrderDto | null>(null)
  const [error, setError] = React.useState("")
  const [updating, setUpdating] = React.useState<string | null>(null)

  const fetch = React.useCallback(() => {
    getAdminOrder(id)
      .then((o) => { setOrder(o); setError("") })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [id])

  React.useEffect(() => { fetch() }, [fetch])

  const handleStatus = async (status: string) => {
    setUpdating(status)
    try {
      const updated = await updateOrderStatus(id, status)
      setOrder(updated)
      toast({ title: "وضعیت سفارش به‌روزرسانی شد" })
    } catch (e: unknown) {
      toast({
        title: "خطا در به‌روزرسانی وضعیت",
        description: e instanceof Error ? e.message : "خطا",
        variant: "destructive",
      })
    } finally {
      setUpdating(null)
    }
  }

  if (error || !order) {
    return (
      <div className="space-y-4">
        <PageHeader title="جزئیات سفارش" breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "سفارش‌ها", href: "/admin/orders" }, { label: `سفارش ${id}` }]} />
        <Card>
          <ErrorState message="خطا در دریافت سفارش" onRetry={fetch} />
        </Card>
      </div>
    )
  }

  if (!order) {
    return (
      <div className="space-y-4">
        <div className="h-8 w-56 animate-pulse rounded bg-muted" />
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="h-72 animate-pulse rounded-xl border bg-card" />
          <div className="h-72 animate-pulse rounded-xl border bg-card lg:col-span-2" />
        </div>
      </div>
    )
  }

  const nextActions = VALID_TRANSITIONS[order.status] || []

  return (
    <div className="space-y-6">
      <PageHeader
        title={`سفارش #${id}`}
        description={formatDate(order.createdAt)}
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "سفارش‌ها", href: "/admin/orders" }, { label: `سفارش ${id}` }]}
        actions={
          <Button variant="outline" size="sm" onClick={() => router.push("/admin/orders")}>
            <ArrowRightIcon className="size-3.5" />
            بازگشت به سفارش‌ها
          </Button>
        }
      />

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Order info */}
        <Card>
          <CardHeader><CardTitle className="text-sm">اطلاعات سفارش</CardTitle></CardHeader>
          <CardContent className="space-y-3 text-sm">
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">وضعیت</span>
              <OrderStatusBadge status={order.status} />
            </div>
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">مبلغ کل</span>
              <span className="font-bold tabular-nums">{formatCurrency(order.totalAmount)}</span>
            </div>
            {order.discountCode && (
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">کد تخفیف</span>
                <Badge variant="outline">{order.discountCode} ({formatCurrency(order.discountAmount)})</Badge>
              </div>
            )}
            <Separator />
            <div>
              <p className="text-muted-foreground">آدرس ارسال</p>
              <p className="mt-1 leading-relaxed">{order.shippingAddress}</p>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">شناسه مشتری</span>
              <span className="font-medium">#{order.userId}</span>
            </div>
            {order.trackingCode && (
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">کد رهگیری</span>
                <span className="font-mono text-xs font-semibold" dir="ltr">{order.trackingCode}</span>
              </div>
            )}
            {order.trackingNumber && (
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">شماره مرسوله</span>
                <span className="font-mono text-xs" dir="ltr">{order.trackingNumber}</span>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Items + status */}
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="text-sm">اقلام سفارش ({order.items.length})</CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="divide-y">
                {order.items.map((item, i) => (
                  <li key={i} className="flex items-center justify-between gap-3 py-2.5">
                    <div className="min-w-0">
                      <p className="text-sm font-medium">{item.productName}</p>
                      <p className="text-xs text-muted-foreground">
                        {item.quantity.toLocaleString("fa-IR")} × {formatCurrency(item.unitPrice)}
                      </p>
                    </div>
                    <p className="text-sm font-semibold tabular-nums">
                      {formatCurrency(item.unitPrice * item.quantity)}
                    </p>
                  </li>
                ))}
              </ul>
              <Separator className="my-2" />
              <div className="flex items-center justify-between text-sm font-bold">
                <span>مبلغ نهایی</span>
                <span className="tabular-nums text-lg">{formatCurrency(order.totalAmount)}</span>
              </div>
            </CardContent>
          </Card>

          {order.payment && (
            <Card>
              <CardHeader><CardTitle className="text-sm">پرداخت</CardTitle></CardHeader>
              <CardContent className="grid gap-3 text-sm sm:grid-cols-2">
                <div>
                  <p className="text-muted-foreground">روش پرداخت</p>
                  <p className="mt-0.5 font-medium">{order.payment.paymentMethod}</p>
                </div>
                <div>
                  <p className="text-muted-foreground">وضعیت پرداخت</p>
                  <Badge variant={order.payment.status === "Completed" ? "default" : "outline"}>
                    {order.payment.status}
                  </Badge>
                </div>
                {order.payment.transactionId && (
                  <div className="sm:col-span-2">
                    <p className="text-muted-foreground">شناسه تراکنش</p>
                    <p className="mt-0.5 break-all font-mono text-xs" dir="ltr">{order.payment.transactionId}</p>
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          <Card>
            <CardHeader><CardTitle className="text-sm">وضعیت سفارش</CardTitle></CardHeader>
            <CardContent>
              <StatusTimeline status={order.status} />
              {nextActions.length > 0 && (
                <>
                  <Separator className="my-4" />
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-xs text-muted-foreground">به‌روزرسانی وضعیت:</span>
                    {nextActions.map((s) => (
                      <Button
                        key={s}
                        size="sm"
                        variant={s === "Cancelled" ? "destructive" : "default"}
                        disabled={updating !== null}
                        onClick={() => handleStatus(s)}
                      >
                        {updating === s && <Loader2Icon className="size-3.5 animate-spin" />}
                        {s === "Confirmed" ? "تأیید" : s === "Pending" ? "در انتظار" : s === "Shipped" ? "ارسال شود" : s === "Delivered" ? "تحویل شده" : s === "Processing" ? "در حال پردازش" : s === "Paid" ? "پرداخت شد" : s}
                      </Button>
                    ))}
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {(order.statusHistory?.length > 0) && (
            <Card>
              <CardHeader><CardTitle className="text-sm">تاریخچه وضعیت</CardTitle></CardHeader>
              <CardContent>
                <ol className="space-y-3">
                  {order.statusHistory.map((h, i) => (
                    <li key={i} className="flex gap-3">
                      <span className="mt-1.5 flex size-2.5 shrink-0 rounded-full bg-primary" />
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2 text-sm">
                          <Badge variant="outline" className="text-xs">
                            {h.fromStatus ? ORDER_STATUS_LABELS[h.fromStatus] ?? h.fromStatus : "—"}
                          </Badge>
                          <span className="text-muted-foreground">←</span>
                          <Badge className="text-xs">{ORDER_STATUS_LABELS[h.toStatus] ?? h.toStatus}</Badge>
                          {h.changedByRole === "Customer" && (
                            <span className="text-xs text-muted-foreground">(مشتری)</span>
                          )}
                        </div>
                        {h.note && <p className="mt-1 text-xs text-muted-foreground">{h.note}</p>}
                        <p className="mt-0.5 text-xs text-muted-foreground">{formatDate(h.changedAt)}</p>
                      </div>
                    </li>
                  ))}
                </ol>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}
