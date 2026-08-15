"use client"

import * as React from "react"
import {
  getAdminDiscounts,
  createDiscount,
  updateDiscount,
  deleteDiscount,
  getAdminCategories,
  getAdminProducts,
  type AdminDiscount,
  type PagedResult,
  type AdminCategory,
  type AdminProduct,
} from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from "@/components/ui/dialog"
import { PageHeader } from "@/components/admin/page-header"
import { Pagination } from "@/components/admin/pagination"
import { TableSkeleton } from "@/components/admin/skeletons"
import { EmptyState, ErrorState } from "@/components/admin/states"
import { ConfirmDialog } from "@/components/admin/confirm-dialog"
import { DISCOUNT_TYPE_LABELS, formatCurrency, formatDateShort } from "@/lib/admin-i18n"
import { PlusIcon, PencilIcon, Trash2Icon, Loader2Icon, CopyIcon } from "lucide-react"

const PAGE_SIZE = 10

type DiscountForm = {
  code: string
  type: string
  value: string
  startDate: string
  endDate: string
  usageLimit: string
  minOrderAmount: string
  isActive: boolean
}

const EMPTY_FORM: DiscountForm = {
  code: "",
  type: "Percentage",
  value: "",
  startDate: "",
  endDate: "",
  usageLimit: "1",
  minOrderAmount: "0",
  isActive: true,
}

function toLocalInput(d: string): string {
  const dt = new Date(d)
  if (isNaN(dt.getTime())) return ""
  const pad = (n: number) => String(n).padStart(2, "0")
  return `${dt.getFullYear()}-${pad(dt.getMonth() + 1)}-${pad(dt.getDate())}T${pad(dt.getHours())}:${pad(dt.getMinutes())}`
}

