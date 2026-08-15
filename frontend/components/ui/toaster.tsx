"use client"

import * as React from "react"
import { useToast } from "@/hooks/use-toast"
import { XIcon } from "lucide-react"
import { cn } from "@/lib/utils"

function Toaster() {
  const { toasts, dismiss } = useToast()

  return (
    <div
      className="fixed top-0 right-0 z-[100] flex max-h-screen w-full flex-col-reverse gap-2 p-4 sm:max-w-[420px]"
    >
      {toasts.map((t) => (
        <div
          key={t.id}
          className={cn(
            "pointer-events-auto flex items-start gap-3 rounded-lg border bg-background p-4 shadow-lg transition-all animate-in slide-in-from-right-full",
            t.variant === "destructive" && "border-destructive/30 bg-destructive/5"
          )}
        >
          <div className="flex-1">
            {t.title && <p className="text-sm font-semibold">{t.title}</p>}
            {t.description && <p className="text-sm text-muted-foreground">{t.description}</p>}
          </div>
          <button
            onClick={() => dismiss(t.id)}
            className="shrink-0 rounded-md p-1 transition-colors hover:bg-muted"
          >
            <XIcon className="size-4" />
          </button>
        </div>
      ))}
    </div>
  )
}

export { Toaster }
