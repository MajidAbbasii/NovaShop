'use client';

import Link from 'next/link';
import { ChevronRight, Home } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useLocale } from '@/lib/locale-context';

interface Crumb {
  label: string;
  href?: string;
}

interface BreadcrumbProps {
  items: Crumb[];
}

export function Breadcrumb({ items }: BreadcrumbProps) {
  const { t } = useLocale();
  const all: Crumb[] = [{ label: t('header.home'), href: '/' }, ...items];

  return (
    <nav aria-label={t('common.breadcrumb')} className="flex items-center gap-1 text-sm text-muted-foreground">
      {all.map((item, i) => {
        const isLast = i === all.length - 1;
        return (
          <span key={i} className="flex items-center gap-1">
            {i === 0 && <Home className="size-3.5" />}
            {item.href && !isLast ? (
              <Link
                href={item.href}
                className="hover:text-foreground transition-colors"
              >
                {item.label}
              </Link>
            ) : (
              <span
                className={cn(
                  isLast ? 'text-foreground font-medium' : ''
                )}
              >
                {item.label}
              </span>
            )}
            {!isLast && <ChevronRight className="size-3.5" />}
          </span>
        );
      })}
    </nav>
  );
}
