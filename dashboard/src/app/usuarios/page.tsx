'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useApp } from '@/types';
import { UsersView } from '@/components/UsersView';

// Solo Admin: un operador que entra por URL vuelve al Dashboard (el ítem tampoco aparece en el sidebar).
export default function Page() {
  const { user } = useApp();
  const router = useRouter();
  const allowed = user?.role === 'Admin';
  useEffect(() => {
    if (user && !allowed) router.replace('/');
  }, [user, allowed, router]);
  return allowed ? <UsersView /> : null;
}
