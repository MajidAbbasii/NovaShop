'use client';

import Link from "next/link"
import { ChevronLeftIcon } from "lucide-react"
import { useLocale } from "@/lib/locale-context"

interface BreadcrumbItem {
  label: string
  href?: string
}

export function PageHeader({
  title,
  description,
  breadcrumbs = [],
  actions,
}: {
  title: string
  description?: string
  breadcrumbs?: BreadcrumbItem[]
  actions?: React.ReactNode
}) {
  const { t } = useLocale()
  return (
    <div className="mb-6 space-y-3">
      {breadcrumbs.length > 0 && (
        <nav aria-label={t('common.back')} className="flex flex-wrap items-center gap-1 text-xs text-muted-foreground">
          {breadcrumbs.map((bc, i) => (
            <span key={i} className="flex items-center gap-1">
              {i > 0 && <ChevronLeftIcon className="size-3 rtl:rotate-180" />}
              {bc.href ? (
                <Link href={bc.href} className="hover:text-foreground hover:underline">
                  {bc.label}
                </Link>
              ) : (
                <span className="font-medium text-foreground">{bc.label}</span>
              )}
            </span>
          ))}
        </nav>
      )}
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold tracking-tight">{title}</h1>
          {description && <p className="mt-1 text-sm text-muted-foreground">{description}</p>}
        </div>
        {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
      </div>
    </div>
  )
}
