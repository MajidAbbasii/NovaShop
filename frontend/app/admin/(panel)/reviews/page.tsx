"use client"

import * as React from "react"
import {
  getAdminReviews,
  deleteAdminReview,
  type AdminReview,
  type PagedResult,
} from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import { PageHeader } from "@/components/admin/page-header"
import { Pagination } from "@/components/admin/pagination"
import { TableSkeleton } from "@/components/admin/skeletons"
import { EmptyState, ErrorState } from "@/components/admin/states"
import { ConfirmDialog } from "@/components/admin/confirm-dialog"
import { formatDateShort } from "@/lib/admin-i18n"
import { useLocale } from "@/lib/locale-context"
import { Loader2Icon, Trash2Icon } from "lucide-react"

const PAGE_SIZE = 10
const RATINGS = [1, 2, 3, 4, 5]

export default function AdminReviewsPage() {
  const { t, tva } = useLocale()
  const [data, setData] = React.useState<PagedResult<AdminReview> | null>(null)
  const [error, setError] = React.useState("")
  const [page, setPage] = React.useState(1)
  const [rating, setRating] = React.useState("all")

  const [deleteTarget, setDeleteTarget] = React.useState<AdminReview | null>(null)
  const [deleting, setDeleting] = React.useState(false)

  const fetch = React.useCallback(() => {
    getAdminReviews({
      pageNumber: page,
      pageSize: PAGE_SIZE,
      rating: rating === "all" ? undefined : Number(rating),
    })
      .then((d) => { setData(d); setError("") })
      .catch(() => setError(t('admin.reviews.loadError')))
  }, [page, rating, t])

  React.useEffect(() => { fetch() }, [fetch])

  const handleDelete = async () => {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await deleteAdminReview(deleteTarget.id)
      toast({ title: t('admin.reviews.deleted') })
      setDeleteTarget(null)
      fetch()
    } catch (e: unknown) {
      toast({
        title: t('admin.reviews.deleteError'),
        description: e instanceof Error ? e.message : t('admin.reviews.deleteError'),
        variant: "destructive",
      })
    } finally {
      setDeleting(false)
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title={t('admin.reviews.title')}
        description={tva('admin.reviews.manage', { count: (data?.totalCount ?? 0).toLocaleString("fa-IR") })}
        breadcrumbs={[{ label: t('admin.dashboard'), href: "/admin" }, { label: t('admin.reviews.title') }]}
      />

      <div className="flex items-center gap-2">
        <Select value={rating} onValueChange={(v) => { setRating(v); setPage(1) }}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder={t('admin.reviews.allRatings')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">{t('admin.reviews.allRatings')}</SelectItem>
            {RATINGS.map((r) => (
              <SelectItem key={r} value={String(r)}>
                {tva('admin.reviews.ratingStar', { rating: r.toLocaleString("fa-IR") })}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="overflow-hidden rounded-xl border bg-card">
        {error ? (
          <ErrorState message={error} onRetry={fetch} />
        ) : !data ? (
          <div className="p-4"><TableSkeleton rows={6} cols={5} /></div>
        ) : !data?.items.length ? (
          <EmptyState
            title={t('admin.reviews.notFound')}
            description=""
          />
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('admin.reviews.product')}</TableHead>
                  <TableHead>{t('admin.reviews.customer')}</TableHead>
                  <TableHead>{t('admin.reviews.rating')}</TableHead>
                  <TableHead>{t('admin.reviews.comment')}</TableHead>
                  <TableHead>{t('admin.reviews.date')}</TableHead>
                  <TableHead className="w-16"> </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.map((r) => (
                  <TableRow key={r.id}>
                    <TableCell className="text-sm font-medium">
                      <a href={`/product/${r.productId}`} className="hover:underline">
                        {r.productName}
                      </a>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {r.userName || `#${r.userId}`}
                    </TableCell>
                    <TableCell>
                      <Badge variant="default">
                        ⭐ {r.rating.toLocaleString("fa-IR")}
                      </Badge>
                    </TableCell>
                    <TableCell className="max-w-xs text-sm text-muted-foreground">
                      <span className="line-clamp-2">{r.comment || "—"}</span>
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground whitespace-nowrap">
                      {formatDateShort(r.createdAt)}
                    </TableCell>
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        className="text-destructive hover:text-destructive"
                        onClick={() => setDeleteTarget(r)}
                        aria-label={t('admin.reviews.deleteTitle')}
                      >
                        <Trash2Icon className="size-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
        {data && data.items.length > 0 && (
          <Pagination page={page} totalPages={data.totalPages} totalCount={data.totalCount} pageSize={PAGE_SIZE} onChange={setPage} />
        )}
      </div>

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title={t('admin.reviews.deleteTitle')}
        description={t('admin.reviews.deleteConfirm')}
        confirmLabel={t('admin.reviews.deleteTitle')}
        loading={deleting}
        onConfirm={handleDelete}
      />
    </div>
  )
}