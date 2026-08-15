'use client';

import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react"
import { useLocale } from "@/lib/locale-context"
import { formatNumber } from "@/lib/formatters"

export function Pagination({
  page,
  totalPages,
  totalCount,
  pageSize,
  onChange,
}: {
  page: number
  totalPages: number
  totalCount: number
  pageSize: number
  onChange: (page: number) => void
}) {
  const { t, tva, locale } = useLocale()
  if (totalPages <= 1) return null

  const pages: (number | "…")[] = []
  for (let i = 1; i <= totalPages; i++) {
    if (i === 1 || i === totalPages || Math.abs(i - page) <= 1) {
      pages.push(i)
    } else if (pages[pages.length - 1] !== "…") {
      pages.push("…")
    }
  }

  const from = (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t px-4 py-3">
      <p className="text-xs text-muted-foreground">
        {tva('pagination.showing', {
          from: formatNumber(from, locale),
          to: formatNumber(to, locale),
          total: formatNumber(totalCount, locale),
        })}
      </p>
      <div className="flex items-center gap-1">
        <Button
          variant="outline"
          size="icon-sm"
          disabled={page <= 1}
          onClick={() => onChange(page - 1)}
          aria-label={t('pagination.prevAria')}
        >
          <ChevronRightIcon className="size-4 rtl:rotate-180" />
        </Button>
        {pages.map((p, i) =>
          p === "…" ? (
            <span key={`e${i}`} className="px-1.5 text-xs text-muted-foreground">
              …
            </span>
          ) : (
            <Button
              key={p}
              variant={p === page ? "default" : "outline"}
              size="icon-sm"
              onClick={() => onChange(p)}
              aria-current={p === page ? "page" : undefined}
              className={cn(p === page ? "" : "text-foreground")}
            >
              {formatNumber(p, locale)}
            </Button>
          )
        )}
        <Button
          variant="outline"
          size="icon-sm"
          disabled={page >= totalPages}
          onClick={() => onChange(page + 1)}
          aria-label={t('pagination.nextAria')}
        >
          <ChevronLeftIcon className="size-4 rtl:rotate-180" />
        </Button>
      </div>
    </div>
  )
}
