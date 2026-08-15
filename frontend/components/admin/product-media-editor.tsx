"use client"

import * as React from "react"
import { ImageUploader } from "@/components/admin/image-uploader"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { CheckIcon, StarIcon, Trash2Icon, XIcon } from "lucide-react"
import { cn } from "@/lib/utils"

export interface MediaImage {
  url: string
  displayOrder: number
  isPrimary: boolean
  productColorId: number | null
}

export interface MediaColor {
  name: string
  hexCode: string
  stock: number
  isActive: boolean
  price: number | null
}

interface Props {
  images: MediaImage[]
  onImagesChange: (imgs: MediaImage[]) => void
  colors: MediaColor[]
  onColorsChange: (colors: MediaColor[]) => void
}

export function ProductMediaEditor({ images, onImagesChange, colors, onColorsChange }: Props) {
  const [activeColorIdx, setActiveColorIdx] = React.useState<number | null>(null)

  // Images without a color (product-level)
  const productImages = images.filter((i) => i.productColorId === null)
  // Images per color: productColorId is the index into colors[]
  const colorImages = (idx: number) => images.filter((i) => i.productColorId === idx)
  const currentBucket = activeColorIdx === null ? productImages : colorImages(activeColorIdx)

  const addImage = (url: string, colorIdx: number | null) => {
    const bucket = colorIdx === null ? productImages : colorImages(colorIdx)
    const isFirst = bucket.length === 0
    onImagesChange([
      ...images,
      { url, displayOrder: bucket.length, isPrimary: isFirst, productColorId: colorIdx },
    ])
  }

  const removeImage = (idx: number) => {
    // idx is index within the current bucket
    const colorIdx = activeColorIdx
    const bucket = colorIdx === null ? productImages : colorImages(colorIdx)
    const removed = bucket[idx]
    if (!removed) return
    const remaining = images.filter((i) => i !== removed).map((img) => {
      // re-index displayOrder within its bucket
      const sameBucket = img.productColorId === removed.productColorId
      return sameBucket && img.displayOrder > removed.displayOrder
        ? { ...img, displayOrder: img.displayOrder - 1 }
        : img
    })
    // ensure exactly one primary in the bucket
    const next = remaining.map((img) => ({ ...img }))
    const bucketNow = next.filter((i) => i.productColorId === removed.productColorId)
    if (bucketNow.length > 0 && !bucketNow.some((i) => i.isPrimary)) {
      const first = bucketNow.sort((a, b) => a.displayOrder - b.displayOrder)[0]
      const fi = next.indexOf(first)
      next[fi] = { ...next[fi], isPrimary: true }
    }
    onImagesChange(next)
  }

  const setPrimary = (idx: number) => {
    const colorIdx = activeColorIdx
    const bucket = colorIdx === null ? productImages : colorImages(colorIdx)
    const target = bucket[idx]
    if (!target) return
    onImagesChange(
      images.map((img) =>
        img.productColorId === target.productColorId
          ? { ...img, isPrimary: img === target }
          : img
      )
    )
  }

  const move = (idx: number, dir: -1 | 1) => {
    const colorIdx = activeColorIdx
    const bucket = colorIdx === null ? productImages : colorImages(colorIdx)
    const target = idx + dir
    if (target < 0 || target >= bucket.length) return
    const a = bucket[idx]
    const b = bucket[target]
    if (!a || !b) return
    onImagesChange(
      images.map((img) => {
        if (img === a) return { ...img, displayOrder: b.displayOrder }
        if (img === b) return { ...img, displayOrder: a.displayOrder }
        return img
      })
    )
  }

  const updateColor = (idx: number, patch: Partial<MediaColor>) => {
    onColorsChange(colors.map((c, i) => (i === idx ? { ...c, ...patch } : c)))
  }

  const addColor = () => {
    onColorsChange([...colors, { name: "", hexCode: "#888888", stock: 0, isActive: true, price: null }])
  }

  const removeColor = (idx: number) => {
    onColorsChange(colors.filter((_, i) => i !== idx))
    // drop images that belonged to this color; re-index remaining colors' image refs
    const nextImages = images
      .filter((i) => i.productColorId !== idx)
      .map((img) =>
        img.productColorId !== null && img.productColorId > idx
          ? { ...img, productColorId: (img.productColorId as number) - 1 }
          : img
      )
    onImagesChange(nextImages)
    if (activeColorIdx === idx) setActiveColorIdx(null)
    else if (activeColorIdx !== null && activeColorIdx > idx) setActiveColorIdx(activeColorIdx - 1)
  }

  const renderBucket = (bucket: MediaImage[], colorIdx: number | null) => (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-xs text-muted-foreground">{bucket.length} تصویر</span>
        <ImageUploader
          key={`up-${colorIdx ?? "p"}-${bucket.length}`}
          value=""
          onUpload={async (url) => {
            addImage(url, colorIdx)
            return url
          }}
          onRemove={async () => {}}
          folder="products"
          maxSizeMB={5}
        />
      </div>
      {bucket.length > 0 && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
          {bucket.map((img, i) => (
            <div key={i} className="group relative overflow-hidden rounded-lg border bg-muted">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={img.url} alt={`تصویر ${i + 1}`} className="aspect-square w-full object-cover" />
              <div className="absolute inset-x-0 bottom-0 flex items-center justify-between gap-1 bg-black/60 p-1.5">
                <button
                  type="button"
                  onClick={() => setPrimary(i)}
                  title={img.isPrimary ? "تصویر اصلی" : "انتخاب به عنوان تصویر اصلی"}
                  className={cn(
                    "flex size-7 items-center justify-center rounded-full text-white transition",
                    img.isPrimary ? "bg-amber-500" : "bg-white/20 hover:bg-white/40"
                  )}
                >
                  <StarIcon className="size-3.5" />
                </button>
                <div className="flex gap-1">
                  <button
                    type="button"
                    onClick={() => move(i, -1)}
                    disabled={i === 0}
                    className="flex size-7 items-center justify-center rounded-full bg-white/20 text-white transition hover:bg-white/40 disabled:opacity-30"
                  >
                    ↑
                  </button>
                  <button
                    type="button"
                    onClick={() => move(i, 1)}
                    disabled={i === bucket.length - 1}
                    className="flex size-7 items-center justify-center rounded-full bg-white/20 text-white transition hover:bg-white/40 disabled:opacity-30"
                  >
                    ↓
                  </button>
                  <button
                    type="button"
                    onClick={() => removeImage(i)}
                    className="flex size-7 items-center justify-center rounded-full bg-red-500/80 text-white transition hover:bg-red-500"
                    title="حذف تصویر"
                  >
                    <Trash2Icon className="size-3.5" />
                  </button>
                </div>
              </div>
              {img.isPrimary && (
                <span className="absolute start-2 top-2 rounded-full bg-amber-500 px-2 py-0.5 text-[10px] font-bold text-white">
                  تصویر اصلی
                </span>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )

  return (
    <div className="space-y-6">
      {/* Colors */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Label className="text-sm font-medium">رنگ‌های محصول</Label>
          <Button type="button" size="sm" variant="outline" onClick={addColor}>
            + افزودن رنگ
          </Button>
        </div>
        <p className="text-xs text-muted-foreground">
          برای هر رنگ، تصاویر مخصوص همان رنگ را بارگذاری کنید. تصویر اول هر رنگ، تصویر اصلی آن رنگ است.
        </p>
        {colors.length === 0 && (
          <p className="text-xs text-muted-foreground">رنگی انتخاب نشده است. محصول بدون رنگ خواهد بود.</p>
        )}
        <div className="space-y-2">
          {colors.map((c, i) => (
            <div key={i} className="flex flex-wrap items-center gap-2 rounded-lg border p-2">
              <input
                type="color"
                value={/^#[0-9a-fA-F]{6}$/.test(c.hexCode) ? c.hexCode : "#888888"}
                onChange={(e) => updateColor(i, { hexCode: e.target.value })}
                className="size-8 cursor-pointer rounded border"
                aria-label={`رنگ ${i + 1}`}
              />
              <Input
                value={c.name}
                onChange={(e) => updateColor(i, { name: e.target.value })}
                placeholder="نام رنگ (مثلاً صورتی)"
                className="h-8 w-32 flex-1 sm:w-40"
              />
              <Input
                type="number"
                min="0"
                value={c.stock}
                onChange={(e) => updateColor(i, { stock: Number(e.target.value) })}
                placeholder="موجودی"
                className="h-8 w-20"
              />
              <Input
                type="number"
                min="0"
                value={c.price ?? ""}
                onChange={(e) => updateColor(i, { price: e.target.value === "" ? null : Number(e.target.value) })}
                placeholder="قیمت (اختیاری)"
                className="h-8 w-28"
              />
              <button
                type="button"
                onClick={() => updateColor(i, { isActive: !c.isActive })}
                className={cn(
                  "flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium transition",
                  c.isActive ? "bg-emerald-100 text-emerald-700" : "bg-muted text-muted-foreground"
                )}
                aria-pressed={c.isActive}
              >
                <CheckIcon className="size-3" />
                {c.isActive ? "فعال" : "غیرفعال"}
              </button>
              <button
                type="button"
                onClick={() => removeColor(i)}
                className="flex size-7 items-center justify-center rounded-full text-destructive transition hover:bg-destructive/10"
                title="حذف رنگ"
              >
                <XIcon className="size-3.5" />
              </button>
            </div>
          ))}
        </div>
      </div>

      {/* Color image buckets */}
      {colors.length > 0 && (
        <div className="space-y-4 rounded-lg border p-3">
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={() => setActiveColorIdx(null)}
              className={cn(
                "rounded-full border px-3 py-1 text-xs font-medium transition",
                activeColorIdx === null
                  ? "border-primary bg-primary/10 text-primary"
                  : "border-border hover:border-primary/40"
              )}
            >
              تصاویر عمومی
            </button>
            {colors.map((c, i) => (
              <button
                key={i}
                type="button"
                onClick={() => setActiveColorIdx(i)}
                className={cn(
                  "flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-medium transition",
                  activeColorIdx === i
                    ? "border-primary bg-primary/10 text-primary"
                    : "border-border hover:border-primary/40"
                )}
              >
                <span className="size-2.5 rounded-full border border-black/10" style={{ backgroundColor: c.hexCode || "#ccc" }} />
                {c.name || `رنگ ${i + 1}`}
                <span className="text-muted-foreground">({colorImages(i).length})</span>
              </button>
            ))}
          </div>
          {renderBucket(currentBucket, activeColorIdx)}
        </div>
      )}

      {/* Product-level images (no colors OR general images) */}
      {colors.length === 0 && (
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <Label className="text-sm font-medium">تصاویر محصول</Label>
            <span className="text-xs text-muted-foreground">{productImages.length} تصویر</span>
          </div>
          <ImageUploader
            key={`up-p-${productImages.length}`}
            value=""
            onUpload={async (url) => {
              addImage(url, null)
              return url
            }}
            onRemove={async () => {}}
            folder="products"
            maxSizeMB={5}
          />
          {productImages.length > 0 && (
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
              {productImages.map((img, i) => (
                <div key={i} className="group relative overflow-hidden rounded-lg border bg-muted">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={img.url} alt={`تصویر ${i + 1}`} className="aspect-square w-full object-cover" />
                  <div className="absolute inset-x-0 bottom-0 flex items-center justify-between gap-1 bg-black/60 p-1.5">
                    <button
                      type="button"
                      onClick={() => setPrimary(i)}
                      title={img.isPrimary ? "تصویر اصلی" : "انتخاب به عنوان تصویر اصلی"}
                      className={cn(
                        "flex size-7 items-center justify-center rounded-full text-white transition",
                        img.isPrimary ? "bg-amber-500" : "bg-white/20 hover:bg-white/40"
                      )}
                    >
                      <StarIcon className="size-3.5" />
                    </button>
                    <div className="flex gap-1">
                      <button
                        type="button"
                        onClick={() => move(i, -1)}
                        disabled={i === 0}
                        className="flex size-7 items-center justify-center rounded-full bg-white/20 text-white transition hover:bg-white/40 disabled:opacity-30"
                      >
                        ↑
                      </button>
                      <button
                        type="button"
                        onClick={() => move(i, 1)}
                        disabled={i === productImages.length - 1}
                        className="flex size-7 items-center justify-center rounded-full bg-white/20 text-white transition hover:bg-white/40 disabled:opacity-30"
                      >
                        ↓
                      </button>
                      <button
                        type="button"
                        onClick={() => removeImage(i)}
                        className="flex size-7 items-center justify-center rounded-full bg-red-500/80 text-white transition hover:bg-red-500"
                        title="حذف تصویر"
                      >
                        <Trash2Icon className="size-3.5" />
                      </button>
                    </div>
                  </div>
                  {img.isPrimary && (
                    <span className="absolute start-2 top-2 rounded-full bg-amber-500 px-2 py-0.5 text-[10px] font-bold text-white">
                      تصویر اصلی
                    </span>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
