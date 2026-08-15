import { cn } from "@/lib/utils"
import { useLocale } from "@/lib/locale-context"

export function DataTableSkeleton({ rows = 5, cols = 4 }: { rows?: number; cols?: number }) {
  const { t } = useLocale()
  return (
    <div className="space-y-3" aria-busy="true" aria-label={t('common.loading')}>
      {Array.from({ length: rows }).map((_, r) => (
        <div key={r} className="flex gap-3">
          {Array.from({ length: cols }).map((_, c) => (
            <div
              key={c}
              className={cn(
                "h-4 animate-pulse rounded bg-muted",
                c === 0 ? "w-1/4" : "flex-1"
              )}
            />
          ))}
        </div>
      ))}
    </div>
  )
}

export function CardSkeleton({ count = 4 }: { count?: number }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="h-28 animate-pulse rounded-xl border bg-card" />
      ))}
    </div>
  )
}

export function TableSkeleton({ rows = 6, cols = 5 }: { rows?: number; cols?: number }) {
  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <div className="flex gap-3 border-b p-4">
        {Array.from({ length: cols }).map((_, c) => (
          <div key={c} className={cn("h-3.5 animate-pulse rounded bg-muted", c === 0 ? "w-24" : "flex-1")} />
        ))}
      </div>
      {Array.from({ length: rows }).map((_, r) => (
        <div key={r} className="flex gap-3 border-b p-4 last:border-0">
          {Array.from({ length: cols }).map((_, c) => (
            <div key={c} className={cn("h-3.5 animate-pulse rounded bg-muted/60", c === 0 ? "w-28" : "flex-1")} />
          ))}
        </div>
      ))}
    </div>
  )
}