export default function AdminDiscountsPage() {
  const [data, setData] = React.useState<PagedResult<AdminDiscount> | null>(null)
  const [categories, setCategories] = React.useState<AdminCategory[]>([])
  const [products, setProducts] = React.useState<AdminProduct[]>([])
  const [error, setError] = React.useState("")
  const [page, setPage] = React.useState(1)

  const [dialogOpen, setDialogOpen] = React.useState(false)
  const [editing, setEditing] = React.useState<AdminDiscount | null>(null)
  const [form, setForm] = React.useState<DiscountForm>(EMPTY_FORM)
  const [formError, setFormError] = React.useState("")
  const [saving, setSaving] = React.useState(false)

  const [deleteTarget, setDeleteTarget] = React.useState<AdminDiscount | null>(null)
  const [deleting, setDeleting] = React.useState(false)

  const fetch = React.useCallback(() => {
    getAdminDiscounts({ pageNumber: page, pageSize: PAGE_SIZE })
      .then((d) => { setData(d); setError("") })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [page])

  React.useEffect(() => { fetch() }, [fetch])

  React.useEffect(() => {
    getAdminCategories().then((r) => setCategories(r.items)).catch(() => {})
    getAdminProducts({ pageSize: 100 }).then((r) => setProducts(r.items)).catch(() => {})
  }, [])

  const openCreate = () => {
    setEditing(null)
    setForm(EMPTY_FORM)
    setFormError("")
    setDialogOpen(true)
  }

  const openEdit = (d: AdminDiscount) => {
    setEditing(d)
    setForm({
      code: d.code,
      type: d.type,
      value: String(d.value),
      startDate: toLocalInput(d.startDate),
      endDate: toLocalInput(d.endDate),
      usageLimit: String(d.usageLimit),
      minOrderAmount: String(d.minOrderAmount),
      isActive: d.isActive,
    })
    setFormError("")
    setDialogOpen(true)
  }

  const validate = (): boolean => {
    const f = form
    if (!f.code.trim()) { setFormError("کد تخفیف اجباری است"); return false }
    const v = Number(f.value)
    if (isNaN(v) || v <= 0) { setFormError("مقدار تخفیف باید بزرگ‌تر از صفر باشد"); return false }
    if (f.type === "Percentage" && v > 100) { setFormError("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد"); return false }
    if (!f.startDate || !f.endDate) { setFormError("تاریخ شروع و پایان اجباری است"); return false }
    if (new Date(f.startDate) >= new Date(f.endDate)) { setFormError("تاریخ شروع باید قبل از تاریخ پایان باشد"); return false }
    const ul = Number(f.usageLimit)
    if (isNaN(ul) || ul <= 0) { setFormError("محدودیت استفاده باید بزرگ‌تر از صفر باشد"); return false }
    return true
  }

  const handleSave = async () => {
    if (!validate()) return
    setSaving(true)
    setFormError("")
    const payload = {
      code: form.code.trim(),
      type: form.type,
      value: Number(form.value),
      startDate: new Date(form.startDate).toISOString(),
      endDate: new Date(form.endDate).toISOString(),
      usageLimit: Number(form.usageLimit),
      minOrderAmount: Number(form.minOrderAmount || "0"),
      isActive: form.isActive,
    }
    try {
      if (editing) {
        await updateDiscount(editing.id, payload)
        toast({ title: "تخفیف به‌روزرسانی شد" })
      } else {
        await createDiscount(payload)
        toast({ title: "تخفیف ایجاد شد" })
      }
      setDialogOpen(false)
      fetch()
    } catch (e: unknown) {
      setFormError(e instanceof Error ? e.message : "خطا در ذخیره")
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await deleteDiscount(deleteTarget.id)
      toast({ title: "تخفیف حذف شد" })
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

  const copyCode = (code: string) => {
    navigator.clipboard?.writeText(code).then(
      () => toast({ title: "کد کپی شد", description: code }),
      () => {}
    )
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title="تخفیف‌ها"
        description={`مدیریت ${data?.totalCount.toLocaleString("fa-IR") ?? ""} کد تخفیف`}
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "تخفیف‌ها" }]}
        actions={
          <Button onClick={openCreate}>
            <PlusIcon className="size-4" />
            تخفیف جدید
          </Button>
        }
      />

      <div className="overflow-hidden rounded-xl border bg-card">
        {error ? (
          <ErrorState message="خطا در دریافت تخفیف‌ها" onRetry={fetch} />
        ) : !data ? (
          <div className="p-4"><TableSkeleton rows={6} cols={5} /></div>
        ) : !data?.items.length ? (
          <EmptyState
            title="هنوز کد تخفیفی ثبت نشده است"
            description="اولین کد تخفیف را ایجاد کنید"
            actionLabel="ایجاد تخفیف"
            onAction={openCreate}
          />
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>کد</TableHead>
                  <TableHead>نوع</TableHead>
                  <TableHead>مقدار</TableHead>
                  <TableHead>دوره اعتبار</TableHead>
                  <TableHead>استفاده</TableHead>
                  <TableHead>وضعیت</TableHead>
                  <TableHead className="w-24">عملیات</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.map((d) => (
                  <TableRow key={d.id}>
                    <TableCell>
                      <button
                        className="flex items-center gap-1.5 rounded-md bg-muted px-2 py-0.5 font-mono text-sm font-semibold hover:bg-muted/70"
                        onClick={() => copyCode(d.code)}
                        title="کپی کد"
                      >
                        {d.code}
                        <CopyIcon className="size-3 text-muted-foreground" />
                      </button>
                    </TableCell>
                    <TableCell className="text-sm">{DISCOUNT_TYPE_LABELS[d.type] || d.type}</TableCell>
                    <TableCell className="text-sm font-semibold tabular-nums">
                      {d.type === "Percentage" ? `${d.value.toLocaleString("fa-IR")}٪` : formatCurrency(d.value)}
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground">
                      <div>{formatDateShort(d.startDate)}</div>
                      <div>تا {formatDateShort(d.endDate)}</div>
                    </TableCell>
                    <TableCell className="text-sm tabular-nums text-muted-foreground">
                      {d.usedCount.toLocaleString("fa-IR")} / {d.usageLimit.toLocaleString("fa-IR")}
                    </TableCell>
                    <TableCell>
                      <Badge variant={d.isActive ? "default" : "secondary"}>
                        {d.isActive ? "فعال" : "غیرفعال"}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button variant="ghost" size="icon-sm" onClick={() => openEdit(d)} aria-label="ویرایش">
                          <PencilIcon className="size-4" />
                        </Button>
                        <Button variant="ghost" size="icon-sm" className="text-destructive hover:text-destructive" onClick={() => setDeleteTarget(d)} aria-label="حذف">
                          <Trash2Icon className="size-4" />
                        </Button>
                      </div>
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

      {/* Create/Edit dialog */}
      <Dialog open={dialogOpen} onOpenChange={(o) => !o && setDialogOpen(false)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{editing ? "ویرایش تخفیف" : "تخفیف جدید"}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="d-code">
                  کد تخفیف <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="d-code"
                  dir="ltr"
                  value={form.code}
                  onChange={(e) => setForm({ ...form, code: e.target.value.toUpperCase() })}
                  placeholder="SUMMER10"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="d-type">نوع تخفیف</Label>
                <Select value={form.type} onValueChange={(v) => setForm({ ...form, type: v })}>
                  <SelectTrigger id="d-type" className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Percentage">درصدی</SelectItem>
                    <SelectItem value="Fixed">مبلغ ثابت</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="d-value">
                  {form.type === "Percentage" ? "درصد تخفیف" : "مبلغ تخفیف (تومان)"} <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="d-value"
                  type="number"
                  step="0.01"
                  min="0"
                  value={form.value}
                  onChange={(e) => setForm({ ...form, value: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="d-limit">
                  محدودیت استفاده <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="d-limit"
                  type="number"
                  min="1"
                  value={form.usageLimit}
                  onChange={(e) => setForm({ ...form, usageLimit: e.target.value })}
                />
              </div>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="d-start">
                  شروع <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="d-start"
                  type="datetime-local"
                  value={form.startDate}
                  onChange={(e) => setForm({ ...form, startDate: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="d-end">
                  پایان <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="d-end"
                  type="datetime-local"
                  value={form.endDate}
                  onChange={(e) => setForm({ ...form, endDate: e.target.value })}
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="d-min">حداقل مبلغ سفارش (تومان)</Label>
              <Input
                id="d-min"
                type="number"
                step="0.01"
                min="0"
                value={form.minOrderAmount}
                onChange={(e) => setForm({ ...form, minOrderAmount: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label>وضعیت</Label>
              <Select value={form.isActive ? "1" : "0"} onValueChange={(v) => setForm({ ...form, isActive: v === "1" })}>
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">فعال</SelectItem>
                  <SelectItem value="0">غیرفعال</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {formError && <p role="alert" className="text-sm text-destructive">{formError}</p>}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogOpen(false)} disabled={saving}>
              انصراف
            </Button>
            <Button onClick={handleSave} disabled={saving}>
              {saving && <Loader2Icon className="size-4 animate-spin" />}
              {saving ? "در حال ذخیره..." : "ذخیره"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="حذف تخفیف"
        description={`آیا از حذف کد تخفیف «${deleteTarget?.code}» مطمئن هستید؟ این عملیات قابل بازگشت نیست.`}
        confirmLabel="حذف"
        loading={deleting}
        onConfirm={handleDelete}
      />
    </div>
  )
}
