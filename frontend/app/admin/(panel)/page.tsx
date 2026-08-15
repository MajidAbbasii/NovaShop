"use client"

import * as React from "react"
import Link from "next/link"
import { getDashboardStats, type DashboardStats } from "@/lib/admin-api"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { PageHeader } from "@/components/admin/page-header"
import { StatCard } from "@/components/admin/stat-card"
import { CardSkeleton } from "@/components/admin/skeletons"
import { ErrorState } from "@/components/admin/states"
import { OrderStatusBadge } from "@/components/admin/status-badge"
import { formatCurrency, formatDateShort } from "@/lib/admin-i18n"
import {
  UsersIcon,
  PackageIcon,
  DollarSignIcon,
  ClockIcon,
  TrendingUpIcon,
  ArrowLeftIcon,
} from "lucide-react"
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  AreaChart,
  Area,
} from "recharts"

export default function AdminDashboardPage() {
  const [stats, setStats] = React.useState<DashboardStats | null>(null)
  const [error, setError] = React.useState("")

  const fetch = React.useCallback(() => {
    getDashboardStats()
      .then((s) => { setStats(s); setError("") })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [])

  React.useEffect(() => { fetch() }, [fetch])

  if (error || !stats) {
    return (
      <div className="space-y-6">
        <PageHeader title="داشبورد" description="نمای کلی فروشگاه" />
        <Card>
          <ErrorState message="خطا در دریافت اطلاعات داشبورد" onRetry={fetch} />
        </Card>
      </div>
    )
  }

  if (!stats) {
    return (
      <div className="space-y-6">
        <PageHeader title="داشبورد" description="نمای کلی فروشگاه" />
        <CardSkeleton />
        <div className="grid gap-6 lg:grid-cols-2">
          <div className="h-72 animate-pulse rounded-xl border bg-card" />
          <div className="h-72 animate-pulse rounded-xl border bg-card" />
        </div>
      </div>
    )
  }

  const statCards = [
    {
      label: "کل کاربران",
      value: stats.totalUsers.toLocaleString("fa-IR"),
      icon: UsersIcon,
      hint: "کاربران ثبت‌نام‌شده",
    },
    {
      label: "کل سفارش‌ها",
      value: stats.totalOrders.toLocaleString("fa-IR"),
      icon: PackageIcon,
      hint: "همه سفارش‌ها",
    },
    {
      label: "درآمد",
      value: formatCurrency(stats.revenue),
      icon: DollarSignIcon,
      hint: "سفارش‌های ارسال‌شده و تحویل‌شده",
    },
    {
      label: "سفارش‌های در انتظار",
      value: stats.pendingOrders.toLocaleString("fa-IR"),
      icon: ClockIcon,
      hint: "نیازمند اقدام",
    },
  ]

  const chartData = stats.dailyRevenue.map((d) => ({
    label: formatDateShort(d.date),
    درآمد: d.revenue,
  }))

  const noRevenue = chartData.every((d) => d.درآمد === 0)

  return (
    <div className="space-y-6">
      <PageHeader
        title="داشبورد"
        description="نمای کلی عملکرد فروشگاه نووا‌شاپ"
        actions={
          <Button asChild variant="outline" size="sm">
            <Link href="/admin/orders">
              مشاهده سفارش‌ها
              <ArrowLeftIcon className="size-3.5" />
            </Link>
          </Button>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {statCards.map((card) => (
          <StatCard key={card.label} {...card} />
        ))}
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm">
              <TrendingUpIcon className="size-4 text-primary" />
              روند درآمد ۷ روز اخیر
            </CardTitle>
          </CardHeader>
          <CardContent>
            {noRevenue || chartData.length === 0 ? (
              <div className="flex h-64 flex-col items-center justify-center text-sm text-muted-foreground">
                <DollarSignIcon className="mb-2 size-8 text-muted-foreground/40" />
                درآمدی در ۷ روز اخیر ثبت نشده است
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <AreaChart data={chartData}>
                  <defs>
                    <linearGradient id="revenueGradient" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="var(--color-primary)" stopOpacity={0.35} />
                      <stop offset="95%" stopColor="var(--color-primary)" stopOpacity={0.02} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-border" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
                  <YAxis tick={{ fontSize: 11 }} tickLine={false} axisLine={false} width={70} />
                  <Tooltip
                    formatter={(v) => [formatCurrency(Number(v ?? 0)), "درآمد"]}
                    labelStyle={{ fontFamily: "Vazirmatn" }}
                  />
                  <Area
                    type="monotone"
                    dataKey="درآمد"
                    stroke="var(--color-primary)"
                    strokeWidth={2}
                    fill="url(#revenueGradient)"
                  />
                </AreaChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm">
              <PackageIcon className="size-4 text-primary" />
              سفارش‌های اخیر
            </CardTitle>
          </CardHeader>
          <CardContent>
            {stats.recentOrders.length === 0 ? (
              <div className="flex h-64 flex-col items-center justify-center text-sm text-muted-foreground">
                <PackageIcon className="mb-2 size-8 text-muted-foreground/40" />
                هنوز سفارشی ثبت نشده است
              </div>
            ) : (
              <ul className="divide-y">
                {stats.recentOrders.map((o) => (
                  <li key={o.id} className="flex items-center justify-between gap-3 py-2.5">
                    <div className="min-w-0">
                      <Link href={`/admin/orders`} className="text-sm font-semibold hover:text-primary">
                        سفارش #{o.id}
                      </Link>
                      <p className="text-xs text-muted-foreground">{formatDateShort(o.createdAt)}</p>
                    </div>
                    <div className="flex shrink-0 items-center gap-2">
                      <span className="text-sm font-bold tabular-nums">{formatCurrency(o.totalAmount)}</span>
                      <OrderStatusBadge status={o.status} />
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">توزیع درآمد روزانه</CardTitle>
        </CardHeader>
        <CardContent>
          {noRevenue ? (
            <div className="flex h-56 items-center justify-center text-sm text-muted-foreground">
              داده‌ای برای نمایش وجود ندارد
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" vertical={false} />
                <XAxis dataKey="label" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
                <YAxis tick={{ fontSize: 11 }} tickLine={false} axisLine={false} width={70} />
                <Tooltip
                  formatter={(v) => [formatCurrency(Number(v ?? 0)), "درآمد"]}
                  labelStyle={{ fontFamily: "Vazirmatn" }}
                  cursor={{ fill: "var(--color-muted)" }}
                />
                <Bar dataKey="درآمد" fill="var(--color-primary)" radius={[6, 6, 0, 0]} maxBarSize={40} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
