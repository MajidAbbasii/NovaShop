import { Button } from "@/components/ui/button"
import { PackageOpenIcon, RefreshCwIcon, CloudOffIcon } from "lucide-react"
import { useLocale } from "@/lib/locale-context"

export function EmptyState({
  title,
  description,
  actionLabel,
  onAction,
}: {
  title?: string
  description?: string
  actionLabel?: string
  onAction?: () => void
}) {
  const { t } = useLocale()
  return (
    <div className="flex flex-col items-center justify-center gap-3 px-4 py-14 text-center">
      <div className="flex size-12 items-center justify-center rounded-2xl bg-muted text-muted-foreground">
        <PackageOpenIcon className="size-6" />
      </div>
      <div className="space-y-1">
        <p className="text-sm font-semibold">{title ?? t('error.notFound')}</p>
        {description && <p className="text-xs text-muted-foreground">{description}</p>}
      </div>
      {actionLabel && onAction && (
        <Button variant="outline" size="sm" onClick={onAction}>
          {actionLabel}
        </Button>
      )}
    </div>
  )
}

export function ErrorState({
  message = "",
  onRetry,
}: {
  message?: string
  onRetry?: () => void
}) {
  const { t } = useLocale()
  return (
    <div className="flex flex-col items-center justify-center gap-3 px-4 py-14 text-center">
      <div className="flex size-12 items-center justify-center rounded-2xl bg-destructive/10 text-destructive">
        <CloudOffIcon className="size-6" />
      </div>
      <div className="space-y-1">
        <p className="text-sm font-semibold">{message || t('error.loadFailed')}</p>
        <p className="text-xs text-muted-foreground">{t('error.networkHint')}</p>
      </div>
      {onRetry && (
        <Button variant="outline" size="sm" onClick={onRetry}>
          <RefreshCwIcon className="size-3.5" />
          {t('common.retry')}
        </Button>
      )}
    </div>
  )
}

export function InlineError({ message }: { message: string }) {
  return (
    <p role="alert" className="rounded-lg bg-destructive/10 px-3 py-2 text-sm text-destructive">
      {message}
    </p>
  )
}
