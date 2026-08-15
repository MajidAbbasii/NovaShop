"use client"

import * as React from "react"
import { useRouter } from "next/navigation"
import { getAdminOrders, type OrderDto, type PagedResult } from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
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
import { OrderStatusBadge } from "@/components/admin/status-badge"
import { ORDER_STATUSES, formatCurrency, formatDateShort } from "@/lib/admin-i18n"
import { SearchIcon, EyeIcon, XIcon } from "lucide-react"

const PAGE_SIZE = 10

export default function AdminOrdersPage() {
  const router = useRouter()
  const [data, setData] = React.useState<PagedResult<OrderDto> | null>(null)
  const [error, setError] = React.useState("")
  const [search, setSearch] = React.useState("")
  const [statusFilter, setStatusFilter] = React.useState("all")
  const [page, setPage] = React.useState(1)

  const fetch = React.useCallback(() => {
    getAdminOrders({
      pageNumber: page,
      pageSize: PAGE_SIZE,
      searchTerm: search || undefined,
      status: statusFilter === "all" ? undefined : statusFilter,
    })
      .then((d) => { setData(d); setError("") })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [page, search, statusFilter])

  React.useEffect(() => { fetch() }, [fetch])

  const clearFilters = () => {
    setSearch("")
    setStatusFilter("all")
    setPage(1)
  }

  const hasFilters = search !== "" || statusFilter !== "all"

  return (
    <div className="space-y-4">
      <PageHeader
        title="سفارش‌ها"
        description={`مدیریت ${data?.totalCount.toLocaleString("fa-IR") ?? ""} سفارش مشتریان`}
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "سفارش‌ها" }]}
      />

      <div className="flex flex-wrap items-center gap-2">
        <div className="relative min-w-52 flex-1">
          <SearchIcon className="absolute right-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="جستجو با شناسه سفارش..."
            className="pr-8"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
          />
        </div>
        <Select value={statusFilter} onValueChange={(v) => { setStatusFilter(v); setPage(1) }}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="همه وضعیت‌ها" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">همه وضعیت‌ها</SelectItem>
            {ORDER_STATUSES.map((s) => (
              <SelectItem key={s} value={s}>{s}</SelectItem>
            ))}
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button variant="ghost" size="sm" onClick={clearFilters}>
            <XIcon className="size-3.5" />
            پاک‌کردن فیلترها
          </Button>
        )}
      </div>

      <div className="overflow-hidden rounded-xl border bg-card">
        {error ? (
          <ErrorState message="خطا در دریافت سفارش‌ها" onRetry={fetch} />
        ) : !data ? (
          <div className="p-4"><TableSkeleton rows={6} cols={5} /></div>
        ) : !data?.items.length ? (
          <EmptyState
            title={hasFilters ? "سفارشی مطابق فیلترها یافت نشد" : "هنوز سفارشی ثبت نشده است"}
            description={hasFilters ? "فیلترها را تغییر دهید یا پاک کنید" : "سفارش‌های جدید اینجا نمایش داده می‌شوند"}
            actionLabel={hasFilters ? "پاک‌کردن فیلترها" : undefined}
            onAction={hasFilters ? clearFilters : undefined}
          />
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>سفارش</TableHead>
                  <TableHead>مشتری</TableHead>
                  <TableHead>مبلغ کل</TableHead>
                  <TableHead>وضعیت</TableHead>
                  <TableHead>تاریخ</TableHead>
                  <TableHead className="w-12" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.map((order) => (
                  <TableRow key={order.id} className="cursor-pointer" onClick={() => router.push(`/admin/orders/${order.id}`)}>
                    <TableCell className="text-sm font-semibold">#{order.id}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">کاربر #{order.userId}</TableCell>
                    <TableCell className="text-sm font-semibold tabular-nums">{formatCurrency(order.totalAmount)}</TableCell>
                    <TableCell><OrderStatusBadge status={order.status} /></TableCell>
                    <TableCell className="text-sm text-muted-foreground">{formatDateShort(order.createdAt)}</TableCell>
                    <TableCell onClick={(e) => e.stopPropagation()}>
                      <Button variant="ghost" size="icon-sm" aria-label="مشاهده سفارش" onClick={() => router.push(`/admin/orders/${order.id}`)}>
                        <EyeIcon className="size-4" />
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
    </div>
  )
}
