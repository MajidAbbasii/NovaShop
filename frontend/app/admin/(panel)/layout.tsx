"use client"

import * as React from "react"
import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import {
  LayoutDashboardIcon,
  PackageIcon,
  ShoppingCartIcon,
  UsersIcon,
  TagsIcon,
  LogOutIcon,
  StoreIcon,
  MenuIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  BellIcon,
    BoxesIcon,
        MessageSquareIcon,
              StarIcon,
              ImageIcon,
              CameraIcon,
              LanguagesIcon,
              TruckIcon,
            } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { Toaster } from "@/components/ui/toaster"
import { adminLogout } from "@/lib/admin-api"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { useLocale } from "@/lib/locale-context"
import { LanguageSwitcher } from "@/components/language-switcher"

interface NavItem {
  href: string
  labelKey: string
  icon: React.ComponentType<{ className?: string }>
}

const NAV_GROUPS: { titleKey: string; items: NavItem[] }[] = [
  {
    titleKey: "admin.management",
    items: [
      { href: "/admin", labelKey: "admin.dashboard", icon: LayoutDashboardIcon },
    ],
  },
  {
    titleKey: "admin.store",
    items: [
      { href: "/admin/products", labelKey: "admin.products.title", icon: PackageIcon },
      { href: "/admin/categories", labelKey: "admin.categories.title", icon: TagsIcon },
            { href: "/admin/banners", labelKey: "admin.banners.title", icon: ImageIcon },
            { href: "/admin/discounts", labelKey: "admin.discounts.title", icon: TagsIcon },
    ],
  },
  {
    titleKey: "admin.sales",
    items: [
      { href: "/admin/orders", labelKey: "admin.orders.title", icon: ShoppingCartIcon },
            { href: "/admin/custom-doll-requests", labelKey: "admin.customDoll.title", icon: CameraIcon },
            { href: "/admin/reviews", labelKey: "admin.reviews.title", icon: StarIcon },
            { href: "/admin/inventory", labelKey: "admin.inventory.title", icon: BoxesIcon },
            { href: "/admin/notifications", labelKey: "admin.notifications.title", icon: MessageSquareIcon },
    ],
  },
  {
    titleKey: "admin.usersNav",
    items: [
      { href: "/admin/users", labelKey: "admin.users.title", icon: UsersIcon },
    ],
  },
  {
    titleKey: "admin.settings.title",
    items: [
      { href: "/admin/translations", labelKey: "admin.translations.title", icon: LanguagesIcon },
      { href: "/admin/shipping-settings", labelKey: "shippingSettings", icon: TruckIcon },
    ],
  },
]

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname()
  const router = useRouter()
  const { t, dir } = useLocale()
  const [collapsed, setCollapsed] = React.useState(false)
  const [mobileOpen, setMobileOpen] = React.useState(false)
  const username = React.useSyncExternalStore(
    () => () => {},
    () => localStorage.getItem("admin-username") || localStorage.getItem("username") || t('admin.brand'),
    () => t('admin.brand')
  )

  React.useEffect(() => {
    const token =
      localStorage.getItem("token") ||
      document.cookie.match(/(?:^|;\\s*)token=([^;]*)/)?.[1]
    if (!token) {
      router.replace("/admin/login")
    }
  }, [router])

  const handleLogout = async () => {
    await adminLogout()
    localStorage.removeItem("token")
    localStorage.removeItem("admin-username")
    document.cookie = "token=;path=/;max-age=0"
    router.push("/admin/login")
  }

  const isActive = (href: string) =>
    href === "/admin" ? pathname === "/admin" : pathname.startsWith(href)

  const sidebar = (
    <>
      <div className="flex h-16 items-center gap-2 border-b px-4">
        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-primary text-primary-foreground">
          <StoreIcon className="size-5" />
        </div>
        {!collapsed && (
          <div className="min-w-0">
            <p className="truncate text-sm font-bold">{t('admin.brand')}</p>
            <p className="truncate text-[11px] text-muted-foreground">{t('admin.title')}</p>
          </div>
        )}
      </div>
      <nav className="flex-1 overflow-y-auto p-3">
        {NAV_GROUPS.map((group) => (
          <div key={group.titleKey} className="mb-4">
            {!collapsed && (
              <p className="mb-1 px-2 text-[11px] font-medium uppercase tracking-wide text-muted-foreground/70">
                {t(group.titleKey)}
              </p>
            )}
            <div className="space-y-0.5">
              {group.items.map((item) => {
                const active = isActive(item.href)
                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    onClick={() => setMobileOpen(false)}
                    title={collapsed ? t(item.labelKey) : undefined}
                    className={cn(
                      "flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-sm font-medium transition-colors",
                      active
                        ? "bg-primary/10 text-primary"
                        : "text-muted-foreground hover:bg-muted hover:text-foreground"
                    )}
                  >
                    <item.icon className="size-4 shrink-0" />
                    {!collapsed && t(item.labelKey)}
                  </Link>
                )
              })}
            </div>
          </div>
        ))}
      </nav>
      <Separator />
      <div className="p-3">
        <Link
          href="/"
          className={cn(
            "flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-sm text-muted-foreground transition-colors hover:bg-muted hover:text-foreground",
            collapsed && "justify-center"
          )}
        >
          <StoreIcon className="size-4 shrink-0" />
          {!collapsed && t('header.backToStore')}
        </Link>
      </div>
    </>
  )

  return (
    <div dir={dir} className="flex min-h-screen bg-muted/30">
      {/* Desktop sidebar */}
      <aside
        className={cn(
          "sticky top-0 hidden h-screen shrink-0 border-l bg-background transition-all duration-200 lg:flex lg:flex-col",
          collapsed ? "w-[68px]" : "w-64"
        )}
      >
        {sidebar}
      </aside>

      {/* Mobile sidebar */}
      {mobileOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div
            className="absolute inset-0 bg-black/50"
            onClick={() => setMobileOpen(false)}
          />
          <aside className="absolute right-0 top-0 flex h-full w-72 flex-col border-l bg-background">
            {sidebar}
          </aside>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        {/* Top bar */}
        <header className="sticky top-0 z-40 flex h-16 items-center gap-3 border-b bg-background/95 px-4 backdrop-blur">
          <Button
            variant="ghost"
            size="icon-sm"
            className="lg:hidden"
            onClick={() => setMobileOpen(true)}
            aria-label={t('admin.sidebar.open')}
          >
            <MenuIcon className="size-5" />
          </Button>
          <Button
            variant="ghost"
            size="icon-sm"
            className="hidden lg:inline-flex"
            onClick={() => setCollapsed(!collapsed)}
            aria-label={collapsed ? t('admin.sidebar.open') : t('admin.sidebar.toggle')}
          >
            {collapsed ? <ChevronLeftIcon className="size-4" /> : <ChevronRightIcon className="size-4" />}
          </Button>

          <div className="ms-auto flex items-center gap-1.5">
            <LanguageSwitcher />
            <Button variant="ghost" size="icon-sm" aria-label={t('admin.notifications')}>
              <BellIcon className="size-4" />
            </Button>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="h-9 gap-2 px-2">
                  <Avatar className="size-7">
                    <AvatarFallback className="bg-primary/10 text-xs text-primary">
                      {username.slice(0, 1).toUpperCase()}
                    </AvatarFallback>
                  </Avatar>
                  <span className="hidden text-sm font-medium sm:inline">{username}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-48">
                <DropdownMenuLabel>{t('admin.title')}</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={handleLogout} className="text-destructive focus:text-destructive">
                  <LogOutIcon className="size-4" />
                  {t('admin.logout')}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </header>

        <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8">
          <div className="mx-auto w-full max-w-7xl">{children}</div>
        </main>
      </div>
      <Toaster />
    </div>
  )
}
