'use client';

import { Suspense, type ReactNode } from 'react';
import { useApp } from '@/types';
import { AppProvider } from './AppProvider';
import { Login } from '@/components/Login';
import { Sidebar } from '@/components/Sidebar';
import { Header } from '@/components/Header';
import { IncidentDetailDrawer } from '@/components/IncidentTable';

/** Sin sesión muestra el Login en la misma URL: tras loguear queda en la vista pedida. */
function Layout({ children }: { children: ReactNode }) {
  const { user } = useApp();
  if (!user) return <Login />;
  return (
    <div className="flex h-screen bg-[#0a0e14] overflow-hidden">
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0">
        <Header />
        <main className="flex-1 overflow-y-auto p-6">
          <div className="animate-fade-in max-w-7xl mx-auto">{children}</div>
        </main>
      </div>
      <IncidentDetailDrawer />
    </div>
  );
}

export function AppShell({ children }: { children: ReactNode }) {
  // useSearchParams (filtro ?estado=) exige un límite de Suspense en el export estático.
  return (
    <Suspense fallback={null}>
      <AppProvider>
        <Layout>{children}</Layout>
      </AppProvider>
    </Suspense>
  );
}
