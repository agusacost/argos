import { useState, useCallback, useEffect, useRef } from 'react';
import { AppContext } from './types';
import type { AppContextType, User, IncidentVM, Metrics } from './types';
import type { Notification } from './api';
import { toIncidentVM } from './adapters';
import {
  getMetrics, listIncidents, patchIncident,
  loginApi, getMe, setStoredToken, getStoredToken,
  generateDocument, getNotifications, getSettings,
} from './api';
import { Login } from './components/Login';
import { Sidebar } from './components/Sidebar';
import { Header } from './components/Header';
import { DashboardView } from './components/DashboardView';
import { IncidentsView } from './components/IncidentsView';
import { KnowledgeBaseView } from './components/KnowledgeBaseView';
import { MetricsView } from './components/MetricsView';
import { LoadTestsView } from './components/LoadTestsView';
import { UsersView } from './components/UsersView';
import { SettingsView } from './components/SettingsView';
import { IncidentDetailDrawer } from './components/IncidentTable';

const DEFAULT_POLL_SECONDS = 20;

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [activeView, setActiveView] = useState('dashboard');
  const [selectedIncidentId, setSelectedIncidentId] = useState<string | null>(null);
  const [incidents, setIncidents] = useState<IncidentVM[]>([]);
  const [metrics, setMetrics] = useState<Metrics | null>(null);
  const [loadingIncidents, setLoadingIncidents] = useState(false);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [pollingSeconds, setPollingSeconds] = useState(DEFAULT_POLL_SECONDS);
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
      const notifs = await getNotifications();
      setNotifications(notifs);
    } catch {
      // silently fail — token might not be ready
    }
  }, []);

  // Try to restore session from stored token
  useEffect(() => {
    const token = getStoredToken();
    if (!token) return;
    getMe()
      .then((u) => {
        setUser({ id: u.id, name: u.name, email: u.email, role: u.role as User['role'], avatar: '' });
      })
      .catch(() => {
        setStoredToken(null);
      });
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
    setActiveView('dashboard');
    setSelectedIncidentId(null);
    setIncidents([]);
    setMetrics(null);
    setNotifications([]);
  }, []);

  const resolveIncident = useCallback(async (id: string) => {
    setSelectedIncidentId(null);
    await patchIncident(id, { estado: 'resuelto', resolved_at: new Date().toISOString() });
    void loadData();
  }, [loadData]);

  const documentIncident = useCallback(async (id: string) => {
    setSelectedIncidentId(null);
    await generateDocument(id);
    void loadData();
  }, [loadData]);

  const ctx: AppContextType = {
    user, login, logout,
    activeView, setActiveView,
    selectedIncidentId, setSelectedIncidentId,
    incidents, metrics, loadingIncidents,
    notifications,
    resolveIncident, documentIncident,
    refreshIncidents: () => { void loadData(); },
    pollingSeconds, setPollingSeconds,
  };

  const renderView = () => {
    switch (activeView) {
      case 'dashboard': return <DashboardView />;
      case 'incidents': return <IncidentsView />;
      case 'knowledge': return <KnowledgeBaseView />;
      case 'metrics': return <MetricsView />;
      case 'loadtests': return <LoadTestsView />;
      case 'users': return <UsersView />;
      case 'settings': return <SettingsView />;
      default: return <DashboardView />;
    }
  };

  return (
    <AppContext.Provider value={ctx}>
      {!user ? (
        <Login />
      ) : (
        <div className="flex h-screen bg-[#0a0e14] overflow-hidden">
          <Sidebar />
          <div className="flex-1 flex flex-col min-w-0">
            <Header />
            <main className="flex-1 overflow-y-auto p-6">
              <div className="animate-fade-in max-w-7xl mx-auto">{renderView()}</div>
            </main>
          </div>
          <IncidentDetailDrawer />
        </div>
      )}
    </AppContext.Provider>
  );
}

export default App;
