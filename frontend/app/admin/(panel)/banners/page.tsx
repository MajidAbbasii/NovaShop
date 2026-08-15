"use client"

import * as React from "react"
import {
  getAdminBanners,
  createBanner,
  updateBanner,
  deleteBanner,
  type BannerDto,
} from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from "@/components/ui/dialog"
import { PageHeader } from "@/components/admin/page-header"
import { TableSkeleton } from "@/components/admin/skeletons"
import { EmptyState, ErrorState } from "@/components/admin/states"
import { ConfirmDialog } from "@/components/admin/confirm-dialog"
import { ImageUploader } from "@/components/admin/image-uploader"
import {
  PlusIcon, PencilIcon, Trash2Icon, Loader2Icon, ImageOffIcon,
} from "lucide-react"

type BannerForm = {
  title: string
  subtitle: string
  imageUrl: string
  linkUrl: string
  isActive: boolean
  sortOrder: number
}

const EMPTY_FORM: BannerForm = {
  title: "",
  subtitle: "",
  imageUrl: "",
  linkUrl: "/products",
  isActive: true,
  sortOrder: 0,
}

export default function AdminBannersPage() {
  const [data, setData] = React.useState<BannerDto[] | null>(null)
  const [error, setError] = React.useState("")
  const [dialogOpen, setDialogOpen] = React.useState(false)
  const [editing, setEditing] = React.useState<BannerDto | null>(null)
  const [form, setForm] = React.useState<BannerForm>(EMPTY_FORM)
  const [formError, setFormError] = React.useState("")
  const [saving, setSaving] = React.useState(false)
  const [deleteTarget, setDeleteTarget] = React.useState<BannerDto | null>(null)
  const [deleting, setDeleting] = React.useState(false)

  const fetch = React.useCallback(() => {
    getAdminBanners()
      .then((r) => { setData(r.items); setError("") })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [])

  React.useEffect(() => { fetch() }, [fetch])

  const openCreate = () => {
    setEditing(null)
    setForm(EMPTY_FORM)
    setFormError("")
    setDialogOpen(true)
  }

  const openEdit = (b: BannerDto) => {
    setEditing(b)
    setForm({
      title: b.title,
      subtitle: b.subtitle ?? "",
      imageUrl: b.imageUrl ?? "",
      linkUrl: b.linkUrl ?? "",
      isActive: b.isActive,
      sortOrder: b.sortOrder ?? 0,
    })
    setFormError("")
    setDialogOpen(true)
  }

  const handleSave = async () => {
    if (form.title.trim().length < 2) {
      setFormError("عنوان بنر باید حداقل ۲ کاراکتر باشد")
      return
    }
    setSaving(true)
    setFormError("")
    try {
      const payload = {
        title: form.title.trim(),
        subtitle: form.subtitle.trim() || undefined,
        imageUrl: form.imageUrl.trim() || undefined,
        linkUrl: form.linkUrl.trim() || undefined,
        isActive: form.isActive,
        sortOrder: form.sortOrder,
      }
      if (editing) {
        await updateBanner(editing.id, payload)
        toast({ title: "بنر به‌روزرسانی شد" })
      } else {
        await createBanner(payload)
        toast({ title: "بنر ایجاد شد" })
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
      await deleteBanner(deleteTarget.id)
      toast({ title: "بنر حذف شد" })
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

  return (
    <div className="space-y-4">
      <PageHeader
        title="بنرها"
        description={`مدیریت ${data?.length.toLocaleString("fa-IR") ?? ""} بنر نمایشی فروشگاه`}
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "بنرها" }]}
        actions={
          <Button onClick={openCreate}>
            <PlusIcon className="size-4" />
            بنر جدید
          </Button>
        }
      />

      <div className="overflow-hidden rounded-xl border bg-card">
        {error ? (
          <ErrorState message="خطا در دریافت بنرها" onRetry={fetch} />
        ) : !data ? (
          <div className="p-4"><TableSkeleton rows={4} cols={5} /></div>
        ) : !data?.length ? (
          <EmptyState
            title="هنوز بنری ثبت نشده است"
            description="اولین بنر نمایشی فروشگاه را ایجاد کنید"
            actionLabel="ایجاد بنر"
            onAction={openCreate}
          />
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-16">تصویر</TableHead>
                  <TableHead>عنوان</TableHead>
                  <TableHead>زیرعنوان</TableHead>
                  <TableHead>لینک</TableHead>
                  <TableHead>ترتیب</TableHead>
                  <TableHead>وضعیت</TableHead>
                  <TableHead className="w-24">عملیات</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.map((b) => (
                  <TableRow key={b.id}>
                    <TableCell>
                      {b.imageUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img src={b.imageUrl} alt={b.title} className="size-14 rounded-lg object-cover" loading="lazy" />
                      ) : (
                        <div className="flex size-14 items-center justify-center rounded-lg bg-muted">
                          <ImageOffIcon className="size-4 text-muted-foreground" />
                        </div>
                      )}
                    </TableCell>
                    <TableCell className="text-sm font-medium">{b.title}</TableCell>
                    <TableCell className="max-w-xs truncate text-sm text-muted-foreground">{b.subtitle || "—"}</TableCell>
                    <TableCell className="max-w-[180px] truncate font-mono text-xs text-muted-foreground" dir="ltr">{b.linkUrl || "—"}</TableCell>
                    <TableCell className="text-sm tabular-nums text-muted-foreground">{b.sortOrder.toLocaleString("fa-IR")}</TableCell>
                    <TableCell>
                      <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${b.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"}`}>
                        {b.isActive ? "فعال" : "غیرفعال"}
                      </span>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button variant="ghost" size="icon-sm" onClick={() => openEdit(b)} aria-label="ویرایش">
                          <PencilIcon className="size-4" />
                        </Button>
                        <Button variant="ghost" size="icon-sm" className="text-destructive hover:text-destructive" onClick={() => setDeleteTarget(b)} aria-label="حذف">
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
      </div>

      {/* Create/Edit dialog */}
      <Dialog open={dialogOpen} onOpenChange={(o) => !o && setDialogOpen(false)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{editing ? "ویرایش بنر" : "بنر جدید"}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="b-title">
                عنوان بنر <span className="text-destructive">*</span>
              </Label>
              <Input
                id="b-title"
                value={form.title}
                onChange={(e) => setForm({ ...form, title: e.target.value })}
                placeholder="مثلاً فروش تابستانه"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="b-subtitle">زیرعنوان</Label>
              <Input
                id="b-subtitle"
                value={form.subtitle}
                onChange={(e) => setForm({ ...form, subtitle: e.target.value })}
                placeholder="مثلاً تخفیف تا ۴۰٪"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="b-link">لینک مقصد</Label>
              <Input
                id="b-link"
                dir="ltr"
                value={form.linkUrl}
                onChange={(e) => setForm({ ...form, linkUrl: e.target.value })}
                placeholder="/products"
              />
            </div>
            <div className="space-y-2">
              <Label>تصویر بنر (پیشنهادی ۱۶۰۰×۵۰۰)</Label>
              <ImageUploader
                value={form.imageUrl}
                onUpload={async (url) => {
                  setForm(prev => ({ ...prev, imageUrl: url }))
                  return url
                }}
                onRemove={async () => {
                  setForm(prev => ({ ...prev, imageUrl: "" }))
                }}
                folder="banners"
                maxSizeMB={5}
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="b-order">ترتیب نمایش</Label>
                <Input
                  id="b-order"
                  type="number"
                  value={form.sortOrder}
                  onChange={(e) => setForm({ ...form, sortOrder: Number(e.target.value) || 0 })}
                />
              </div>
              <div className="flex items-end pb-1">
                <label className="flex cursor-pointer items-center gap-2 text-sm font-medium">
                  <input
                    id="b-active"
                    type="checkbox"
                    checked={form.isActive}
                    onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                    className="size-4 accent-amber-600"
                  />
                  فعال
                </label>
              </div>
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
        title="حذف بنر"
        description={`آیا از حذف بنر «${deleteTarget?.title}» مطمئن هستید؟ این عملیات قابل بازگشت نیست.`}
        confirmLabel="حذف"
        loading={deleting}
        onConfirm={handleDelete}
      />
    </div>
  )
}