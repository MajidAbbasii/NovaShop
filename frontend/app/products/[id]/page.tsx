import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { getProductById, getRelatedProducts, getCategory } from '@/lib/api';
import ProductDetailClient from './client-page';

interface PageProps {
  params: Promise<{ id: string }>;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { id } = await params;
  const product = await getProductById(Number(id));

  if (!product) {
    return { title: 'Product Not Found — NovaShop' };
  }

  const title = `${product.name} — NovaShop`;
  const description = product.description ?? `${product.name} — NovaShop`;
  const images = product.imageUrl ? [product.imageUrl] : [];

  return {
    title,
    description,
    openGraph: {
      title,
      description,
      images,
      type: 'website',
      siteName: 'NovaShop',
    },
    twitter: {
      card: 'summary_large_image',
      title,
      description,
      images,
    },
    alternates: {
      canonical: `/products/${id}`,
    },
  };
}

export default async function ProductDetailPage({ params }: PageProps) {
  const { id } = await params;
  const productId = Number(id);

  const product = await getProductById(productId);
  if (!product) notFound();

  // Fetch category for breadcrumb if not already embedded
  const [category, related] = await Promise.all([
    product.category ? Promise.resolve(product.category) : product.categoryId
      ? getCategory(product.categoryId).catch(() => null)
      : Promise.resolve(null),
    getRelatedProducts(product.categoryId, product.id, 8),
  ]);

  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'Product',
    name: product.name,
    description: product.description,
    image: product.imageUrls?.length ? product.imageUrls : product.imageUrl,
    sku: String(product.id),
    mpn: String(product.id),
    brand: { '@type': 'Brand', name: 'NovaShop' },
    offers: {
      '@type': 'Offer',
      price: product.price,
      priceCurrency: 'IRT',
      availability: product.stock > 0
        ? 'https://schema.org/InStock'
        : 'https://schema.org/OutOfStock',
      itemCondition: 'https://schema.org/NewCondition',
      url: `/products/${product.id}`,
    },
    ...(category?.name ? { category: category.name } : {}),
    aggregateRating: product.rating
      ? {
          '@type': 'AggregateRating',
          ratingValue: product.rating,
          bestRating: 5,
          ratingCount: product.reviews?.length ?? 0,
        }
      : undefined,
  };

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd, null, 2) }}
      />
      <ProductDetailClient
        product={product}
        category={category}
        initialRelatedProducts={related}
      />
    </>
  );
}
