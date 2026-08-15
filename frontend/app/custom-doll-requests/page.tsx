"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { useLocale } from "@/lib/locale-context";
import { isAuthenticated } from "@/lib/cart-api";
import { getMyCustomDollRequests, type CustomDollRequest } from "@/lib/custom-doll-api";
import { resolveImageUrl } from "@/lib/config";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";
import { Camera, Plus, Send } from "lucide-react";

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
    <span className={cn("inline-flex items-center rounded-full px-2.5 py-1 text-xs font-bold", cls)}>
      {label}
    </span>
  );
}

export default function MyCustomDollRequestsPage() {
  const router = useRouter();
  const { t, dir, locale } = useLocale();
  const [items, setItems] = useState<CustomDollRequest[] | null>(null);
  const [error, setError] = useState(false);
  const [authed, setAuthed] = useState(false);

  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace("/login");
      return;
    }
    setAuthed(true);
    getMyCustomDollRequests()
      .then(setItems)
      .catch(() => setError(true));
  }, [router]);

  if (!authed) return null;

  return (
    <div className="min-h-screen bg-amber-50/30 py-10">
      <div className="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8">
        <div className={`mb-6 flex items-center justify-between ${dir === "rtl" ? "flex-row-reverse" : ""}`}>
          <h1 className="flex items-center gap-2 text-2xl font-bold">
            <Camera className="size-6 text-primary" />
            {t("customDoll.requestList")}
          </h1>
          <Button size="sm" className="rounded-full gap-1.5" asChild>
            <Link href="/custom-doll-request">
              <Plus className="size-4" />
              {t("customDoll.createNew")}
            </Link>
          </Button>
        </div>

        {items === null && !error ? (
          <div className="space-y-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="store-skeleton h-20" />
            ))}
          </div>
        ) : error ? (
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-12 text-center text-muted-foreground">{t("notifications.loadError")}</CardContent>
          </Card>
        ) : items && items.length === 0 ? (
          <Card className="rounded-2xl border-0 shadow-md">
            <CardContent className="py-16 text-center">
              <Send className="mx-auto mb-4 size-12 text-muted-foreground/50" />
              <p className="text-lg font-medium">{t("customDoll.empty")}</p>
              <p className="mt-1 text-sm text-muted-foreground">{t("customDoll.emptyDesc")}</p>
              <Button className="mt-6 rounded-full" asChild>
                <Link href="/custom-doll-request">
                  <Camera className="size-4" />
                  {t("customDoll.createNew")}
                </Link>
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className="overflow-hidden rounded-2xl border-0 shadow-md">
            <table className="w-full bg-card text-sm">
              <thead className="bg-muted/60 text-xs text-muted-foreground">
                <tr>
                  <th className="p-3 text-start">{t("customDoll.image")}</th>
                  <th className="p-3 text-start">{t("customDoll.requestDate")}</th>
                  <th className="p-3 text-start">{t("customDoll.status")}</th>
                  <th className="p-3 text-start">{t("customDoll.price")}</th>
                </tr>
              </thead>
              <tbody>
                {items!.map((r) => (
                  <tr key={r.id} className="border-t border-border/60 transition-colors hover:bg-muted/40">
                    <td className="p-3">
                      <Link href={`/custom-doll-requests/${r.id}`} className="flex items-center gap-3">
                        <img
                          src={resolveImageUrl(r.imageUrl)}
                          alt=""
                          className="size-14 rounded-lg border object-cover"
                        />
                        #{r.id}
                      </Link>
                    </td>
                    <td className="p-3">
                      <Link href={`/custom-doll-requests/${r.id}`} className="text-muted-foreground">
                        {new Intl.DateTimeFormat(locale, { year: "numeric", month: "short", day: "numeric" }).format(
                          new Date(r.createdAt)
                        )}
                      </Link>
                    </td>
                    <td className="p-3">{statusBadge(r.status, (k) => t(k))}</td>
                    <td className="p-3 font-semibold">
                      {r.price != null ? formatCurrency(r.price, locale) : "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}