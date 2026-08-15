"use client"

import * as React from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import {
  getAdminProducts,
  getAdminCategories,
  deleteProduct,
  type AdminProduct,
  type PagedResult,
  type AdminCategory,
} from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { PageHeader } from "@/components/admin/page-header"
import { Pagination } from "@/components/admin/pagination"
import { TableSkeleton } from "@/components/admin/skeletons"
import { EmptyState, ErrorState } from "@/components/admin/states"
import { ConfirmDialog } from "@/components/admin/confirm-dialog"
import { StockBadge } from "@/components/admin/status-badge"
import { formatCurrency, formatDateShort } from "@/lib/admin-i18n"
import {
  SearchIcon, PlusIcon, MoreHorizontalIcon, EyeIcon, PencilIcon, Trash2Icon, XIcon, ImageOffIcon,
} from "lucide-react"

const PAGE_SIZE = 10

export default function AdminProductsPage() {
  const router = useRouter()
  const [data, setData] = React.useState<PagedResult<AdminProduct> | null>(null)
  const [categories, setCategories] = React.useState<AdminCategory[]>([])
  const [error, setError] = React.useState("")
  const [search, setSearch] = React.useState("")
  const [categoryFilter, setCategoryFilter] = React.useState("all")
  const [availFilter, setAvailFilter] = React.useState("all")
  const [page, setPage] = React.useState(1)
  const [deleteTarget, setDeleteTarget] = React.useState<AdminProduct | null>(null)
  const [deleting, setDeleting] = React.useState(false)

  React.useEffect(() => {
    getAdminCategories().then((r) => setCategories(r.items)).catch(() => {})
  }, [])

  const categoryName = React.useCallback(
    (id?: number) => categories.find((c) => c.id === id)?.name,
    [categories]
  )

  const fetch = React.useCallback(() => {
    getAdminProducts({
      pageNumber: page,
      pageSize: PAGE_SIZE,
      searchTerm: search || undefined,
      categoryId: categoryFilter === "all" ? undefined : Number(categoryFilter),
      onlyAvailable: availFilter === "all" ? undefined : availFilter === "available",
    })
      .then((d) => { setData(d); setError("") })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [page, search, categoryFilter, availFilter])

  React.useEffect(() => { fetch() }, [fetch])

  const clearFilters = () => {
    setSearch("")
    setCategoryFilter("all")
    setAvailFilter("all")
    setPage(1)
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await deleteProduct(deleteTarget.id)
      toast({ title: "محصول حذف شد", description: deleteTarget.name })
      setDeleteTarget(null)
      fetch()
    } catch (e: unknown) {
      toast({
        title: "خطا در حذف",
        description: e instanceof Error ? e.message : "خطا",
        variant: "destructive",
      })
    } finally {
      setDeleting(false)
    }
  }

  const hasFilters = search !== "" || categoryFilter !== "all" || availFilter !== "all"

  return (
    <div className="space-y-4">
      <PageHeader
        title="محصولات"
        description={`مدیریت ${data?.totalCount.toLocaleString("fa-IR") ?? ""} محصول فروشگاه`}
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "محصولات" }]}
        actions={
          <Button onClick={() => router.push("/admin/products/new")}>
            <PlusIcon className="size-4" />
            محصول جدید
          </Button>
        }
      />

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-2">
        <div className="relative min-w-52 flex-1">
          <SearchIcon className="absolute right-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="جستجوی محصول..."
            className="pr-8"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
          />
        </div>
        <Select value={categoryFilter} onValueChange={(v) => { setCategoryFilter(v); setPage(1) }}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder="همه دسته‌بندی‌ها" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">همه دسته‌بندی‌ها</SelectItem>
            {categories.map((c) => (
              <SelectItem key={c.id} value={String(c.id)}>{c.name}</SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={availFilter} onValueChange={(v) => { setAvailFilter(v); setPage(1) }}>
          <SelectTrigger className="w-36">
            <SelectValue placeholder="وضعیت موجودی" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">همه وضعیت‌ها</SelectItem>
            <SelectItem value="available">موجود</SelectItem>
            <SelectItem value="out">ناموجود</SelectItem>
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button variant="ghost" size="sm" onClick={clearFilters}>
            <XIcon className="size-3.5" />
            پاک‌کردن فیلترها
          </Button>
        )}
      </div>

      {/* Table */}
      <div className="overflow-hidden rounded-xl border bg-card">
        {error ? (
          <ErrorState message="خطا در دریافت محصولات" onRetry={fetch} />
        ) : !data ? (
          <div className="p-4"><TableSkeleton rows={6} cols={5} /></div>
        ) : !data?.items.length ? (
          <EmptyState
            title={hasFilters ? "محصولی مطابق فیلترها یافت نشد" : "هنوز محصولی ثبت نشده است"}
            description={hasFilters ? "فیلترها را تغییر دهید یا پاک کنید" : "اولین محصول را ایجاد کنید"}
            actionLabel={hasFilters ? "پاک‌کردن فیلترها" : "ایجاد محصول"}
            onAction={hasFilters ? clearFilters : () => router.push("/admin/products/new")}
          />
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-16">تصویر</TableHead>
                  <TableHead>نام محصول</TableHead>
                  <TableHead>دسته‌بندی</TableHead>
                  <TableHead>قیمت</TableHead>
                  <TableHead>موجودی</TableHead>
                  <TableHead>امتیاز</TableHead>
                  <TableHead className="w-12" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.map((p) => (
                  <TableRow key={p.id} className="cursor-pointer" onClick={() => router.push(`/admin/products/${p.id}`)}>
                    <TableCell>
                      {p.imageUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img
                          src={p.imageUrl}
                          alt={p.name}
                          className="size-10 rounded-lg object-cover"
                          loading="lazy"
                        />
                      ) : (
                        <div className="flex size-10 items-center justify-center rounded-lg bg-muted">
                          <ImageOffIcon className="size-4 text-muted-foreground" />
                        </div>
                      )}
                    </TableCell>
                    <TableCell>
                      <p className="text-sm font-medium">{p.name}</p>
                      <p className="text-xs text-muted-foreground">#{p.id}</p>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {categoryName(p.categoryId) ?? p.category?.name ?? "-"}
                    </TableCell>
                    <TableCell>
                      <p className="text-sm font-semibold tabular-nums">{formatCurrency(p.price)}</p>
                      {p.originalPrice && p.originalPrice > p.price && (
                        <p className="text-xs text-muted-foreground line-through">
                          {formatCurrency(p.originalPrice)}
                        </p>
                      )}
                    </TableCell>
                    <TableCell><StockBadge stock={p.stock} /></TableCell>
                    <TableCell className="text-sm tabular-nums">
                      {p.rating.toLocaleString("fa-IR", { maximumFractionDigits: 1 })}
                    </TableCell>
                    <TableCell onClick={(e) => e.stopPropagation()}>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="icon-sm" aria-label="عملیات">
                            <MoreHorizontalIcon className="size-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end" className="w-40">
                          <DropdownMenuItem onClick={() => router.push(`/admin/products/${p.id}`)}>
                            <EyeIcon className="size-4" />
                            مشاهده
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => router.push(`/admin/products/${p.id}?edit=1`)}>
                            <PencilIcon className="size-4" />
                            ویرایش
                          </DropdownMenuItem>
                          <DropdownMenuItem
                            className="text-destructive focus:text-destructive"
                            onClick={() => setDeleteTarget(p)}
                          >
                            <Trash2Icon className="size-4" />
                            حذف
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
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
        title="حذف محصول"
        description={`آیا از حذف «${deleteTarget?.name}» مطمئن هستید؟ این عملیات قابل بازگشت نیست.`}
        confirmLabel="حذف محصول"
        loading={deleting}
        onConfirm={handleDelete}
      />
    </div>
  )
}
