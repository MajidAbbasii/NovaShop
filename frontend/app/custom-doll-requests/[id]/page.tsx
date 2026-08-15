"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useLocale } from "@/lib/locale-context";
import { isAuthenticated } from "@/lib/cart-api";
import { getMyCustomDollRequest, acceptCustomDollRequest, type CustomDollRequest } from "@/lib/custom-doll-api";
import { resolveImageUrl } from "@/lib/config";
import { formatCurrency } from "@/lib/formatters";
import { toast } from "@/hooks/use-toast";
import { cn } from "@/lib/utils";
import { Camera, ArrowRight, BadgeCheck, XCircle, Clock, MessageSquareText, Coins } from "lucide-react";

function statusBadge(status: string, t: (k: string) => string) {
  const cls =
    status === "Approved" || status === "CustomerAccepted"
      ? "bg-green-100 text-green-700"
      : status === "Rejected"
        ? "bg-red-100 text-red-700"
        : "bg-amber-100 text-amber-700";
  const label =
    status === "Approved"
      ? t("customDoll.approved")
      : status === "CustomerAccepted"
        ? t("customDoll.customerAccepted")
        : status === "Rejected"
          ? t("customDoll.rejected")
          : t("customDoll.pendingReview");
  return (
    <span className={cn("inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-bold", cls)}>
      {status === "Approved" || status === "CustomerAccepted" ? <BadgeCheck className="size-4" /> : status === "Rejected" ? <XCircle className="size-4" /> : <Clock className="size-4" />}
      {label}
    </span>
  );
}

export default function CustomDollRequestDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { t, dir, locale } = useLocale();
  const [req, setReq] = useState<CustomDollRequest | null>(null);
  const [error, setError] = useState(false);
  const [authed, setAuthed] = useState(false);

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace("/login");
      return;
    }
    setAuthed(true);
    getMyCustomDollRequest(Number(id))
      .then(setReq)
      .catch(() => setError(true));
  }, [id, router]);

  if (!authed) return null;

  return (
    <div className="min-h-screen bg-amber-50/30 py-10">
      <div className="mx-auto max-w-2xl px-4 sm:px-6 lg:px-8">
        <div className={`mb-6 flex items-center justify-between ${dir === "rtl" ? "flex-row-reverse" : ""}`}>
          <h1 className="flex items-center gap-2 text-2xl font-bold">
            <Camera className="size-6 text-primary" />
            {t("customDoll.title")} #{id}
          </h1>
          <Button variant="outline" size="sm" asChild>
            <Link href="/custom-doll-requests">
              <ArrowRight className="size-4 rtl:rotate-180" />
              {t("customDoll.myRequests")}
            </Link>
          </Button>
        </div>

        {!req && !error ? (
          <div className="space-y-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="store-skeleton h-24" />
            ))}
          </div>
        ) : error ? (
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-12 text-center text-muted-foreground">{t("notifications.loadError")}</CardContent>
          </Card>
        ) : req ? (
          <div className="space-y-5">
            <Card className="rounded-2xl border-0 shadow-md">
              <CardContent className="space-y-5 p-6">
                <div className="flex items-center justify-between">
                  {statusBadge(req.status, (k) => t(k))}
                  <span className="text-sm text-muted-foreground">
                    {new Intl.DateTimeFormat(locale, { year: "numeric", month: "long", day: "numeric" }).format(
                      new Date(req.createdAt)
                    )}
                  </span>
                </div>

                <img
                  src={resolveImageUrl(req.imageUrl)}
                  alt={t("customDoll.uploadImage")}
                  className="mx-auto max-h-80 w-full rounded-xl border object-contain bg-muted/40"
                />

                {req.description && (
                  <div className="rounded-xl bg-muted/50 p-4">
                    <p className="mb-1 flex items-center gap-1.5 text-sm font-semibold">
                      <MessageSquareText className="size-4 text-primary" />
                      {t("customDoll.descriptionLabel")}
                    </p>
                    <p className="text-sm leading-relaxed text-muted-foreground">{req.description}</p>
                  </div>
                )}

                {req.status === "Approved" && req.price != null && (
                  <div className="rounded-xl border border-green-200 bg-green-50 p-4 text-center">
                    <p className="mb-1 flex items-center justify-center gap-1.5 text-sm font-semibold text-green-800">
                      <Coins className="size-4" />
                      {t("customDoll.price")}
                    </p>
                    <p className="text-2xl font-bold text-green-700">
                      {formatCurrency(req.price, locale)}
                    </p>
                  </div>
                )}

                {req.status === "Approved" && (
                  <div className="rounded-xl border border-primary/20 bg-primary/5 p-4 text-center">
                    <p className="mb-3 text-sm text-muted-foreground">{t("customDoll.acceptHint")}</p>
                    <Button
                      className="gap-1.5 rounded-full bg-green-600 hover:bg-green-700"
                      onClick={async () => {
                        try {
                          await acceptCustomDollRequest(req.id);
                          setReq({ ...req, status: "CustomerAccepted" });
                          toast({ title: t("customDoll.accepted") });
                        } catch (e) {
                          toast({
                            title: t("customDoll.accepted"),
                            description: e instanceof Error ? e.message : undefined,
                            variant: "destructive",
                          });
                        }
                      }}
                    >
                      <BadgeCheck className="size-4" />
                      {t("customDoll.accept")}
                    </Button>
                  </div>
                )}

                {req.status === "Rejected" && (
                  <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-center">
                    <p className="text-sm font-semibold text-red-700">{t("customDoll.rejected")}</p>
                  </div>
                )}

                {req.adminMessage && (
                  <div className="rounded-xl border bg-primary/5 p-4">
                    <p className="mb-1 flex items-center gap-1.5 text-sm font-semibold">
                      <MessageSquareText className="size-4 text-primary" />
                      {t("customDoll.adminMessage")}
                    </p>
                    <p className="text-sm leading-relaxed text-muted-foreground">{req.adminMessage}</p>
                  </div>
                )}

                {req.reviewedAt && (
                  <p className="text-center text-xs text-muted-foreground">
                    {t("customDoll.reviewDate")}:{" "}
                    {new Intl.DateTimeFormat(locale, { year: "numeric", month: "long", day: "numeric" }).format(
                      new Date(req.reviewedAt)
                    )}
                  </p>
                )}
              </CardContent>
            </Card>
          </div>
        ) : null}
      </div>
    </div>
  );
}