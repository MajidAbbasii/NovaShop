import { NextResponse } from 'next/server';

import { API_GATEWAY_URL } from '@/lib/config';

const API_BASE_URL = API_GATEWAY_URL;

export async function GET(
  _request: Request,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params;

  try {
    const res = await fetch(`${API_BASE_URL}/api/products`, {
      next: { revalidate: 60 },
    });
    if (!res.ok) {
      return NextResponse.json({ error: 'Failed to fetch products' }, { status: 502 });
    }

    const paged = await res.json();
    const product = (paged.items ?? []).find((p: { id: string | number }) => String(p.id) === id);
    if (!product) {
      return NextResponse.json({ error: 'Product not found' }, { status: 404 });
    }

    const [catRes, revRes] = await Promise.allSettled([
      product.categoryId
        ? fetch(`${API_BASE_URL}/api/categories/${product.categoryId}`, {
            next: { revalidate: 120 },
          })
        : null,
      fetch(`${API_BASE_URL}/api/products/${product.id}/reviews`, {
        next: { revalidate: 60 },
      }),
    ]);

    const category = product.categoryId && catRes?.status === 'fulfilled' && catRes.value?.ok
      ? await catRes.value.json()
      : null;

    const reviews = revRes?.status === 'fulfilled' && revRes.value?.ok
      ? await revRes.value.json()
      : [];

    return NextResponse.json({
      ...product,
      category,
      reviews,
    });
  } catch {
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}
