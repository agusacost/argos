import { Bell, Search, ChevronDown, LogOut, Shield, Zap, X, Cpu, AlertTriangle, Siren, CheckCheck } from 'lucide-react';
import { useState } from 'react';
import { useApp } from '@/types';
import type { Notification } from '@/api';
import { markAllNotificationsRead, markNotificationRead } from '@/api';
import { summarizeAi } from '@/llmStatus';

const AI_BADGE_CLASS = {
  ok: 'bg-cyan-500/8 border-cyan-500/15 text-cyan-300',
  fallback: 'bg-amber-500/10 border-amber-500/25 text-amber-300',
  down: 'bg-red-500/10 border-red-500/25 text-red-300',
} as const;

const severityDot = (sev: string) => {
  if (sev === 'critical') return 'bg-red-400';
  if (sev === 'warning') return 'bg-amber-400';
  return 'bg-blue-400';
};

function KindIcon({ notification }: { notification: Notification }) {
  if (notification.kind === 'llm_health') return <Cpu className="w-3.5 h-3.5 text-violet-400" />;
  if (notification.kind === 'pipeline_error') return <AlertTriangle className="w-3.5 h-3.5 text-red-400" />;
  return <Siren className="w-3.5 h-3.5 text-cyan-400" />;
}

export function Header() {
  const { user, logout, activeView, notifications, refreshNotifications, llmHealth, setSelectedIncidentId } = useApp();
  const [menuOpen, setMenuOpen] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);
  // Leídas en esta sesión antes de que el próximo polling lo confirme.
  const [readIds, setReadIds] = useState<Set<string>>(new Set());

  if (!user) return null;

  const titles: Record<string, string> = {
    dashboard: 'Dashboard',
    incidents: 'Incidentes Activos',
    knowledge: 'Base de Conocimiento (RAG)',
    metrics: 'Métricas de Sistema',
    loadtests: 'Pruebas de Carga',
    users: 'Gestión de Usuarios y Roles',
    settings: 'Configuración',
  };

  const isRead = (n: Notification) => n.read || readIds.has(n.id);
  const unreadCount = notifications.filter((n) => !isRead(n)).length;
  const ai = summarizeAi(llmHealth);

  const markRead = async (id: string) => {
    setReadIds((prev) => new Set(prev).add(id));
    try {
      await markNotificationRead(id);
    } finally {
      refreshNotifications();
    }
  };

  const markAllRead = async () => {
    setReadIds(new Set(notifications.map((n) => n.id)));
    try {
      await markAllNotificationsRead();
    } finally {
      refreshNotifications();
    }
  };

  const openNotification = (n: Notification) => {
    if (!isRead(n)) void markRead(n.id);
    if (n.incident_id) {
      setSelectedIncidentId(n.incident_id);
      setNotifOpen(false);
    }
  };

  return (
    <header className="h-14 bg-[#0d1117] border-b border-slate-800/60 flex items-center justify-between px-6 flex-shrink-0">
      <div className="flex items-center gap-3">
        <h1 className="text-sm font-semibold text-white">{titles[activeView] || 'Argos'}</h1>
        <div className="flex items-center gap-1.5 px-2 py-0.5 rounded-full bg-emerald-500/10 border border-emerald-500/20">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse-dot" />
          <span className="text-[10px] font-medium text-emerald-400">Sistema Operativo</span>
        </div>
      </div>

      <div className="flex items-center gap-4">
        <div
          title={ai.detail}
          className={`hidden md:flex items-center gap-2 px-3 py-1 rounded-lg border ${AI_BADGE_CLASS[ai.tone]}`}
        >
          <Zap className="w-3.5 h-3.5" />
          <span className="text-[11px] font-medium">{ai.label}</span>
        </div>

        <div className="relative hidden lg:block">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-slate-500" />
          <input
            type="text"
            placeholder="Buscar incidentes..."
            className="bg-slate-950/50 border border-slate-800 rounded-lg pl-8 pr-3 py-1.5 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-cyan-500/50 w-56 transition-all"
          />
        </div>

        {/* Notifications */}
        <div className="relative">
          <button
            onClick={() => { setNotifOpen(!notifOpen); setMenuOpen(false); }}
            className="relative text-slate-400 hover:text-slate-200 transition-colors p-1.5"
            aria-label="Notificaciones"
          >
            <Bell className="w-4 h-4" />
            {unreadCount > 0 && (
              <span className="absolute -top-0.5 -right-0.5 w-4 h-4 rounded-full bg-red-500 text-[9px] font-bold text-white flex items-center justify-center">
                {unreadCount > 9 ? '9+' : unreadCount}
              </span>
            )}
          </button>

          {notifOpen && (
            <>
              <div className="fixed inset-0 z-40" onClick={() => setNotifOpen(false)} />
              <div className="absolute right-0 top-full mt-1 w-96 bg-slate-900 border border-slate-800 rounded-xl shadow-xl z-50 overflow-hidden animate-fade-in">
                <div className="px-4 py-3 border-b border-slate-800 flex items-center justify-between">
                  <span className="text-xs font-semibold text-white">Notificaciones</span>
                  {unreadCount > 0 && (
                    <button
                      onClick={() => { void markAllRead(); }}
                      className="flex items-center gap-1 text-[10px] text-slate-400 hover:text-cyan-300"
                    >
                      <CheckCheck className="w-3 h-3" /> Marcar {unreadCount} como leídas
                    </button>
                  )}
                </div>
                <div className="max-h-96 overflow-y-auto">
                  {notifications.length === 0 && (
                    <div className="px-4 py-6 text-center text-xs text-slate-500">Sin notificaciones</div>
                  )}
                  {notifications.map((n) => {
                    const read = isRead(n);
                    return (
                      <div
                        key={n.id}
                        onClick={() => openNotification(n)}
                        className={`px-4 py-3 border-b border-slate-800/50 flex items-start gap-3 ${n.incident_id ? 'cursor-pointer hover:bg-slate-800/40' : ''} ${read ? 'opacity-50' : 'bg-slate-800/20'}`}
                      >
                        <div className="relative mt-0.5 flex-shrink-0">
                          <KindIcon notification={n} />
                          {!read && <span className={`absolute -top-1 -right-1 w-1.5 h-1.5 rounded-full ${severityDot(n.severity)}`} />}
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="text-xs font-medium text-white truncate">{n.title}</p>
                          <p className="text-[11px] text-slate-500 line-clamp-2 mt-0.5">{n.message}</p>
                          <p className="text-[10px] text-slate-600 mt-1">{new Date(n.created_at).toLocaleString('es-AR')}</p>
                        </div>
                        {!read && (
                          <button
                            onClick={(e) => { e.stopPropagation(); void markRead(n.id); }}
                            className="text-slate-600 hover:text-slate-400 flex-shrink-0"
                            aria-label="Marcar como leída"
                          >
                            <X className="w-3.5 h-3.5" />
                          </button>
                        )}
                      </div>
                    );
                  })}
                </div>
              </div>
            </>
          )}
        </div>

        {/* User menu */}
        <div className="relative">
          <button
            onClick={() => { setMenuOpen(!menuOpen); setNotifOpen(false); }}
            className="flex items-center gap-2.5 pl-2 pr-3 py-1.5 rounded-lg hover:bg-slate-800/40 transition-colors"
          >
            <div className="w-7 h-7 rounded-full bg-gradient-to-br from-cyan-400 to-blue-600 flex items-center justify-center text-white text-xs font-semibold flex-shrink-0">
              {user.name.split(' ').map((n) => n[0]).join('')}
            </div>
            <div className="text-left hidden sm:block">
              <p className="text-xs font-medium text-white leading-tight">{user.name}</p>
              <div className="flex items-center gap-1">
                <Shield className="w-2.5 h-2.5 text-cyan-400" />
                <p className="text-[10px] text-slate-500">{user.role}</p>
              </div>
            </div>
            <ChevronDown className="w-3 h-3 text-slate-500" />
          </button>

          {menuOpen && (
            <>
              <div className="fixed inset-0 z-40" onClick={() => setMenuOpen(false)} />
              <div className="absolute right-0 top-full mt-1 w-56 bg-slate-900 border border-slate-800 rounded-lg shadow-xl z-50 overflow-hidden animate-fade-in">
                <div className="px-4 py-3 border-b border-slate-800">
                  <p className="text-xs font-medium text-white">{user.name}</p>
                  <p className="text-[11px] text-slate-500">{user.email}</p>
                </div>
                <div className="py-1">
                  <div className="px-4 py-2 flex items-center justify-between">
                    <span className="text-xs text-slate-400">Rol</span>
                    <span className="text-xs font-medium text-cyan-300 flex items-center gap-1">
                      <Shield className="w-3 h-3" />
                      {user.role}
                    </span>
                  </div>
                </div>
                <button
                  onClick={logout}
                  className="w-full flex items-center gap-2 px-4 py-2.5 text-xs text-slate-400 hover:text-red-400 hover:bg-slate-800/50 transition-colors border-t border-slate-800"
                >
                  <LogOut className="w-3.5 h-3.5" />
                  Cerrar Sesión
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
