'use client';

import { useState, useEffect } from 'react';
import { getCategories, type Category } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { ChevronDown, ChevronRight, X } from 'lucide-react';
import { useLocale } from '@/lib/locale-context';

interface CategoryFilterProps {
  selectedId: number | null;
  onSelect: (id: number | null) => void;
}

export function CategoryFilter({ selectedId, onSelect }: CategoryFilterProps) {
  const { t } = useLocale();
  const [categories, setCategories] = useState<Category[]>([]);
  const [expanded, setExpanded] = useState<Set<number>>(new Set());
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getCategories()
      .then(setCategories)
      .catch(() => setCategories([]))
      .finally(() => setLoading(false));
  }, []);

  const rootCats = categories.filter((c) => !c.parentCategoryId);
  const childrenMap = new Map<number, Category[]>();
  for (const c of categories) {
    if (c.parentCategoryId) {
      const arr = childrenMap.get(c.parentCategoryId) ?? [];
      arr.push(c);
      childrenMap.set(c.parentCategoryId, arr);
    }
  }

  const toggleExpand = (id: number) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const renderCategory = (cat: Category, depth: number) => {
    const children = childrenMap.get(cat.id) ?? [];
    const isExpanded = expanded.has(cat.id);
    const isSelected = selectedId === cat.id;
    const hasChildren = children.length > 0;

    return (
      <div key={cat.id}>
        <button
          type="button"
          onClick={() => {
            if (hasChildren) toggleExpand(cat.id);
            onSelect(isSelected ? null : cat.id);
          }}
          className={cn(
            'flex w-full items-center gap-1.5 rounded-md px-2 py-1.5 text-left text-sm transition-colors hover:bg-muted',
            isSelected && 'bg-primary/10 font-medium text-primary',
            depth > 0 && 'pl-6'
          )}
        >
          {hasChildren && (
            <span className="shrink-0 text-muted-foreground">
              {isExpanded ? (
                <ChevronDown className="size-3.5" />
              ) : (
                <ChevronRight className="size-3.5" />
              )}
            </span>
          )}
          {!hasChildren && <span className="shrink-0 w-3.5" />}
          <span className="truncate">{cat.name}</span>
        </button>

        {hasChildren && isExpanded && (
          <div>
            {children.map((child) => renderCategory(child, depth + 1))}
          </div>
        )}
      </div>
    );
  };

  if (loading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="h-8 animate-pulse rounded-md bg-muted" />
        ))}
      </div>
    );
  }

  if (categories.length === 0) return null;

  return (
    <div className="space-y-1">
      <div className="flex items-center justify-between px-2 py-1">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          {t('header.categories')}
        </h3>
        {selectedId && (
          <Button
            variant="ghost"
            size="icon-xs"
            onClick={() => onSelect(null)}
            aria-label={t('category.clear')}
            className="size-5 text-muted-foreground hover:text-foreground"
          >
            <X className="size-3" />
          </Button>
        )}
      </div>

      {rootCats.map((cat) => renderCategory(cat, 0))}
    </div>
  );
}
