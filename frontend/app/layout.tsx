import './globals.css';
import { Providers } from '@/components/providers';
import { SiteHeader } from '@/components/site-header';
import { CartSheet } from '@/components/cart-sheet';
import { Toaster } from '@/components/ui/toaster';
import { StoreFooter } from '@/components/store-footer';
import { ClientLayout } from './client-layout';

export const metadata = {
  title: 'نووا‌شاپ | فروشگاه عروسک‌های بافتنی دست‌ساز',
  description: 'فروشگاه تخصصی عروسک‌های بافتنی دست‌ساز — هر عروسک با عشق بافته شده است',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <ClientLayout>
      <Providers>
        <SiteHeader />
        <main className="flex-1">{children}</main>
        <StoreFooter />
        <CartSheet />
        <Toaster />
      </Providers>
    </ClientLayout>
  );
}
