"use client"

import * as React from "react"
import { useRouter } from "next/navigation"
import { createProduct, getAdminCategories, type AdminCategory } from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { PageHeader } from "@/components/admin/page-header"
import { Loader2Icon, ArrowRightIcon } from "lucide-react"
import { ImageUploader } from "@/components/admin/image-uploader"
import { ProductMediaEditor, type MediaImage, type MediaColor } from "@/components/admin/product-media-editor"

export default function NewProductPage() {
  const router = useRouter()
  const [categories, setCategories] = React.useState<AdminCategory[]>([])
  const [name, setName] = React.useState("")
  const [description, setDescription] = React.useState("")
  const [price, setPrice] = React.useState("")
  const [originalPrice, setOriginalPrice] = React.useState("")
  const [stock, setStock] = React.useState("")
  const [imageUrl, setImageUrl] = React.useState("")
  const [images, setImages] = React.useState<MediaImage[]>([])
  const [colors, setColors] = React.useState<MediaColor[]>([])
  const [categoryId, setCategoryId] = React.useState("")
  const [errors, setErrors] = React.useState<Record<string, string>>({})
  const [saving, setSaving] = React.useState(false)

  React.useEffect(() => {
    getAdminCategories().then((r) => setCategories(r.items)).catch(() => {})
  }, [])

  const validate = (): boolean => {
    const e: Record<string, string> = {}
    if (name.trim().length < 3) e.name = "نام محصول باید حداقل ۳ کاراکتر باشد"
    const p = Number(price)
    if (!price || isNaN(p) || p <= 0) e.price = "قیمت باید عددی بزرگ‌تر از صفر باشد"
    const op = originalPrice ? Number(originalPrice) : null
    if (op !== null && (isNaN(op) || op <= 0)) e.originalPrice = "قیمت قبلی نامعتبر است"
    const s = Number(stock)
    if (!stock || isNaN(s) || s < 0) e.stock = "موجودی باید عددی غیرمنفی باشد"
    if (!imageUrl.trim() && images.length === 0) e.imageUrl = "آدرس تصویر اجباری است"
    if (!categoryId) e.categoryId = "انتخاب دسته‌بندی اجباری است"
    if (colors.some((c) => !c.name.trim())) e.colors = "نام همه رنگ‌ها باید پر شود"
    setErrors(e)
    return Object.keys(e).length === 0
  }

  const handleSubmit = async (ev: React.FormEvent) => {
    ev.preventDefault()
    if (!validate()) return
    setSaving(true)
    try {
      const primaryImage = images.find((i) => i.isPrimary)?.url ?? imageUrl.trim()
      const id = await createProduct({
        name: name.trim(),
        description: description.trim() || undefined,
        price: Number(price),
        originalPrice: originalPrice ? Number(originalPrice) : undefined,
        stock: Number(stock),
        imageUrl: primaryImage,
        categoryId: Number(categoryId),
        images: images.length > 0 ? images.map((img) => ({ url: img.url, displayOrder: img.displayOrder, isPrimary: img.isPrimary, productColorId: img.productColorId })) : undefined,
        colors: colors.length > 0 ? colors.map((c) => ({ name: c.name.trim(), hexCode: c.hexCode || undefined, stock: c.stock, isActive: c.isActive, price: c.price ?? undefined })) : undefined,
      })
      toast({ title: "محصول ایجاد شد" })
      router.push(`/admin/products/${id}`)
    } catch (e: unknown) {
      toast({
        title: "خطا در ایجاد محصول",
        description: e instanceof Error ? e.message : "خطا",
        variant: "destructive",
      })
    } finally {
      setSaving(false)
    }
  }

  return (
    <div dir="rtl" className="mx-auto max-w-3xl space-y-6">
      <PageHeader
        title="ایجاد محصول جدید"
        description="اطلاعات محصول را وارد کنید"
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "محصولات", href: "/admin/products" }, { label: "محصول جدید" }]}
        actions={
          <Button variant="outline" size="sm" onClick={() => router.back()}>
            <ArrowRightIcon className="size-3.5" />
            بازگشت
          </Button>
        }
      />

      <form onSubmit={handleSubmit} noValidate className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">اطلاعات عمومی</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="p-name">
                نام محصول <span className="text-destructive">*</span>
              </Label>
              <Input
                id="p-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="نام محصول"
                aria-invalid={!!errors.name}
              />
              {errors.name && <p className="text-xs text-destructive">{errors.name}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="p-img">آدرس تصویر</Label>
              <Input
                id="p-img"
                type="url"
                dir="ltr"
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
                placeholder="https://..."
                aria-invalid={!!errors.imageUrl}
              />
              {errors.imageUrl && <p className="text-xs text-destructive">{errors.imageUrl}</p>}
            </div>
            <div className="space-y-2">
              <Label>تصویر محصول</Label>
              <ImageUploader
                value={imageUrl}
                onUpload={async (url) => {
                  setImageUrl(url)
                  return url
                }}
                onRemove={async () => {
                  setImageUrl("")
                }}
                folder="products"
                maxSizeMB={2}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="p-desc">توضیحات</Label>
              <Textarea
                id="p-desc"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={4}
                placeholder="توضیحات کامل محصول..."
              />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm">قیمت و موجودی</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="p-price">
                قیمت (تومان) <span className="text-destructive">*</span>
              </Label>
              <Input
                id="p-price"
                type="number"
                step="0.01"
                min="0"
                value={price}
                onChange={(e) => setPrice(e.target.value)}
                placeholder="0.00"
                aria-invalid={!!errors.price}
              />
              {errors.price && <p className="text-xs text-destructive">{errors.price}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="p-oprice">قیمت قبلی (برای نمایش تخفیف)</Label>
              <Input
                id="p-oprice"
                type="number"
                step="0.01"
                min="0"
                value={originalPrice}
                onChange={(e) => setOriginalPrice(e.target.value)}
                placeholder="0.00"
                aria-invalid={!!errors.originalPrice}
              />
              {errors.originalPrice && <p className="text-xs text-destructive">{errors.originalPrice}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="p-stock">
                موجودی <span className="text-destructive">*</span>
              </Label>
              <Input
                id="p-stock"
                type="number"
                min="0"
                value={stock}
                onChange={(e) => setStock(e.target.value)}
                placeholder="0"
                aria-invalid={!!errors.stock}
              />
              {errors.stock && <p className="text-xs text-destructive">{errors.stock}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="p-cat">
                دسته‌بندی <span className="text-destructive">*</span>
              </Label>
              <select
                id="p-cat"
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
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm">تصاویر و رنگ‌ها</CardTitle>
          </CardHeader>
          <CardContent>
            <ProductMediaEditor
              images={images}
              onImagesChange={setImages}
              colors={colors}
              onColorsChange={setColors}
            />
            {errors.colors && <p className="mt-2 text-xs text-destructive">{errors.colors}</p>}
          </CardContent>
        </Card>

        <div className="flex items-center gap-2">
          <Button type="submit" disabled={saving} className="gap-2">
            {saving && <Loader2Icon className="size-4 animate-spin" />}
            {saving ? "در حال ذخیره..." : "ذخیره محصول"}
          </Button>
          <Button type="button" variant="outline" onClick={() => router.push("/admin/products")}>
            انصراف
          </Button>
        </div>
      </form>
    </div>
  )
}
