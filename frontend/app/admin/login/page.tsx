"use client"

import * as React from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { StoreIcon, Loader2Icon, LockIcon, UserIcon } from "lucide-react"
import { apiFetch } from "@/lib/admin-api"

export default function AdminLoginPage() {
  const router = useRouter()
  const [username, setUsername] = React.useState("")
  const [password, setPassword] = React.useState("")
  const [loading, setLoading] = React.useState(false)
  const [error, setError] = React.useState("")

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError("")
    try {
      const data = await apiFetch<{ token: string }>("/api/auth/login", {
        method: "POST",
        body: JSON.stringify({ username, password }),
      })
      if (!data?.token) throw new Error("پاسخی از سرور دریافت نشد")
      localStorage.setItem("token", data.token)
      localStorage.setItem("admin-username", username)
      document.cookie = `token=${data.token};path=/;max-age=28800`
      router.push("/admin")
    } catch (err) {
      setError(
        err instanceof Error && err.message.includes("401")
          ? "نام کاربری یا رمز عبور نادرست است"
          : "خطا در ارتباط با سرور. لطفاً دوباره تلاش کنید."
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <div dir="rtl" className="flex min-h-screen items-center justify-center bg-gradient-to-br from-background via-muted/50 to-primary/10 px-4">
      <Card className="w-full max-w-md border-0 shadow-xl">
        <CardHeader className="text-center">
          <div className="mx-auto mb-3 flex h-14 w-14 items-center justify-center rounded-2xl bg-primary text-primary-foreground">
            <StoreIcon className="size-7" />
          </div>
          <CardTitle className="text-xl">پنل مدیریت نووا‌شاپ</CardTitle>
          <CardDescription>برای ادامه، وارد حساب مدیریتی خود شوید</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4" noValidate>
            <div className="space-y-2">
              <Label htmlFor="admin-username">نام کاربری</Label>
              <div className="relative">
                <UserIcon className="absolute right-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  id="admin-username"
                  className="pr-9"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  required
                  autoComplete="username"
                  disabled={loading}
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="admin-password">رمز عبور</Label>
              <div className="relative">
                <LockIcon className="absolute right-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  id="admin-password"
                  type="password"
                  className="pr-9"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  autoComplete="current-password"
                  disabled={loading}
                />
              </div>
            </div>
            {error && (
              <p role="alert" className="rounded-lg bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {error}
              </p>
            )}
            <Button type="submit" className="w-full gap-2" disabled={loading}>
              {loading && <Loader2Icon className="size-4 animate-spin" />}
              {loading ? "در حال ورود..." : "ورود به پنل"}
            </Button>
            <p className="text-center text-xs text-muted-foreground">
              <Link href="/" className="hover:text-primary hover:underline">
                بازگشت به فروشگاه
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
