// frontend/components/product-card.tsx
import Image from "next/image";
import { Card, CardContent, CardFooter } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Heart, ShoppingCart, Star } from "lucide-react";

interface Product {
  id: number;
  name: string;
  price: number;
  originalPrice?: number;
  image: string;
  rating: number;
  discount?: number;
  inStock: boolean;
}

export default function ProductCard({ product }: { product: Product }) {
  const discountPercent = product.discount
    ? Math.round(((product.originalPrice! - product.price) / product.originalPrice!) * 100)
    : 0;

  return (
    <Card className="group overflow-hidden border border-gray-200 hover:border-primary transition-all duration-300 hover:shadow-xl">
      <div className="relative">
<Image
  src={product.image && product.image.startsWith('http')
    ? product.image
    : "https://picsum.photos/id/20/600/600"}
  alt={product.name || "Product"}
  width={300}
  height={300}
  className="w-full h-64 object-cover transition-transform group-hover:scale-105"
/>

        {product.discount && (
          <Badge className="absolute top-3 right-3 bg-red-500 hover:bg-red-600">
            {discountPercent}% OFF
          </Badge>
        )}

        <Button
          variant="ghost"
          size="icon"
          className="absolute top-3 left-3 opacity-0 group-hover:opacity-100 transition-opacity"
        >
          <Heart className="h-5 w-5" />
        </Button>
      </div>

      <CardContent className="p-4">
        <h3 className="font-semibold text-lg line-clamp-2 mb-2">{product.name}</h3>

        <div className="flex items-center gap-1 mb-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <Star
              key={i}
              className={`h-4 w-4 ${i < Math.floor(product.rating) ? "fill-yellow-400 text-yellow-400" : "text-gray-300"}`}
            />
          ))}
          <span className="text-sm text-gray-500 ml-1">({product.rating})</span>
        </div>

        <div className="flex items-baseline gap-2">
          <span className="text-2xl font-bold text-primary">
            ${product.price.toFixed(2)}
          </span>
          {product.originalPrice && (
            <span className="text-sm text-gray-500 line-through">
              ${product.originalPrice.toFixed(2)}
            </span>
          )}
        </div>
      </CardContent>

      <CardFooter className="p-4 pt-0">
        <Button
          className="w-full gap-2"
          disabled={!product.inStock}
        >
          <ShoppingCart className="h-5 w-5" />
          {product.inStock ? "Add to Cart" : "Out of Stock"}
        </Button>
      </CardFooter>
    </Card>
  );
}
