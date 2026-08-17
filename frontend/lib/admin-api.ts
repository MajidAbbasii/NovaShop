import { API_GATEWAY_URL } from '@/lib/config';

export const API_BASE_URL = API_GATEWAY_URL;

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface UserDto {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  avatarUrl?: string;
  address?: string;
  city?: string;
  postalCode?: string;
}

export interface OrderDto {
  id: number;
  userId: number;
  totalAmount: number;
  discountAmount: number;
  originalTotal: number;
  discountCode?: string;
  status: string;
  trackingCode?: string;
  trackingNumber?: string;
  shippingAddress: string;
  createdAt: string;
  items: OrderItemDto[];
  payment?: PaymentDto;
  statusHistory: OrderStatusHistoryDto[];
}

export interface OrderStatusHistoryDto {
  id: number;
  fromStatus: string;
  toStatus: string;
  note?: string;
  changedByUserId: number;
  changedByRole: string;
  changedAt: string;
}

export interface InventoryTransactionDto {
  id: number;
  productId: number;
  productName: string;
  orderId?: number;
  type: string;
  quantity: number;
  stockBefore: number;
  stockAfter: number;
  reference?: string;
  createdAt: string;
}

export interface SmsNotificationDto {
  id: number;
  orderId?: number;
  phoneNumber: string;
  eventType: string;
  message: string;
  provider: string;
  status: string;
  providerMessageId?: string;
  error?: string;
  createdAt: string;
  sentAt?: string;
}

export interface OrderItemDto {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface PaymentDto {
  id: number;
  amount: number;
  paymentMethod: string;
  status: string;
  transactionId: string;
  createdAt: string;
}

export interface DashboardStats {
  totalUsers: number;
  totalOrders: number;
  pendingOrders: number;
  revenue: number;
  recentOrders: {
    id: number;
    status: string;
    totalAmount: number;
    createdAt: string;
  }[];
  dailyRevenue: {
    date: string;
    revenue: number;
  }[];
}

export function authHeaders(): Record<string, string> {
  const token =
    typeof window !== 'undefined'
      ? (localStorage.getItem('token') || document.cookie.match(/(?:^|;\s*)token=([^;]*)/)?.[1])
      : null;
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function apiFetch<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${url}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...authHeaders(),
      ...options?.headers,
    },
  });
  if (!res.ok) {
    const err = await res.text().catch(() => res.statusText);
    throw new Error(`API ${res.status}: ${err}`);
  }
  if (res.status === 204 || res.status === 200 && options?.method === 'PUT') {
    return {} as T;
  }
  return res.json();
}

// Admin Users
export function getAdminUsers(params: {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  role?: string;
} = {}) {
  const query = new URLSearchParams();
  if (params.pageNumber) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  if (params.searchTerm) query.set('searchTerm', params.searchTerm);
  if (params.role) query.set('role', params.role);
  return apiFetch<PagedResult<UserDto>>(`/api/users?${query}`);
}

