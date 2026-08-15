import { Card, CardContent } from "@/components/ui/card"
import { cn } from "@/lib/utils"

export function StatCard({
  label,
  value,
  icon: Icon,
  hint,
  className,
}: {
  label: string
  value: string | number
  icon?: React.ComponentType<{ className?: string }>
  hint?: string
  className?: string
}) {
  return (
    <Card className={cn("overflow-hidden", className)}>
      <CardContent className="p-5">
        <div className="flex items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="text-xs font-medium text-muted-foreground">{label}</p>
            <p className="mt-1.5 text-2xl font-bold tabular-nums">{value}</p>
            {hint && <p className="mt-1 text-[11px] text-muted-foreground">{hint}</p>}
          </div>
          {Icon && (
            <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <Icon className="size-4.5" />
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  )
}
