'use client';

import { Component, type ReactNode, type ErrorInfo } from 'react';
import { Button } from '@/components/ui/button';
import { useLocale } from '@/lib/locale-context';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

function BoundaryFallback({ error, reset }: { error: Error | null; reset: () => void }) {
  const { t } = useLocale();
  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center gap-4 px-4 text-center">
      <h2 className="text-2xl font-bold">{t('error.boundaryTitle')}</h2>
      <p className="max-w-md text-sm text-muted-foreground">
        {error?.message || t('error.boundaryUnexpected')}
      </p>
      <Button onClick={reset}>{t('error.retry')}</Button>
    </div>
  );
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false, error: null };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('ErrorBoundary caught:', error, info);
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;
      return (
        <BoundaryFallback
          error={this.state.error}
          reset={() => {
            this.setState({ hasError: false, error: null });
            window.location.reload();
          }}
        />
      );
    }
    return this.props.children;
  }
}