export function updateUser(id: number, data: {
  role?: string;
  isActive?: boolean;
  firstName?: string;
  lastName?: string;
  password?: string;
}) {
  return apiFetch<void>(`/api/users/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export function deleteUser(id: number) {
  return apiFetch<void>(`/api/users/${id}`, { method: 'DELETE' });
}

// Admin Orders
export function getAdminOrders(params: {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  status?: string;
} = {}) {
  const query = new URLSearchParams();
  if (params.pageNumber) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  if (params.searchTerm) query.set('searchTerm', params.searchTerm);
  if (params.status) query.set('status', params.status);
  return apiFetch<PagedResult<OrderDto>>(`/api/admin/orders?${query}`);
}

export function updateOrderStatus(orderId: number, status: string, note?: string) {
  return apiFetch<OrderDto>(`/api/admin/orders/${orderId}/status`, {
    method: 'PUT',
    body: JSON.stringify({ orderId, status, note }),
  });
}

export function getAdminOrder(id: number) {
  return apiFetch<OrderDto>(`/api/admin/orders/${id}`);
}

export function cancelOrder(orderId: number) {
  return apiFetch<OrderDto>(`/api/orders/${orderId}/cancel`, { method: 'POST' });
}

// Admin Inventory + SMS
export function getInventoryTransactions(params: {
  productId?: number;
  orderId?: number;
  type?: string;
  pageNumber?: number;
  pageSize?: number;
} = {}) {
  const query = new URLSearchParams();
  if (params.productId) query.set('productId', String(params.productId));
  if (params.orderId) query.set('orderId', String(params.orderId));
  if (params.type) query.set('type', params.type);
  if (params.pageNumber) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  return apiFetch<PagedResult<InventoryTransactionDto>>(`/api/admin/inventory?${query}`);
}

export function getSmsNotifications(params: {
  orderId?: number;
  status?: string;
  pageNumber?: number;
  pageSize?: number;
} = {}) {
  const query = new URLSearchParams();
  if (params.orderId) query.set('orderId', String(params.orderId));
  if (params.status) query.set('status', params.status);
  if (params.pageNumber) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  return apiFetch<PagedResult<SmsNotificationDto>>(`/api/admin/notifications/sms?${query}`);
}

export function getOrder(id: number) {
  return apiFetch<OrderDto>(`/api/orders/${id}`);
}

// --- Admin Reviews ---

export interface AdminReview {
  id: number;
  productId: number;
  productName: string;
  userId: number;
  userName: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export function getAdminReviews(params: {
  rating?: number;
  pageNumber?: number;
  pageSize?: number;
} = {}) {
  const query = new URLSearchParams();
  if (params.rating) query.set('rating', String(params.rating));
  if (params.pageNumber) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  return apiFetch<PagedResult<AdminReview>>(`/api/admin/reviews?${query}`);
}

export function deleteAdminReview(id: number) {
  return apiFetch<void>(`/api/reviews/${id}`, { method: 'DELETE' });
}

// Dashboard
export function getDashboardStats() {
  return apiFetch<DashboardStats>('/api/admin/dashboard');
}

// --- Admin Products ---

export interface AdminCategory {
  id: number;
  name: string;
  description?: string;
  imageUrl: string;
  parentCategoryId?: number;
}

export interface AdminProduct {
  id: number;
  name: string;
  description?: string;
  price: number;
  originalPrice?: number;
  imageUrl: string;
  primaryImageUrl?: string;
  rating: number;
  stock: number;
  isAvailable: boolean;
  categoryId?: number;
  category?: AdminCategory | null;
  images?: AdminProductImage[];
  colors?: AdminProductColor[];
}

export interface AdminProductImage {
  id: number;
  url: string;
  altText?: string;
  displayOrder: number;
  isPrimary: boolean;
  productColorId?: number | null;
}

export interface AdminProductColor {
  id: number;
  name: string;
  hexCode?: string;
  stock: number;
  isActive: boolean;
  price?: number | null;
}

export function getAdminProducts(params: {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  categoryId?: number;
  onlyAvailable?: boolean;
  minPrice?: number;
  maxPrice?: number;
} = {}) {
  const query = new URLSearchParams();
  if (params.pageNumber) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  if (params.searchTerm) query.set('searchTerm', params.searchTerm);
  if (params.categoryId) query.set('categoryId', String(params.categoryId));
  if (params.onlyAvailable !== undefined) query.set('onlyAvailable', String(params.onlyAvailable));
  if (params.minPrice !== undefined) query.set('minPrice', String(params.minPrice));
  if (params.maxPrice !== undefined) query.set('maxPrice', String(params.maxPrice));
  return apiFetch<PagedResult<AdminProduct>>(`/api/products?${query}`);
}

export function getAdminProduct(id: number) {
  return apiFetch<AdminProduct>(`/api/products/${id}`);
}

export function createProduct(data: {
  name: string;
  price: number;
  imageUrl: string;
  stock: number;
  description?: string;
  originalPrice?: number;
  categoryId: number;
  images?: { url: string; altText?: string; displayOrder: number; isPrimary: boolean; productColorId?: number | null }[];
  colors?: { name: string; hexCode?: string; stock: number; isActive: boolean; price?: number | null }[];
}) {
  return apiFetch<number>('/api/products', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export function updateProduct(id: number, data: {
  name?: string;
  description?: string;
  price?: number;
  originalPrice?: number;
  imageUrl?: string;
  stock?: number;
  categoryId?: number;
  images?: { url: string; altText?: string; displayOrder: number; isPrimary: boolean; productColorId?: number | null }[];
  colors?: { name: string; hexCode?: string; stock: number; isActive: boolean; price?: number | null }[];
}) {
  return apiFetch<void>(`/api/products/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export function deleteProduct(id: number) {
  return apiFetch<void>(`/api/products/${id}`, { method: 'DELETE' });
}

export function getAdminCategories() {
  return apiFetch<{ items: AdminCategory[] }>('/api/categories?pageSize=100');
}

export function createCategory(data: { name: string; description?: string; imageUrl?: string }) {
  return apiFetch<number>('/api/categories', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export function updateCategory(id: number, data: { name?: string; description?: string; imageUrl?: string }) {
  return apiFetch<void>(`/api/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export function deleteCategory(id: number) {
  return apiFetch<void>(`/api/categories/${id}`, { method: 'DELETE' });
}

export function getAdminUser(id: number) {
  return apiFetch<UserDto>(`/api/users/${id}`);
}

// --- Customer self-profile (current authenticated user) ---

export function getCurrentUser() {
  return apiFetch<UserDto>('/api/users/me');
}

export function updateProfile(data: {
  firstName?: string;
  lastName?: string;
  email?: string;
  phoneNumber?: string;
  address?: string;
  city?: string;
  postalCode?: string;
}) {
  return apiFetch<UserDto>('/api/users/me', {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

// --- Admin Discounts ---

export interface AdminDiscount {
  id: number;
  code: string;
  type: string;
  value: number;
  startDate: string;
  endDate: string;
  usageLimit: number;
  usedCount: number;
  minOrderAmount: number;
  applicableProductIds: number[];
  applicableCategoryIds: number[];
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export function getAdminDiscounts(params: {
  pageNumber?: number;
  pageSize?: number;
} = {}) {
  const query = new URLSearchParams();
  if (params.pageNumber) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  return apiFetch<PagedResult<AdminDiscount>>(`/api/admin/discounts?${query}`);
}

export function createDiscount(data: {
  code: string;
  type: string;
  value: number;
  startDate: string;
  endDate: string;
  usageLimit: number;
  minOrderAmount: number;
  applicableProductIds?: number[];
  applicableCategoryIds?: number[];
  isActive: boolean;
}) {
  return apiFetch<number>('/api/admin/discounts', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export function updateDiscount(id: number, data: {
  code: string;
  type: string;
  value: number;
  startDate: string;
  endDate: string;
  usageLimit: number;
  minOrderAmount: number;
  applicableProductIds?: number[];
  applicableCategoryIds?: number[];
  isActive: boolean;
}) {
  return apiFetch<void>(`/api/admin/discounts/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export function deleteDiscount(id: number) {
  return apiFetch<void>(`/api/admin/discounts/${id}`, { method: 'DELETE' });
}

// --- Admin Banners ---

export interface BannerDto {
  id: number;
  title: string;
  subtitle: string;
  imageUrl: string;
  linkUrl: string;
  isActive: boolean;
  sortOrder: number;
  createdAt?: string;
  updatedAt?: string;
}

export function getAdminBanners() {
  return apiFetch<{ items: BannerDto[] }>('/api/admin/banners');
}

export function createBanner(data: {
  title: string;
  subtitle?: string;
  imageUrl?: string;
  linkUrl?: string;
  isActive: boolean;
  sortOrder?: number;
}) {
  return apiFetch<number>('/api/admin/banners', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export function updateBanner(id: number, data: {
  title: string;
  subtitle?: string;
  imageUrl?: string;
  linkUrl?: string;
  isActive: boolean;
  sortOrder?: number;
}) {
  return apiFetch<void>(`/api/admin/banners/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export function deleteBanner(id: number) {
  return apiFetch<void>(`/api/admin/banners/${id}`, { method: 'DELETE' });
}

// --- Auth ---

export function adminLogout() {
  return apiFetch<void>('/api/auth/logout', { method: 'POST' }).catch(() => {});
}

// Throws with a Persian message; 401 triggers redirect handled by callers.
export async function apiFetchAuth<T>(url: string, options?: RequestInit): Promise<T> {
  try {
    return await apiFetch<T>(url, options);
  } catch (e: unknown) {
    const err = e as { message?: string };
    if (err?.message?.includes('401')) {
      if (typeof window !== 'undefined') {
        localStorage.removeItem('token');
        document.cookie = 'token=;path=/;max-age=0';
        window.location.href = '/admin/login';
      }
    }
    throw e;
  }
}

// ---- Translation Management ----
export interface TranslationRow {
  id: number;
  key: string;
  locale: string;
  value: string;
  namespace?: string | null;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface TranslationPage {
  items: TranslationRow[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface MissingKey {
  key: string;
  missingLocales: string[];
}
export interface MissingReport {
  supportedLocales: string[];
  totalKeys: number;
  missingCount: number;
  missing: MissingKey[];
}

export function getTranslations(params: {
  pageNumber?: number;
  pageSize?: number;
  locale?: string;
  namespace?: string;
  key?: string;
  search?: string;
  onlyMissing?: boolean;
} = {}) {
  const q = new URLSearchParams();
  if (params.pageNumber) q.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) q.set('pageSize', String(params.pageSize));
  if (params.locale) q.set('locale', params.locale);
  if (params.namespace) q.set('namespace', params.namespace);
  if (params.key) q.set('key', params.key);
  if (params.search) q.set('search', params.search);
  if (params.onlyMissing) q.set('onlyMissing', 'true');
  return apiFetch<TranslationPage>(`/api/admin/translations?${q.toString()}`);
}

export function getMissingTranslations() {
  return apiFetch<MissingReport>('/api/admin/translations/missing');
}

export function createTranslation(req: {
  key: string;
  namespace?: string;
  description?: string;
  values: { locale: string; value: string }[];
}) {
  return apiFetch<TranslationRow>('/api/admin/translations', {
    method: 'POST',
    body: JSON.stringify(req),
  });
}

export function updateTranslation(
  id: number,
  req: { value?: string; namespace?: string; description?: string; isActive?: boolean }
) {
  return apiFetch<TranslationRow>(`/api/admin/translations/${id}`, {
    method: 'PUT',
    body: JSON.stringify(req),
  });
}

export function deleteTranslation(id: number) {
  return apiFetch<void>(`/api/admin/translations/${id}`, { method: 'DELETE' });
}