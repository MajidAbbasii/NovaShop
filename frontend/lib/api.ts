import { API_GATEWAY_URL } from '@/lib/config';

const BACKEND_URL = API_GATEWAY_URL;
const APP_URL = 'http://localhost:3000';

// --- Types ---

export interface Product {
  id: number;
  name: string;
  description?: string;
  price: number;
  originalPrice?: number;
  imageUrl: string;
  rating: number;
  stock: number;
  isAvailable: boolean;
  categoryId?: number;
  category?: Category;
  reviews?: ReviewEmbed[];
  imageUrls?: string[];
  images?: ProductImage[];
  colors?: ProductColor[];
  specifications?: Record<string, string>;
}

export interface ProductImage {
  id: number;
  url: string;
  altText?: string;
  displayOrder: number;
  isPrimary: boolean;
  productColorId?: number | null;
}

export interface ProductColor {
  id: number;
  name: string;
  hexCode?: string;
  stock: number;
  isActive: boolean;
  price?: number | null;
  images?: ProductImage[];
}

export interface Category {
  id: number;
  name: string;
  description?: string;
  imageUrl: string;
  parentCategoryId?: number;
}

export interface ReviewEmbed {
  id: number;
  productId: number;
  userId: number;
  userName?: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface PagedResult<T = Product> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// --- SSR-safe fetch (no window dependency) ---

const ssrOrigin = API_GATEWAY_URL;

/** Fetch a single product with category + reviews embedded. SSR-safe. */
export async function getProductById(id: number): Promise<Product | null> {
  const res = await fetch(`${ssrOrigin}/api/products/${id}`, {
    next: { revalidate: 60 },
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Failed to fetch product ${id}: ${res.status}`);
  return res.json();
}

/** Fetch paged products from backend (SSR-safe). */
export async function getProducts(
  params: {
    searchTerm?: string;
    minPrice?: number;
    maxPrice?: number;
    onlyAvailable?: boolean;
    categoryId?: number;
    pageNumber?: number;
    pageSize?: number;
  } = {}
): Promise<PagedResult<Product>> {
  const url = new URL(`${BACKEND_URL}/api/products`);
  if (params.searchTerm) url.searchParams.set('searchTerm', params.searchTerm.trim());
  if (params.minPrice !== undefined) url.searchParams.set('minPrice', String(params.minPrice));
  if (params.maxPrice !== undefined) url.searchParams.set('maxPrice', String(params.maxPrice));
  if (params.onlyAvailable !== undefined) url.searchParams.set('onlyAvailable', String(params.onlyAvailable));
  if (params.categoryId !== undefined) url.searchParams.set('categoryId', String(params.categoryId));
  if (params.pageNumber) url.searchParams.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) url.searchParams.set('pageSize', String(params.pageSize));

  const res = await fetch(url, { next: { revalidate: 60 } });
  if (!res.ok) throw new Error(`Failed to fetch products: ${res.status}`);
  return res.json();
}

/** Fetch related products (same category) for SSR. */
export async function getRelatedProducts(
  categoryId?: number,
  excludeId?: number,
  limit = 8
): Promise<Product[]> {
  try {
    const paged = await getProducts({ onlyAvailable: true, pageSize: 50 });
    let items = paged.items ?? [];
    if (categoryId) {
      const sameCat = items.filter((p) => p.categoryId === categoryId);
      if (sameCat.length > 0) items = sameCat;
    }
    if (excludeId !== undefined) items = items.filter((p) => p.id !== excludeId);
    return items.slice(0, limit);
  } catch {
    return [];
  }
}

/** Fetch category by id (SSR-safe). */
export async function getCategory(id: number): Promise<Category | null> {
  try {
    const res = await fetch(`${BACKEND_URL}/api/categories/${id}`, {
      next: { revalidate: 120 },
    });
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

/** Fetch all categories (SSR-safe). */
export async function getCategories(): Promise<Category[]> {
  try {
    const res = await fetch(`${BACKEND_URL}/api/categories`, {
      next: { revalidate: 120 },
    });
    if (!res.ok) return [];
    const data = await res.json();
    return data.items ?? data;
  } catch {
    return [];
  }
}
