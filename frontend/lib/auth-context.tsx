'use client';

import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';

interface AuthState {
  /** JWT payload claims, decoded from the token cookie. */
  user: { id: number; username: string; role: string } | null;
  isAuthenticated: boolean;
}

interface AuthContextValue extends AuthState {
  /** Persist token cookie (8h) and notify subscribers. */
  signIn: (token: string) => void;
  /** Clear token + user state. Safe to call even if the logout API fails. */
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const TOKEN_COOKIE = 'token';
const TOKEN_MAX_AGE = 8 * 60 * 60; // 8 hours

export function getTokenCookie(): string | null {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(/(?:^|;\s*)token=([^;]*)/);
  return match ? decodeURIComponent(match[1]) : null;
}

// Only allow internal relative paths as redirect targets (prevents open-redirect).
export function getSafeReturnUrl(returnUrl?: string | null): string {
  if (!returnUrl) return '';
  // Must start with a single slash and not be a protocol-relative //evil.com
  if (!returnUrl.startsWith('/') || returnUrl.startsWith('//')) return '';
  return returnUrl;
}

// Build a /login?returnUrl=... redirect that preserves the current path + query.
export function redirectToLogin(
  router: { replace: (url: string) => void },
  pathname: string,
  searchParams: { toString: () => string }
): void {
  const qs = searchParams.toString();
  const target = qs ? `${pathname}?${qs}` : pathname;
  router.replace(`/login?returnUrl=${encodeURIComponent(target)}`);
}

function base64UrlDecode(input: string): string {
  const b64 = input.replace(/-/g, '+').replace(/_/g, '/');
  const pad = b64.length % 4 === 0 ? '' : '='.repeat(4 - (b64.length % 4));
  return atob(b64 + pad);
}

function decodeToken(token: string): { id: number; username: string; role: string } | null {
  try {
    const payload = JSON.parse(base64UrlDecode(token.split('.')[1]));
    return {
      id: Number(payload.sub),
      username: payload.name
        ?? payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
        ?? '',
      role: payload.role
        ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        ?? '',
    };
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthState['user']>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const token = getTokenCookie();
    const decoded = token ? decodeToken(token) : null;
    setUser(decoded && decoded.id > 0 ? decoded : null);
    setReady(true);
  }, []);

  const value: AuthContextValue = {
    user,
    isAuthenticated: user !== null,
    signIn: (token: string) => {
      document.cookie = `${TOKEN_COOKIE}=${encodeURIComponent(token)};path=/;max-age=${TOKEN_MAX_AGE}`;
      setUser(decodeToken(token));
    },
    signOut: () => {
      document.cookie = `${TOKEN_COOKIE}=;path=/;max-age=0`;
      // Clear any legacy localStorage token too.
      if (typeof window !== 'undefined') localStorage.removeItem('token');
      setUser(null);
    },
  };

  if (!ready) return null;

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}