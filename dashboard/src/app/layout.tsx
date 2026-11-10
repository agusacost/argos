import type { Metadata } from 'next';
import type { ReactNode } from 'react';
import { AppShell } from '@/app-shell/AppShell';
import '../index.css';

export const metadata: Metadata = {
  title: 'Argos AIOps Dashboard',
  icons: { icon: '/argos-logo.png' },
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="es">
      <body>
        <AppShell>{children}</AppShell>
      </body>
    </html>
  );
}
