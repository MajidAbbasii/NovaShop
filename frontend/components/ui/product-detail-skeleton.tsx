export function ProductDetailSkeleton() {
  return (
    <div className="mx-auto max-w-6xl space-y-6 px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex items-center gap-2">
        <div className="h-4 w-16 animate-pulse rounded bg-muted" />
        <div className="h-3 w-3" />
        <div className="h-4 w-20 animate-pulse rounded bg-muted" />
        <div className="h-3 w-3" />
        <div className="h-4 w-32 animate-pulse rounded bg-muted" />
      </div>

      <div className="rounded-xl border bg-card p-6">
        <div className="grid gap-8 lg:grid-cols-2">
          <div className="aspect-square animate-pulse rounded-xl bg-muted" />

          <div className="flex flex-col gap-4">
            <div className="h-7 w-3/4 animate-pulse rounded bg-muted" />
            <div className="h-4 w-1/3 animate-pulse rounded bg-muted" />

            <div className="flex items-center gap-2">
              <div className="flex gap-0.5">
                {Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="size-4 animate-pulse rounded bg-muted" />
                ))}
              </div>
              <div className="h-4 w-20 animate-pulse rounded bg-muted" />
            </div>

            <div className="h-8 w-28 animate-pulse rounded bg-muted" />

            <div className="h-px w-full bg-border" />

            <div className="space-y-2">
              <div className="h-3 w-full animate-pulse rounded bg-muted" />
              <div className="h-3 w-5/6 animate-pulse rounded bg-muted" />
              <div className="h-3 w-4/6 animate-pulse rounded bg-muted" />
            </div>

            <div className="h-5 w-32 animate-pulse rounded bg-muted" />

            <div className="flex gap-3">
              <div className="h-11 flex-1 animate-pulse rounded-lg bg-muted" />
              <div className="h-11 w-32 animate-pulse rounded-lg bg-muted" />
            </div>
          </div>
        </div>
      </div>

      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="h-6 w-24 animate-pulse rounded bg-muted" />
          <div className="h-8 w-28 animate-pulse rounded-lg bg-muted" />
        </div>
        <div className="h-16 animate-pulse rounded-lg bg-muted" />
        <div className="h-16 animate-pulse rounded-lg bg-muted" />
      </div>
    </div>
  );
}
