"use client"

import * as React from "react"
import { useParams, useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { useLocale } from "@/lib/locale-context"
import {
  approveCustomDollRequest,
  getAdminCustomDollRequest,
  rejectCustomDollRequest,
  type AdminCustomDollRequest,
} from "@/lib/custom-doll-api"
import { resolveImageUrl } from "@/lib/config"
import { formatCurrency } from "@/lib/formatters"
import { toast } from "@/hooks/use-toast"
import { cn } from "@/lib/utils"
import { CameraIcon, CheckCircle2Icon, XCircleIcon, Loader2Icon, UserIcon, PhoneIcon, MessageSquareIcon } from "lucide-react"

export default function AdminCustomDollRequestDetailPage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()
  const { t, dir, locale } = useLocale()
  const [req, setReq] = React.useState<AdminCustomDollRequest | null>(null)
  const [error, setError] = React.useState(false)
  const [price, setPrice] = React.useState("")
  const [message, setMessage] = React.useState("")
  const [busy, setBusy] = React.useState<"approve" | "reject" | null>(null)

  React.useEffect(() => {
    getAdminCustomDollRequest(Number(id))
      .then(setReq)
      .catch(() => setError(true))
  }, [id])

  if (error) {
    return (
      <Card>
        <CardContent className="py-12 text-center text-muted-foreground">{t("notifications.loadError")}</CardContent>
      </Card>
    )
  }

  if (!req) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="store-skeleton h-24" />
        ))}
      </div>
    )
  }

  const pending = req.status === "PendingReview"

  const onApprove = async () => {
    const p = Number(price)
    if (!p || p <= 0) {
      toast({ title: t("admin.customDoll.priceRequired"), variant: "destructive" })
      return
    }
    setBusy("approve")
    try {
      await approveCustomDollRequest(req.id, p, message)
      toast({ title: t("admin.customDoll.approved") })
      router.refresh()
      setReq({ ...req, status: "Approved", price: p, adminMessage: message })
    } catch (e) {
      toast({
        title: t("admin.customDoll.approved"),
        description: e instanceof Error ? e.message : undefined,
        variant: "destructive",
      })
    } finally {
      setBusy(null)
    }
  }

  const onReject = async () => {
    setBusy("reject")
    try {
      await rejectCustomDollRequest(req.id, message)
      toast({ title: t("admin.customDoll.rejected") })
      router.refresh()
      setReq({ ...req, status: "Rejected", adminMessage: message })
    } catch (e) {
      toast({
        title: t("admin.customDoll.rejected"),
        description: e instanceof Error ? e.message : undefined,
        variant: "destructive",
      })
    } finally {
      setBusy(null)
    }
  }

  const badge = (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-bold",
        req.status === "Approved"
          ? "bg-green-100 text-green-700"
          : req.status === "Rejected"
            ? "bg-red-100 text-red-700"
            : "bg-amber-100 text-amber-700"
      )}
    >
      {req.status === "Approved"
        ? t("customDoll.approved")
        : req.status === "Rejected"
          ? t("customDoll.rejected")
          : t("customDoll.pendingReview")}
    </span>
  )

  return (
    <div className="space-y-6">
      <div className={`flex items-center justify-between ${dir === "rtl" ? "flex-row-reverse" : ""}`}>
        <h1 className="flex items-center gap-2 text-2xl font-bold">
          <CameraIcon className="size-6 text-primary" />
          {t("customDoll.title")} #{req.id}
        </h1>
        {badge}
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t("customDoll.uploadImage")}</CardTitle>
          </CardHeader>
          <CardContent>
            <img
              src={resolveImageUrl(req.imageUrl)}
              alt={t("customDoll.uploadImage")}
              className="max-h-96 w-full rounded-xl border object-contain bg-muted/40"
            />
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t("admin.customDoll.customer")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <p className="flex items-center gap-2">
                <UserIcon className="size-4 text-muted-foreground" />
                {req.customerUsername || "—"}
              </p>
              <p className="flex items-center gap-2" dir="ltr">
                <PhoneIcon className="size-4 text-muted-foreground" />
                {req.customerPhone || "—"}
              </p>
              {req.customerEmail && (
                <p className="flex items-center gap-2" dir="ltr">
                  <MessageSquareIcon className="size-4 text-muted-foreground" />
                  {req.customerEmail}
                </p>
              )}
              <p className="text-xs text-muted-foreground">
                {t("customDoll.requestDate")}:{" "}
                {new Intl.DateTimeFormat(locale, { year: "numeric", month: "long", day: "numeric" }).format(
                  new Date(req.createdAt)
                )}
              </p>
              {req.description && (
                <div className="rounded-xl bg-muted/50 p-3">
                  <p className="mb-1 text-xs font-semibold text-muted-foreground">{t("customDoll.descriptionLabel")}</p>
                  <p className="text-sm leading-relaxed">{req.description}</p>
                </div>
              )}
              {req.price != null && (
                <p className="rounded-xl border border-green-200 bg-green-50 p-3 text-center font-bold text-green-700">
                  {formatCurrency(req.price, locale)}
                </p>
              )}
              {req.adminMessage && (
                <div className="rounded-xl border bg-primary/5 p-3">
                  <p className="mb-1 text-xs font-semibold text-muted-foreground">{t("customDoll.adminMessage")}</p>
                  <p className="text-sm leading-relaxed">{req.adminMessage}</p>
                </div>
              )}
            </CardContent>
          </Card>

          {pending && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">{t("customDoll.approvalPrice")}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <Label>{t("admin.customDoll.price")}</Label>
                  <Input
                    type="number"
                    min={0}
                    value={price}
                    onChange={(e) => setPrice(e.target.value)}
                    placeholder={t("customDoll.pricePlaceholder")}
                    dir="ltr"
                  />
                </div>
                <div className="space-y-2">
                  <Label>{t("customDoll.adminMessage")}</Label>
                  <Textarea
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    placeholder={t("admin.customDoll.messagePlaceholder")}
                    rows={3}
                  />
                </div>
                <div className={`flex gap-3 ${dir === "rtl" ? "flex-row-reverse" : ""}`}>
                  <Button className="flex-1 gap-1.5 bg-green-600 hover:bg-green-700" onClick={onApprove} disabled={busy !== null}>
                    {busy === "approve" ? <Loader2Icon className="size-4 animate-spin" /> : <CheckCircle2Icon className="size-4" />}
                    {t("customDoll.approve")}
                  </Button>
                  <Button variant="destructive" className="flex-1 gap-1.5" onClick={onReject} disabled={busy !== null}>
                    {busy === "reject" ? <Loader2Icon className="size-4 animate-spin" /> : <XCircleIcon className="size-4" />}
                    {t("customDoll.reject")}
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}
