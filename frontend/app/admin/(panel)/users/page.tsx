"use client"

import * as React from "react"
import { useRouter } from "next/navigation"
import {
  getAdminUsers,
  updateUser,
  deleteUser,
  type UserDto,
  type PagedResult,
} from "@/lib/admin-api"
import { toast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from "@/components/ui/dialog"
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { PageHeader } from "@/components/admin/page-header"
import { Pagination } from "@/components/admin/pagination"
import { TableSkeleton } from "@/components/admin/skeletons"
import { EmptyState, ErrorState } from "@/components/admin/states"
import { ConfirmDialog } from "@/components/admin/confirm-dialog"
import { formatDateShort } from "@/lib/admin-i18n"
import {
  MoreHorizontalIcon, SearchIcon, XIcon, EyeIcon, PencilIcon, Trash2Icon, Loader2Icon, CameraIcon,
} from "lucide-react"

const PAGE_SIZE = 10

export default function AdminUsersPage() {
  const router = useRouter()
  const [data, setData] = React.useState<PagedResult<UserDto> | null>(null)
  const [error, setError] = React.useState("")
  const [search, setSearch] = React.useState("")
  const [roleFilter, setRoleFilter] = React.useState("all")
  const [page, setPage] = React.useState(1)
  const [editUser, setEditUser] = React.useState<UserDto | null>(null)
  const [editRole, setEditRole] = React.useState("")
  const [editActive, setEditActive] = React.useState(true)
  const [saving, setSaving] = React.useState(false)
  const [deleteTarget, setDeleteTarget] = React.useState<UserDto | null>(null)
  const [deleting, setDeleting] = React.useState(false)

  const fetch = React.useCallback(() => {
    getAdminUsers({
      pageNumber: page,
      pageSize: PAGE_SIZE,
      searchTerm: search || undefined,
      role: roleFilter === "all" ? undefined : roleFilter,
    })
      .then((d) => { setData(d); setError("") })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : "خطا"))
  }, [page, search, roleFilter])

  React.useEffect(() => { fetch() }, [fetch])

  const clearFilters = () => {
    setSearch("")
    setRoleFilter("all")
    setPage(1)
  }

  const hasFilters = search !== "" || roleFilter !== "all"

  const openEdit = (u: UserDto) => {
    setEditUser(u)
    setEditRole(u.role)
    setEditActive(u.isActive)
  }

  const handleSaveEdit = async () => {
    if (!editUser) return
    setSaving(true)
    try {
      await updateUser(editUser.id, { role: editRole, isActive: editActive })
      toast({ title: "کاربر به‌روزرسانی شد" })
      setEditUser(null)
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

  const handleDelete = async () => {
    if (!deleteTarget) return
    setDeleting(true)
    try {
      await deleteUser(deleteTarget.id)
      toast({ title: "کاربر حذف شد" })
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
        title="کاربران"
        description={`مدیریت ${data?.totalCount.toLocaleString("fa-IR") ?? ""} کاربر سیستم`}
        breadcrumbs={[{ label: "داشبورد", href: "/admin" }, { label: "کاربران" }]}
      />

      <div className="flex flex-wrap items-center gap-2">
        <div className="relative min-w-52 flex-1">
          <SearchIcon className="absolute right-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="جستجوی کاربر..."
            className="pr-8"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
          />
        </div>
        <Select value={roleFilter} onValueChange={(v) => { setRoleFilter(v); setPage(1) }}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="همه نقش‌ها" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">همه نقش‌ها</SelectItem>
            <SelectItem value="Admin">مدیر</SelectItem>
            <SelectItem value="Customer">مشتری</SelectItem>
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button variant="ghost" size="sm" onClick={clearFilters}>
            <XIcon className="size-3.5" />
            پاک‌کردن فیلترها
          </Button>
        )}
      </div>

      <div className="overflow-hidden rounded-xl border bg-card">
        {error ? (
          <ErrorState message="خطا در دریافت کاربران" onRetry={fetch} />
        ) : !data ? (
          <div className="p-4"><TableSkeleton rows={6} cols={5} /></div>
        ) : !data?.items.length ? (
          <EmptyState
            title={hasFilters ? "کاربری مطابق فیلترها یافت نشد" : "هنوز کاربری ثبت نشده است"}
            description={hasFilters ? "فیلترها را تغییر دهید یا پاک کنید" : undefined}
            actionLabel={hasFilters ? "پاک‌کردن فیلترها" : undefined}
            onAction={hasFilters ? clearFilters : undefined}
          />
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>کاربر</TableHead>
                  <TableHead>نام و نام خانوادگی</TableHead>
                  <TableHead>نقش</TableHead>
                  <TableHead>وضعیت</TableHead>
                  <TableHead>تاریخ عضویت</TableHead>
                  <TableHead className="w-12" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.map((u) => (
                  <TableRow key={u.id} className="cursor-pointer" onClick={() => router.push(`/admin/users/${u.id}`)}>
                    <TableCell>
                      <div className="flex items-center gap-2.5">
                        <Avatar className="size-8">
                          <AvatarFallback className="bg-primary/10 text-xs text-primary">
                            {(u.firstName || u.username).slice(0, 1).toUpperCase()}
                          </AvatarFallback>
                        </Avatar>
                        <div className="min-w-0">
                          <p className="text-sm font-medium">{u.username}</p>
                          <p className="truncate text-xs text-muted-foreground" dir="ltr">{u.email}</p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {[u.firstName, u.lastName].filter(Boolean).join(" ") || "—"}
                    </TableCell>
                    <TableCell>
                      <Badge variant={u.role === "Admin" ? "default" : "secondary"}>
                        {u.role === "Admin" ? "مدیر" : "مشتری"}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={u.isActive ? "default" : "outline"}>
                        {u.isActive ? "فعال" : "غیرفعال"}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">{formatDateShort(u.createdAt)}</TableCell>
                    <TableCell onClick={(e) => e.stopPropagation()}>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="icon-sm" aria-label="عملیات">
                            <MoreHorizontalIcon className="size-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end" className="w-40">
                          <DropdownMenuItem onClick={() => router.push(`/admin/users/${u.id}`)}>
                            <EyeIcon className="size-4" />
                            مشاهده
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => openEdit(u)}>
                            <PencilIcon className="size-4" />
                            ویرایش نقش/وضعیت
                          </DropdownMenuItem>
                          <DropdownMenuItem
                            className="text-destructive focus:text-destructive"
                            onClick={() => setDeleteTarget(u)}
                          >
                            <Trash2Icon className="size-4" />
                            حذف
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
        {data && data.items.length > 0 && (
          <Pagination page={page} totalPages={data.totalPages} totalCount={data.totalCount} pageSize={PAGE_SIZE} onChange={setPage} />
        )}
      </div>

      {/* Edit role/status dialog */}
      <Dialog open={!!editUser} onOpenChange={(o) => !o && setEditUser(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>ویرایش کاربر «{editUser?.username}»</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="u-role">نقش</Label>
              <Select value={editRole} onValueChange={setEditRole}>
                <SelectTrigger id="u-role" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Admin">مدیر</SelectItem>
                  <SelectItem value="Customer">مشتری</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="u-active">وضعیت</Label>
              <Select value={editActive ? "1" : "0"} onValueChange={(v) => setEditActive(v === "1")}>
                <SelectTrigger id="u-active" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">فعال</SelectItem>
                  <SelectItem value="0">غیرفعال</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditUser(null)} disabled={saving}>
              انصراف
            </Button>
            <Button onClick={handleSaveEdit} disabled={saving}>
              {saving && <Loader2Icon className="size-4 animate-spin" />}
              ذخیره
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="حذف کاربر"
        description={`آیا از حذف کاربر «${deleteTarget?.username}» مطمئن هستید؟ این عملیات قابل بازگشت نیست.`}
        confirmLabel="حذف"
        loading={deleting}
        onConfirm={handleDelete}
      />
    </div>
  )
}
