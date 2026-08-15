"use client"

import * as React from "react"
import {
  getAdminCategories,
  createCategory,
  updateCategory,
  deleteCategory,
  type AdminCategory,
} from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
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
import {
  PlusIcon, PencilIcon, Trash2Icon, Loader2Icon, ImageOffIcon,
} from "lucide-react"
import { ImageUploader } from "@/components/admin/image-uploader"

type CategoryForm = { name: string; description: string; imageUrl: string }

const EMPTY_FORM: CategoryForm = { name: "", description: "", imageUrl: "" }

export default function AdminCategoriesPage() {
  const [data, setData] = React.useState<AdminCategory[] | null>(null)
  const [error, setError] = React.useState("")
  const [dialogOpen, setDialogOpen] = React.useState(false)
  const [editing, setEditing] = React.useState<AdminCategory | null>(null)
  const [form, setForm] = React.useState<CategoryForm>(EMPTY_FORM)
  const [formError, setFormError] = React.useState("")
  const [saving, setSaving] = React.useState(false)
  const [deleteTarget, setDeleteTarget] = React.useState<AdminCategory | null>(null)
  const [deleting, setDeleting] = React.useState(false)

  const fetch = React.useCallback(() => {
    getAdminCategories()
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

  const openEdit = (c: AdminCategory) => {
    setEditing(c)
    setForm({ name: c.name, description: c.description ?? "", imageUrl: c.imageUrl })
    setFormError("")
    setDialogOpen(true)
  }

  const handleSave = async () => {
    if (form.name.trim().length < 2) {
      setFormError("نام دسته‌بندی باید حداقل ۲ کاراکتر باشد")
      return
    }
    setSaving(true)
    setFormError("")
    try {
      if (editing) {
        await updateCategory(editing.id, {
          name: form.name.trim(),
          description: form.description.trim() || undefined,
          imageUrl: form.imageUrl.trim() || undefined,
        })
        toast({ title: "دسته‌بندی به‌روزرسانی شد" })
      } else {
        await createCategory({
          name: form.name.trim(),
          description: form.description.trim() || undefined,
          imageUrl: form.imageUrl.trim() || undefined,
        })
        toast({ title: "دسته‌بندی ایجاد شد" })
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
      await deleteCategory(deleteTarget.id)
      toast({ title: "دسته‌بندی حذف شد" })
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
        title="دسته‌بندی‌ها"
        description={`مدیریت ${data?.length.toLocaleString("fa-IR") ?? ""} دسته‌بندی محصولات`}
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "دسته‌بندی‌ها" }]}
        actions={
          <Button onClick={openCreate}>
            <PlusIcon className="size-4" />
            دسته‌بندی جدید
          </Button>
        }
      />

      <div className="overflow-hidden rounded-xl border bg-card">
        {error ? (
          <ErrorState message="خطا در دریافت دسته‌بندی‌ها" onRetry={fetch} />
        ) : !data ? (
          <div className="p-4"><TableSkeleton rows={4} cols={4} /></div>
        ) : !data?.length ? (
          <EmptyState
            title="هنوز دسته‌بندی‌ای ثبت نشده است"
            description="اولین دسته‌بندی را ایجاد کنید"
            actionLabel="ایجاد دسته‌بندی"
            onAction={openCreate}
          />
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-16">تصویر</TableHead>
                  <TableHead>نام</TableHead>
                  <TableHead>توضیحات</TableHead>
                  <TableHead>شناسه</TableHead>
                  <TableHead className="w-24">عملیات</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.map((c) => (
                  <TableRow key={c.id}>
                    <TableCell>
                      {c.imageUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img src={c.imageUrl} alt={c.name} className="size-10 rounded-lg object-cover" loading="lazy" />
                      ) : (
                        <div className="flex size-10 items-center justify-center rounded-lg bg-muted">
                          <ImageOffIcon className="size-4 text-muted-foreground" />
                        </div>
                      )}
                    </TableCell>
                    <TableCell className="text-sm font-medium">{c.name}</TableCell>
                    <TableCell className="max-w-md truncate text-sm text-muted-foreground">{c.description || "—"}</TableCell>
                    <TableCell className="text-sm tabular-nums text-muted-foreground">{c.id.toLocaleString("fa-IR")}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button variant="ghost" size="icon-sm" onClick={() => openEdit(c)} aria-label="ویرایش">
                          <PencilIcon className="size-4" />
                        </Button>
                        <Button variant="ghost" size="icon-sm" className="text-destructive hover:text-destructive" onClick={() => setDeleteTarget(c)} aria-label="حذف">
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
            <DialogTitle>{editing ? "ویرایش دسته‌بندی" : "دسته‌بندی جدید"}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="c-name">
                نام دسته‌بندی <span className="text-destructive">*</span>
              </Label>
              <Input
                id="c-name"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                placeholder="مثلاً عروسک‌های حیوونی"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="c-desc">توضیحات</Label>
              <Textarea
                id="c-desc"
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                rows={3}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="c-img">آدرس تصویر</Label>
              <Input
                id="c-img"
                type="url"
                dir="ltr"
                value={form.imageUrl}
                onChange={(e) => setForm({ ...form, imageUrl: e.target.value })}
                placeholder="https://..."
              />
            </div>
            <div className="space-y-2">
              <Label>تصویر محصول</Label>
              <ImageUploader
                value={form.imageUrl}
                onUpload={async (url) => {
                  setForm(prev => ({ ...prev, imageUrl: url }))
                  return url
                }}
                onRemove={async () => {
                  setForm(prev => ({ ...prev, imageUrl: "" }))
                }}
                folder="categories"
                maxSizeMB={2}
              />
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
        title="حذف دسته‌بندی"
        description={`آیا از حذف دسته‌بندی «${deleteTarget?.name}» مطمئن هستید؟ این عملیات قابل بازگشت نیست.`}
        confirmLabel="حذف"
        loading={deleting}
        onConfirm={handleDelete}
      />
    </div>
  )
}
