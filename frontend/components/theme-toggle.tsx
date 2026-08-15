'use client';

import { useState } from 'react';
import { Moon, Sun } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useLocale } from '@/lib/locale-context';

const KEY = 'novashop-theme';

export function ThemeToggle() {
  const { t } = useLocale();

  const [dark, setDark] = useState(() => {
    if (typeof window === 'undefined') {
      return false;
    }

    const savedTheme = localStorage.getItem(KEY);

    if (savedTheme) {
      return savedTheme === 'dark';
    }

    return document.documentElement.classList.contains('dark');
  });

  const toggle = () => {
    const next = !dark;

    setDark(next);
    document.documentElement.classList.toggle('dark', next);
    localStorage.setItem(KEY, next ? 'dark' : 'light');
  };

  return (
    <Button
      variant="ghost"
      size="icon"
      className="text-muted-foreground hover:text-foreground"
      onClick={toggle}
      aria-label={dark ? t('theme.light') : t('theme.dark')}
    >
      {dark ? <Sun className="size-5" /> : <Moon className="size-5" />}
    </Button>
  );
}
