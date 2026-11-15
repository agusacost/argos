'use client';

import { useState, useCallback, useEffect, useRef, type ReactNode } from 'react';
import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import { AppContext } from '@/types';
import type { AppContextType, User, IncidentVM, Metrics } from '@/types';
import type { LlmHealth, Notification } from '@/api';
import { toIncidentVM } from '@/adapters';
import { routeFor, viewFor, VIEW_ROUTES } from '@/routes';
import {
  getMetrics, listIncidents, patchIncident,
  loginApi, getMe, setStoredToken, getStoredToken,
  generateDocument, getNotifications, getSettings, getLlmHealth,
} from '@/api';

const DEFAULT_POLL_SECONDS = 20;

/**
 * Estado global del panel (sesión, incidentes, métricas, notificaciones, polling).
 * La vista activa ya no es estado: se deriva de la ruta de Next.js (ver src/routes.ts).
 */
export function AppProvider({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const activeView = viewFor(pathname) ?? '';
  const incidentsEstadoFilter = activeView === 'incidents' ? (searchParams.get('estado') ?? '') : '';
  const [user, setUser] = useState<User | null>(null);
  // false hasta intentar restaurar la sesión: evita mostrar el Login un instante con token válido.
  const [sessionChecked, setSessionChecked] = useState(false);
  const [selectedIncidentId, setSelectedIncidentId] = useState<string | null>(null);
  const [incidents, setIncidents] = useState<IncidentVM[]>([]);
  const [metrics, setMetrics] = useState<Metrics | null>(null);
  const [loadingIncidents, setLoadingIncidents] = useState(false);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [llmHealth, setLlmHealth] = useState<LlmHealth[]>([]);
  const [pollingSeconds, setPollingSeconds] = useState(DEFAULT_POLL_SECONDS);
  // Se incrementa tras cada cambio hecho desde el panel; las vistas con lista propia lo usan para recargar.
  const [incidentsVersion, setIncidentsVersion] = useState(0);
  const pollingRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const loadData = useCallback(async () => {
    setLoadingIncidents(true);
    try {
      const [m, r] = await Promise.all([
        getMetrics(),
        listIncidents({ size: 50 }),
      ]);
      setMetrics(m);
      setIncidents(r.data.map(toIncidentVM));
    } catch (e) {
      console.error('Error loading data:', e);
    } finally {
      setLoadingIncidents(false);
    }
  }, []);

  const loadNotifications = useCallback(async () => {
    try {
      const [notifs, health] = await Promise.all([getNotifications(), getLlmHealth()]);
      setNotifications(notifs);
      setLlmHealth(health);
    } catch {
      // silently fail — token might not be ready
    }
  }, []);

  // Try to restore session from stored token
  useEffect(() => {
    const token = getStoredToken();
    if (!token) {
      setSessionChecked(true);
      return;
    }
    getMe()
      .then((u) => {
        setUser({ id: u.id, name: u.name, email: u.email, role: u.role as User['role'], avatar: '' });
      })
      .catch(() => {
        setStoredToken(null);
      })
      .finally(() => setSessionChecked(true));
  }, []);

  // Intervalo de refresco desde Configuración (polling_interval_seconds)
  useEffect(() => {
    if (!user) return;
    getSettings()
      .then((s) => { if (s.polling_interval_seconds > 0) setPollingSeconds(s.polling_interval_seconds); })
      .catch(() => { /* se mantiene el default */ });
  }, [user]);

  useEffect(() => {
    if (user) {
      void loadData();
      void loadNotifications();
    }
  }, [user, loadData, loadNotifications]);

  useEffect(() => {
    if (user) {
      pollingRef.current = setInterval(() => {
        void loadData();
        void loadNotifications();
      }, pollingSeconds * 1000);
    }
    return () => {
      if (pollingRef.current) {
        clearInterval(pollingRef.current);
        pollingRef.current = null;
      }
    };
  }, [user, pollingSeconds, loadData, loadNotifications]);

  const login = useCallback(async (email: string, password: string) => {
    const res = await loginApi(email, password);
    setStoredToken(res.access_token);
    const u = res.user;
    setUser({ id: u.id, name: u.name, email: u.email, role: u.role as User['role'], avatar: '' });
  }, []);

  const logout = useCallback(() => {
    setStoredToken(null);
    setUser(null);
    router.replace(VIEW_ROUTES.dashboard);
    setSelectedIncidentId(null);
    setIncidents([]);
    setMetrics(null);
    setNotifications([]);
    setLlmHealth([]);
  }, [router]);

  const bumpIncidents = useCallback(() => {
    setIncidentsVersion((v) => v + 1);
    void loadData();
  }, [loadData]);

  // Las dos acciones esperan la respuesta con el panel abierto: si fallan, el error le llega al panel
  // (que lo muestra y deja reintentar); si salen bien, se cierra y se refrescan las listas.
  const resolveIncident = useCallback(async (id: string) => {
    await patchIncident(id, { estado: 'resuelto', resolved_at: new Date().toISOString() });
    setSelectedIncidentId((cur) => (cur === id ? null : cur));
    bumpIncidents();
  }, [bumpIncidents]);

  const documentIncident = useCallback(async (id: string) => {
    await generateDocument(id);
    setSelectedIncidentId((cur) => (cur === id ? null : cur));
    bumpIncidents();
  }, [bumpIncidents]);

  const ctx: AppContextType = {
    user, login, logout,
    activeView,
    // Navegar a una vista abre Incidentes sin filtro; solo openIncidents lo fija (?estado=).
    setActiveView: (v: string) => { router.push(routeFor(v)); },
    incidentsEstadoFilter,
    openIncidents: (estado = '') => {
      router.push(estado ? `${VIEW_ROUTES.incidents}?estado=${encodeURIComponent(estado)}` : VIEW_ROUTES.incidents);
    },
    selectedIncidentId, setSelectedIncidentId,
    incidents, metrics, loadingIncidents,
    notifications,
    refreshNotifications: () => { void loadNotifications(); },
    llmHealth, setLlmHealth,
    resolveIncident, documentIncident,
    incidentsVersion,
    refreshIncidents: bumpIncidents,
    pollingSeconds, setPollingSeconds,
  };

  if (!sessionChecked) return null;

  return <AppContext.Provider value={ctx}>{children}</AppContext.Provider>;
}
