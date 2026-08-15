"use client"

import { useState, useRef, useCallback } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { UploadIcon, XIcon, ImageIcon, Loader2Icon } from "lucide-react"
import { cn } from "@/lib/utils"
import { toast } from "@/hooks/use-toast"
import { useLocale } from "@/lib/locale-context"
import { resolveImageUrl } from "@/lib/config"

interface ImageUploaderProps {
  value?: string
  onUpload: (url: string) => Promise<string>
  onRemove: () => Promise<void>
  folder?: string
  maxSizeMB?: number
  accept?: string
  className?: string
}

export function ImageUploader({
  value,
  onUpload,
  onRemove,
  folder = "products",
  maxSizeMB = 5,
  accept = "image/*",
  className,
}: ImageUploaderProps) {
  const { t, tva, locale } = useLocale()
  const number = (n: number) => new Intl.NumberFormat(locale).format(n)
  const [isUploading, setIsUploading] = useState(false)
  const [dragActive, setDragActive] = useState(false)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const validateFile = useCallback((file: File): string | null => {
    if (!file.type.startsWith("image/")) {
      return t("admin.image.notImage")
    }

    const ext = file.name.split(".").pop()?.toLowerCase()
    const validExt = ["jpg", "jpeg", "png", "webp"]
    if (!validExt.includes(ext || "")) {
      return t("admin.image.invalidFormat")
    }

    if (file.size > maxSizeMB * 1024 * 1024) {
      return tva("admin.image.tooLarge", { size: number(maxSizeMB) })
    }

    return null
  }, [maxSizeMB, t, tva, number])

  const handleFile = useCallback(async (file: File) => {
    const validationError = validateFile(file)
    if (validationError) {
      toast({ title: t("admin.image.uploadError"), description: validationError, variant: "destructive" })
      setError(validationError)
      return
    }

    setError(null)
    setIsUploading(true)

    try {
      const formData = new FormData()
      formData.append("file", file)
      formData.append("folder", folder)
      formData.append("category", "product")

      // All API traffic goes through the API Gateway (see lib/config.ts).
      const uploadUrl = `${process.env.NEXT_PUBLIC_API_GATEWAY_URL ?? 'http://localhost:5100'}/api/images/upload`

      const token =
        typeof window !== "undefined"
          ? (localStorage.getItem("token") ||
            document.cookie.match(/(?:^|;\s*)token=([^;]*)/)?.[1])
          : null;

      const response = await fetch(uploadUrl, {
        method: "POST",
        headers: {
          "x-api-key": process.env.NEXT_PUBLIC_API_KEY || "",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: formData,
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || t("admin.image.uploadFailed"))
      }

      const data = await response.json()
      const uploadedUrl = data.url || data.imageUrl || data.location || data.path || data.filePath
      if (!uploadedUrl) {
        throw new Error(t("admin.image.noUrl"))
      }

      // Store relative URL in DB; preview resolves via gateway.
      await onUpload(uploadedUrl)
      setPreviewUrl(resolveImageUrl(uploadedUrl))

      toast({
        title: t("admin.image.uploaded"),
        description: tva("admin.image.uploadedDesc", { name: file.name, folder }),
      })
    } catch (error) {
      console.error("Upload error:", error)
      toast({
        title: t("admin.image.uploadError"),
        description: error instanceof Error ? error.message : t("admin.image.validFile"),
        variant: "destructive",
      })
    } finally {
      setIsUploading(false)
    }
  }, [validateFile, folder, onUpload, t, tva])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setDragActive(false)

    const files = Array.from(e.dataTransfer.files)
    if (files.length > 0) {
      handleFile(files[0])
    }
  }, [handleFile])

  const handleChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (files && files.length > 0) {
      handleFile(files[0])
    }
    e.target.value = ""
  }, [handleFile])

  const removeImage = useCallback(async () => {
    try {
      if (value) {
        await onRemove()
      }
      setPreviewUrl(null)
      setError(null)
      toast({
        title: t("admin.image.deleted"),
        description: t("admin.image.deletedDesc"),
      })
    } catch (error) {
      toast({
        title: t("admin.image.deleteError"),
        description: t("admin.image.deleteFailed"),
        variant: "destructive",
      })
    }
  }, [value, onRemove, t])

  const displayUrl = previewUrl || value

  return (
    <div className={cn("w-full space-y-4", className)}>
      {!displayUrl ? (
        <div
          className={cn(
            "relative border-2 border-dashed rounded-lg p-8 transition-colors",
            "hover:border-primary/50 cursor-pointer",
            dragActive && "border-primary bg-primary/5",
            "border-muted-foreground/25 bg-muted/50",
          )}
          onDragOver={(e) => {
            e.preventDefault()
            setDragActive(true)
          }}
          onDragLeave={() => setDragActive(false)}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
        >
          <input
            ref={fileInputRef}
            type="file"
            accept={accept}
            onChange={handleChange}
            className="hidden"
            disabled={isUploading}
          />

          <div className="flex flex-col items-center space-y-4">
            {isUploading ? (
              <Loader2Icon className="h-12 w-12 text-muted-foreground animate-spin" />
            ) : (
              <div className="h-12 w-12 rounded-full bg-muted flex items-center justify-center">
                <UploadIcon className="h-6 w-6 text-muted-foreground" />
              </div>
            )}

            <div className="text-center space-y-2">
              <p className="text-sm font-medium">
                {isUploading
                  ? t("admin.image.uploading")
                  : t("admin.image.dragOrClick")
                }
              </p>

              <p className="text-xs text-muted-foreground">
                {tva("admin.image.formats", { size: number(maxSizeMB) })}
              </p>

              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={isUploading}
                onClick={(e) => {
                  e.stopPropagation()
                  fileInputRef.current?.click()
                }}
              >
                {t("admin.image.selectFile")}
              </Button>
            </div>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          <div className="relative group">
            <img
              src={displayUrl}
              alt={t("admin.image.previewAlt")}
              className="w-full h-64 object-cover rounded-lg"
              onError={(e) => { (e.target as HTMLImageElement).style.opacity = "0.2" }}
            />

            <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity rounded-lg flex items-center justify-center gap-2">
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={(e) => {
                  e.stopPropagation()
                  fileInputRef.current?.click()
                }}
              >
                {t("admin.image.replace")}
              </Button>

              <Button
                type="button"
                variant="destructive"
                size="sm"
                onClick={(e) => {
                  e.stopPropagation()
                  removeImage()
                }}
              >
                {t("admin.image.removeImage")}
              </Button>
            </div>
          </div>

          <div className="text-xs text-muted-foreground text-center">
            {value && !previewUrl && <span>{t("admin.image.current")}</span>}
            {previewUrl && <span>{t("admin.image.newUpload")}</span>}
          </div>
        </div>
      )}

      {error && (
        <div className="text-xs text-destructive text-center">
          {error}
        </div>
      )}
    </div>
  )
}