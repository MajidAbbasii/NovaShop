"use client"

import * as React from "react"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { useLocale } from "@/lib/locale-context"
import { getAdminCustomDollRequests, type AdminCustomDollRequest } from "@/lib/custom-doll-api"
import { resolveImageUrl } from "@/lib/config"
import { formatCurrency } from "@/lib/formatters"
import { cn } from "@/lib/utils"
import { CameraIcon, EyeIcon, RefreshCwIcon } from "lucide-react"

export default function AdminCustomDollRequestsPage() {
  const { t, dir, locale } = useLocale()
  const [items, setItems] = React.useState<AdminCustomDollRequest[] | null>(null)
  const [error, setError] = React.useState(false)

  const load = React.useCallback(() => {
    setItems(null)
    setError(false)
    getAdminCustomDollRequests()
      .then(setItems)
      .catch(() => setError(true))
  }, [])

  React.useEffect(() => {
    load()
  }, [load])

  const badge = (status: string) => {
    const cls =
      status === "Approved" || status === "CustomerAccepted"
        ? "bg-green-100 text-green-700"
        : status === "Rejected"
          ? "bg-red-100 text-red-700"
          : "bg-amber-100 text-amber-700"
    const label =
      status === "Approved"
        ? t("customDoll.approved")
        : status === "CustomerAccepted"
          ? t("customDoll.customerAccepted")
          : status === "Rejected"
            ? t("customDoll.rejected")
            : t("customDoll.pendingReview")
    return (
      <span className={cn("inline-flex items-center rounded-full px-2.5 py-1 text-xs font-bold", cls)}>
        {label}
      </span>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="flex items-center gap-2 text-2xl font-bold">
            <CameraIcon className="size-6 text-primary" />
            {t("admin.customDoll.list")}
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">{t("customDoll.subtitle")}</p>
        </div>
        <Button variant="outline" size="sm" onClick={load}>
          <RefreshCwIcon className="size-4" />
        </Button>
      </div>

      {items === null && !error ? (
        <div className="space-y-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="store-skeleton h-16" />
          ))}
        </div>
      ) : error ? (
        <Card>
          <CardContent className="py-12 text-center text-muted-foreground">{t("notifications.loadError")}</CardContent>
        </Card>
      ) : items && items.length === 0 ? (
        <Card>
          <CardContent className="py-16 text-center text-muted-foreground">{t("customDoll.empty")}</CardContent>
        </Card>
      ) : (
        <div className="overflow-hidden rounded-2xl border shadow-sm">
          <table className="w-full bg-card text-sm">
            <thead className="bg-muted/60 text-xs text-muted-foreground">
              <tr>
                <th className="p-3 text-start">{t("admin.customDoll.customer")}</th>
                <th className="p-3 text-start">{t("customDoll.image")}</th>
                <th className="p-3 text-start">{t("customDoll.requestDate")}</th>
                <th className="p-3 text-start">{t("customDoll.status")}</th>
                <th className="p-3 text-start">{t("customDoll.price")}</th>
                <th className="p-3 text-start">{t("customDoll.reviewDate")}</th>
                <th className="p-3" />
              </tr>
            </thead>
            <tbody>
              {items!.map((r) => (
                <tr key={r.id} className="border-t border-border/60 transition-colors hover:bg-muted/40">
                  <td className="p-3">
                    <p className="font-medium">{r.customerUsername || r.customerPhone}</p>
                    <p className="text-xs text-muted-foreground" dir="ltr">
                      {r.customerPhone}
                    </p>
                  </td>
                  <td className="p-3">
                    <img src={resolveImageUrl(r.imageUrl)} alt="" className="size-12 rounded-lg border object-cover" />
                  </td>
                  <td className="p-3 text-muted-foreground">
                    {new Intl.DateTimeFormat(locale, { year: "numeric", month: "short", day: "numeric" }).format(
                      new Date(r.createdAt)
                    )}
                  </td>
                  <td className="p-3">{badge(r.status)}</td>
                  <td className="p-3 font-semibold">
                    {r.price != null ? formatCurrency(r.price, locale) : "—"}
                  </td>
                  <td className="p-3 text-muted-foreground">
                    {r.reviewedAt
                      ? new Intl.DateTimeFormat(locale, { year: "numeric", month: "short", day: "numeric" }).format(
                          new Date(r.reviewedAt)
                        )
                      : "—"}
                  </td>
                  <td className="p-3">
                    <Button variant="outline" size="sm" asChild>
                      <Link href={`/admin/custom-doll-requests/${r.id}`}>
                        <EyeIcon className="size-4" />
                        {t("customDoll.status")}
                      </Link>
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
