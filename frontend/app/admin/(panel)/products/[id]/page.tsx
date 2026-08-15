"use client"

import * as React from "react"
import { useParams, useRouter, useSearchParams } from "next/navigation"
import {
  getAdminProduct,
  getAdminCategories,
  updateProduct,
  deleteProduct,
  type AdminProduct,
  type AdminCategory,
} from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { PageHeader } from "@/components/admin/page-header"
import { ConfirmDialog } from "@/components/admin/confirm-dialog"
import { ErrorState } from "@/components/admin/states"
import { StockBadge } from "@/components/admin/status-badge"
import { formatCurrency, formatDateShort } from "@/lib/admin-i18n"
import { Loader2Icon, PencilIcon, SaveIcon, XIcon, Trash2Icon, ArrowRightIcon } from "lucide-react"
import { ImageUploader } from "@/components/admin/image-uploader"
import { ProductMediaEditor, type MediaImage, type MediaColor } from "@/components/admin/product-media-editor"

export default function AdminProductDetailPage() {
  const params = useParams<{ id: string }>()
  const router = useRouter()
  const searchParams = useSearchParams()
  const id = Number(params.id)

  const [product, setProduct] = React.useState<AdminProduct | null>(null)
  const [categories, setCategories] = React.useState<AdminCategory[]>([])
  const [error, setError] = React.useState("")
  const [editMode, setEditMode] = React.useState(searchParams.get("edit") === "1")

  const [name, setName] = React.useState("")
  const [description, setDescription] = React.useState("")
  const [price, setPrice] = React.useState("")
  const [originalPrice, setOriginalPrice] = React.useState("")
  const [stock, setStock] = React.useState("")
  const [imageUrl, setImageUrl] = React.useState("")
  const [images, setImages] = React.useState<MediaImage[]>([])
  const [colors, setColors] = React.useState<MediaColor[]>([])
  const [categoryId, setCategoryId] = React.useState("")
  const [saving, setSaving] = React.useState(false)
  const [errors, setErrors] = React.useState<Record<string, string>>({})
  const [deleteOpen, setDeleteOpen] = React.useState(false)
  const [deleting, setDeleting] = React.useState(false)

  const fetch = React.useCallback(() => {
    Promise.all([getAdminProduct(id), getAdminCategories().then((r) => r.items).catch(() => [] as AdminCategory[])])
      .then(([p, cats]) => {
        setProduct(p)
        setCategories(cats)
        setName(p.name)
        setDescription(p.description ?? "")
        setPrice(String(p.price))
        setOriginalPrice(p.originalPrice ? String(p.originalPrice) : "")
        setStock(String(p.stock))
        setImageUrl(p.imageUrl)
        // Map real color ids → index for the editor (productColorId is an index)
        const cols = (p.colors ?? []).map((c) => ({ name: c.name, hexCode: c.hexCode ?? "", stock: c.stock, isActive: c.isActive, price: c.price ?? null }))
        const idToIdx = new Map(cols.map((c, i) => [(p.colors ?? [])[i].id, i]))
        setImages((p.images ?? []).map((img) => ({ url: img.url, displayOrder: img.displayOrder, isPrimary: img.isPrimary, productColorId: img.productColorId != null ? (idToIdx.get(img.productColorId) ?? null) : null })))
        setColors(cols)
        setCategoryId(p.categoryId ? String(p.categoryId) : "")
        setError("")
      })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [id])

  React.useEffect(() => { fetch() }, [fetch])

  const validate = (): boolean => {
    const e: Record<string, string> = {}
    if (name.trim().length < 3) e.name = "نام محصول باید حداقل ۳ کاراکتر باشد"
    const p = Number(price)
    if (!price || isNaN(p) || p <= 0) e.price = "قیمت باید عددی بزرگ‌تر از صفر باشد"
    const op = originalPrice ? Number(originalPrice) : null
    if (op !== null && (isNaN(op) || op <= 0)) e.originalPrice = "قیمت قبلی نامعتبر است"
    const s = Number(stock)
    if (!stock || isNaN(s) || s < 0) e.stock = "موجودی باید عددی غیرمنفی باشد"
    if (!categoryId) e.categoryId = "انتخاب دسته‌بندی اجباری است"
    setErrors(e)
    return Object.keys(e).length === 0
  }

  const handleSave = async () => {
    if (!validate()) return
    setSaving(true)
    try {
      await updateProduct(id, {
        name: name.trim(),
        description: description.trim() || undefined,
        price: Number(price),
        originalPrice: originalPrice ? Number(originalPrice) : undefined,
        stock: Number(stock),
        imageUrl: images.find((i) => i.isPrimary)?.url ?? imageUrl.trim(),
        categoryId: Number(categoryId),
        images: images.length > 0 ? images.map((img) => ({ url: img.url, displayOrder: img.displayOrder, isPrimary: img.isPrimary, productColorId: img.productColorId })) : undefined,
        colors: colors.length > 0 ? colors.map((c) => ({ name: c.name.trim(), hexCode: c.hexCode || undefined, stock: c.stock, isActive: c.isActive, price: c.price ?? undefined })) : undefined,
      })
      toast({ title: "تغییرات ذخیره شد" })
      setEditMode(false)
      fetch()
    } catch (e: unknown) {
      toast({
        title: "خطا در ذخیره",
        description: e instanceof Error ? e.message : "خطا",
        variant: "destructive",
      })
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async () => {
    setDeleting(true)
    try {
      await deleteProduct(id)
      toast({ title: "محصول حذف شد" })
      router.push("/admin/products")
    } catch (e: unknown) {
      toast({
        title: "خطا در حذف",
        description: e instanceof Error ? e.message : "خطا",
        variant: "destructive",
      })
      setDeleteOpen(false)
    } finally {
      setDeleting(false)
    }
  }

  if (error || !product) {
    return (
      <div className="space-y-4">
        <PageHeader title="جزئیات محصول" breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "محصولات", href: "/admin/products" }, { label: `محصول ${id}` }]} />
        <Card>
          <ErrorState message="خطا در دریافت محصول" onRetry={fetch} />
        </Card>
      </div>
    )
  }

  if (!product) {
    return (
      <div className="space-y-4">
        <div className="h-8 w-56 animate-pulse rounded bg-muted" />
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="h-80 animate-pulse rounded-xl border bg-card" />
          <div className="h-80 animate-pulse rounded-xl border bg-card lg:col-span-2" />
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={editMode ? "ویرایش محصول" : product.name}
        description={editMode ? "تغییرات را اعمال کنید" : `شناسه محصول: #${id}`}
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "محصولات", href: "/admin/products" }, { label: editMode ? "ویرایش" : product.name }]}
        actions={
          <>
            {!editMode ? (
              <Button size="sm" onClick={() => setEditMode(true)}>
                <PencilIcon className="size-3.5" />
                ویرایش
              </Button>
            ) : (
              <>
                <Button size="sm" variant="outline" onClick={() => { setEditMode(false); fetch() }}>
                  <XIcon className="size-3.5" />
                  انصراف
                </Button>
                <Button size="sm" onClick={handleSave} disabled={saving}>
                  {saving ? <Loader2Icon className="size-3.5 animate-spin" /> : <SaveIcon className="size-3.5" />}
                  ذخیره
                </Button>
              </>
            )}
            <Button size="sm" variant="destructive" onClick={() => setDeleteOpen(true)}>
              <Trash2Icon className="size-3.5" />
              حذف
            </Button>
          </>
        }
      />

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Image */}
        <Card>
          <CardContent className="p-4">
            {product.imageUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img src={product.imageUrl} alt={product.name} className="aspect-square w-full rounded-xl object-cover" />
            ) : (
              <div className="flex aspect-square w-full items-center justify-center rounded-xl bg-muted text-muted-foreground">
                بدون تصویر
              </div>
            )}
          </CardContent>
        </Card>

        {/* Details / Edit form */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-sm">{editMode ? "فرم ویرایش" : "اطلاعات محصول"}</CardTitle>
          </CardHeader>
          <CardContent>
            {editMode ? (
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2 sm:col-span-2">
                  <Label htmlFor="e-name">نام محصول <span className="text-destructive">*</span></Label>
                  <Input id="e-name" value={name} onChange={(e) => setName(e.target.value)} aria-invalid={!!errors.name} />
                  {errors.name && <p className="text-xs text-destructive">{errors.name}</p>}
                </div>
                <div className="space-y-2 sm:col-span-2">
                  <Label htmlFor="e-desc">توضیحات</Label>
                  <Textarea id="e-desc" value={description} onChange={(e) => setDescription(e.target.value)} rows={4} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="e-price">قیمت (تومان) <span className="text-destructive">*</span></Label>
                  <Input id="e-price" type="number" step="0.01" min="0" value={price} onChange={(e) => setPrice(e.target.value)} aria-invalid={!!errors.price} />
                  {errors.price && <p className="text-xs text-destructive">{errors.price}</p>}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="e-oprice">قیمت قبلی</Label>
                  <Input id="e-oprice" type="number" step="0.01" min="0" value={originalPrice} onChange={(e) => setOriginalPrice(e.target.value)} aria-invalid={!!errors.originalPrice} />
                  {errors.originalPrice && <p className="text-xs text-destructive">{errors.originalPrice}</p>}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="e-stock">موجودی <span className="text-destructive">*</span></Label>
                  <Input id="e-stock" type="number" min="0" value={stock} onChange={(e) => setStock(e.target.value)} aria-invalid={!!errors.stock} />
                  {errors.stock && <p className="text-xs text-destructive">{errors.stock}</p>}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="e-cat">دسته‌بندی <span className="text-destructive">*</span></Label>
                  <select
                    id="e-cat"
                    value={categoryId}
                    onChange={(e) => setCategoryId(e.target.value)}
                    className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    aria-invalid={!!errors.categoryId}
                  >
                    <option value="">انتخاب کنید...</option>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>{c.name}</option>
                    ))}
                  </select>
                  {errors.categoryId && <p className="text-xs text-destructive">{errors.categoryId}</p>}
                </div>
                <div className="space-y-2 sm:col-span-2">
                  <Label htmlFor="e-img">تصاویر و رنگ‌ها</Label>
                  <ProductMediaEditor
                    images={images}
                    onImagesChange={setImages}
                    colors={colors}
                    onColorsChange={setColors}
                  />
                  {errors.colors && <p className="text-xs text-destructive">{errors.colors}</p>}
                </div>
              </div>
            ) : (
              <dl className="grid gap-4 text-sm sm:grid-cols-2">
                <div>
                  <dt className="text-xs text-muted-foreground">دسته‌بندی</dt>
                  <dd className="mt-0.5 font-medium">
                    {categories.find((c) => c.id === product.categoryId)?.name ?? product.category?.name ?? "—"}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs text-muted-foreground">قیمت</dt>
                  <dd className="mt-0.5 font-bold tabular-nums">{formatCurrency(product.price)}</dd>
                </div>
                {product.originalPrice != null && product.originalPrice > product.price && (
                  <div>
                    <dt className="text-xs text-muted-foreground">قیمت قبلی</dt>
                    <dd className="mt-0.5 text-muted-foreground line-through">{formatCurrency(product.originalPrice)}</dd>
                  </div>
                )}
                <div>
                  <dt className="text-xs text-muted-foreground">موجودی</dt>
                  <dd className="mt-0.5"><StockBadge stock={product.stock} /></dd>
                </div>
                <div>
                  <dt className="text-xs text-muted-foreground">امتیاز</dt>
                  <dd className="mt-0.5 font-medium tabular-nums">{product.rating.toLocaleString("fa-IR", { maximumFractionDigits: 1 })}</dd>
                </div>
                <div>
                  <dt className="text-xs text-muted-foreground">وضعیت</dt>
                  <dd className="mt-0.5">
                    <Badge variant={product.isAvailable ? "default" : "secondary"}>
                      {product.isAvailable ? "موجود" : "ناموجود"}
                    </Badge>
                  </dd>
                </div>
                {product.description && (
                  <div className="sm:col-span-2">
                    <dt className="text-xs text-muted-foreground">توضیحات</dt>
                    <dd className="mt-0.5 whitespace-pre-line leading-relaxed">{product.description}</dd>
                  </div>
                )}
              </dl>
            )}
          </CardContent>
        </Card>
      </div>

      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title="حذف محصول"
        description={`آیا از حذف «${product.name}» مطمئن هستید؟ این عملیات قابل بازگشت نیست.`}
        confirmLabel="حذف محصول"
        loading={deleting}
        onConfirm={handleDelete}
      />
    </div>
  )
}
