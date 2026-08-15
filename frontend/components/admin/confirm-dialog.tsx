'use client';

import { AlertDialog as AlertDialogPrimitive } from "radix-ui"
import { Button } from "@/components/ui/button"
import { Loader2Icon, TriangleAlertIcon } from "lucide-react"
import { useLocale } from "@/lib/locale-context"

export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel,
  cancelLabel,
  loading = false,
  destructive = true,
  onConfirm,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: string
  confirmLabel?: string
  cancelLabel?: string
  loading?: boolean
  destructive?: boolean
  onConfirm: () => void
}) {
  const { t } = useLocale()
  const confirm = confirmLabel ?? t('common.delete')
  const cancel = cancelLabel ?? t('common.cancel')
  return (
    <AlertDialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
      <AlertDialogPrimitive.Portal>
        <AlertDialogPrimitive.Overlay className="fixed inset-0 z-50 bg-black/50 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0" />
        <AlertDialogPrimitive.Content className="fixed left-1/2 top-1/2 z-50 w-[calc(100%-2rem)] max-w-md -translate-x-1/2 -translate-y-1/2 rounded-xl border bg-background p-6 shadow-lg data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95">
          <div className="flex items-start gap-3">
            <div className={destructive ? "flex size-10 shrink-0 items-center justify-center rounded-full bg-destructive/10 text-destructive" : "flex size-10 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary"}>
              <TriangleAlertIcon className="size-5" />
            </div>
            <div className="space-y-1">
              <AlertDialogPrimitive.Title className="text-base font-bold">{title}</AlertDialogPrimitive.Title>
              <AlertDialogPrimitive.Description className="text-sm text-muted-foreground">{description}</AlertDialogPrimitive.Description>
            </div>
          </div>
          <div className="mt-6 flex justify-start gap-2">
            <AlertDialogPrimitive.Cancel asChild>
              <Button variant="outline" disabled={loading}>
                {cancel}
              </Button>
            </AlertDialogPrimitive.Cancel>
            <AlertDialogPrimitive.Action asChild>
              <Button
                variant={destructive ? "destructive" : "default"}
                disabled={loading}
                onClick={(e) => {
                  e.preventDefault()
                  onConfirm()
                }}
              >
                {loading && <Loader2Icon className="size-4 animate-spin" />}
                {confirm}
              </Button>
            </AlertDialogPrimitive.Action>
          </div>
        </AlertDialogPrimitive.Content>
      </AlertDialogPrimitive.Portal>
    </AlertDialogPrimitive.Root>
  )
}
