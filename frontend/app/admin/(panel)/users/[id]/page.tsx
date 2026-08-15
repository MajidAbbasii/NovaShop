"use client"

import * as React from "react"
import { useParams, useRouter } from "next/navigation"
import { getAdminUser, updateUser, type UserDto } from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { PageHeader } from "@/components/admin/page-header"
import { ErrorState } from "@/components/admin/states"
import { formatDate } from "@/lib/admin-i18n"
import {
  MailIcon, PhoneIcon, UserIcon, CalendarIcon, ShieldIcon, ArrowRightIcon, Loader2Icon,
} from "lucide-react"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import { ImageUploader } from "@/components/admin/image-uploader"

export default function AdminUserDetailPage() {
  const params = useParams<{ id: string }>()
  const router = useRouter()
  const id = Number(params.id)

  const [user, setUser] = React.useState<UserDto | null>(null)
  const [error, setError] = React.useState("")
  const [role, setRole] = React.useState("")
  const [isActive, setIsActive] = React.useState(true)
  const [newPassword, setNewPassword] = React.useState("")
  const [saving, setSaving] = React.useState(false)

  const fetch = React.useCallback(() => {
    getAdminUser(id)
      .then((u) => {
        setUser(u)
        setRole(u.role)
        setIsActive(u.isActive)
        setError("")
      })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [id])

  React.useEffect(() => { fetch() }, [fetch])

  const handleSave = async () => {
    setSaving(true)
    try {
      await updateUser(id, {
        role,
        isActive,
        ...(newPassword.trim() ? { password: newPassword.trim() } : {}),
      })
      setNewPassword("")
      toast({ title: "کاربر به‌روزرسانی شد" })
      fetch()
    } catch (e: unknown) {
      toast({
        title: "خطا در به‌روزرسانی",
        description: e instanceof Error ? e.message : "خطا",
        variant: "destructive",
      })
    } finally {
      setSaving(false)
    }
  }

  if (error || !user) {
    return (
      <div className="space-y-4">
        <PageHeader title="جزئیات کاربر" breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "کاربران", href: "/admin/users" }, { label: `کاربر ${id}` }]} />
        <Card><ErrorState message="خطا در دریافت کاربر" onRetry={fetch} /></Card>
      </div>
    )
  }

  if (!user) {
    return (
      <div className="space-y-4">
        <div className="h-8 w-56 animate-pulse rounded bg-muted" />
        <div className="h-72 animate-pulse rounded-xl border bg-card" />
      </div>
    )
  }

  const changed = role !== user.role || isActive !== user.isActive

  return (
    <div className="space-y-6">
      <PageHeader
        title={user.username}
        description={`کاربر #${id}`}
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "کاربران", href: "/admin/users" }, { label: user.username }]}
        actions={
          <Button variant="outline" size="sm" onClick={() => router.push("/admin/users")}>
            <ArrowRightIcon className="size-3.5" />
            بازگشت به کاربران
          </Button>
        }
      />

      <div className="grid gap-6 lg:grid-cols-3">
        <Card>
          <CardContent className="flex flex-col items-center gap-3 p-6 text-center">
            <Avatar className="size-20">
              <AvatarFallback className="bg-primary/10 text-2xl text-primary">
                {(user.firstName || user.username).slice(0, 1).toUpperCase()}
              </AvatarFallback>
            </Avatar>
            <div>
              <p className="text-base font-bold">{[user.firstName, user.lastName].filter(Boolean).join(" ") || user.username}</p>
              <p className="text-sm text-muted-foreground" dir="ltr">{user.username}</p>
            </div>
            <div className="flex flex-wrap items-center justify-center gap-2">
              <Badge variant={user.role === "Admin" ? "default" : "secondary"}>
                {user.role === "Admin" ? "مدیر" : "مشتری"}
              </Badge>
              <Badge variant={user.isActive ? "default" : "outline"}>
                {user.isActive ? "فعال" : "غیرفعال"}
              </Badge>
            </div>
            <div className="w-full space-y-2">
              <p className="text-xs text-muted-foreground">تصویر پروفایل</p>
              <ImageUploader
                value={user.avatarUrl || ""}
                onUpload={async (url) => {
                  // Note: This will need backend API support for user avatar updates
                  // For now, this is for frontend demonstration only
                  console.log("Avatar upload would be called with URL:", url);
                  return url;
                }}
                onRemove={async () => {
                  // Note: This will need backend API support for user avatar removal
                  console.log("Avatar removal would be called");
                }}
                folder="users"
                maxSizeMB={2}
              />
            </div>
          </CardContent>
        </Card>

        <Card className="lg:col-span-2">
          <CardHeader><CardTitle className="text-sm">اطلاعات کاربر</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 text-sm sm:grid-cols-2">
              <div className="flex items-center gap-2.5">
                <MailIcon className="size-4 shrink-0 text-muted-foreground" />
                <div className="min-w-0">
                  <p className="text-xs text-muted-foreground">ایمیل</p>
                  <p className="truncate font-medium" dir="ltr">{user.email}</p>
                </div>
              </div>
              <div className="flex items-center gap-2.5">
                <PhoneIcon className="size-4 shrink-0 text-muted-foreground" />
                <div className="min-w-0">
                  <p className="text-xs text-muted-foreground">تلفن</p>
                  <p className="truncate font-medium" dir="ltr">{user.phoneNumber || "—"}</p>
                </div>
              </div>
              <div className="flex items-center gap-2.5">
                <UserIcon className="size-4 shrink-0 text-muted-foreground" />
                <div className="min-w-0">
                  <p className="text-xs text-muted-foreground">نام کاربری</p>
                  <p className="truncate font-medium" dir="ltr">{user.username}</p>
                </div>
              </div>
              <div className="flex items-center gap-2.5">
                <CalendarIcon className="size-4 shrink-0 text-muted-foreground" />
                <div className="min-w-0">
                  <p className="text-xs text-muted-foreground">تاریخ عضویت</p>
                  <p className="font-medium">{formatDate(user.createdAt)}</p>
                </div>
              </div>
            </div>

            <div className="rounded-xl border bg-muted/30 p-4">
              <p className="mb-3 flex items-center gap-2 text-sm font-semibold">
                <ShieldIcon className="size-4 text-primary" />
                نقش و دسترسی
              </p>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <label className="text-xs text-muted-foreground">نقش کاربر</label>
                  <Select value={role} onValueChange={setRole}>
                    <SelectTrigger className="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Admin">مدیر</SelectItem>
                      <SelectItem value="Customer">مشتری</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <label className="text-xs text-muted-foreground">وضعیت حساب</label>
                  <Select value={isActive ? "1" : "0"} onValueChange={(v) => setIsActive(v === "1")}>
                    <SelectTrigger className="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="1">فعال</SelectItem>
                      <SelectItem value="0">غیرفعال</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <label className="text-xs text-muted-foreground">رمز عبور جدید (اختیاری)</label>
                  <input
                    type="password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="اگر می‌خواهید رمز تغییر کند"
                    className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  />
                </div>
              </div>
              <Button size="sm" className="mt-4 gap-2" onClick={handleSave} disabled={saving || (!changed && !newPassword.trim())}>
                {saving && <Loader2Icon className="size-3.5 animate-spin" />}
                ذخیره تغییرات
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}