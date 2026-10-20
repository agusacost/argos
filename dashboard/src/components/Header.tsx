import { Bell, Search, ChevronDown, LogOut, Shield, Zap, X } from 'lucide-react';
import { useState } from 'react';
import { useApp } from '@/types';
import { markNotificationRead } from '@/api';

export function Header() {
  const { user, logout, activeView, notifications } = useApp();
  const [menuOpen, setMenuOpen] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);
  const [localNotifs, setLocalNotifs] = useState(notifications);

  // Sync local state when context updates
  if (localNotifs !== notifications) {
    setLocalNotifs(notifications);
  }

  if (!user) return null;

  const titles: Record<string, string> = {
    dashboard: 'Dashboard',
    incidents: 'Incidentes Activos',
    knowledge: 'Base de Conocimiento (RAG)',
    metrics: 'Métricas de Sistema',
    users: 'Gestión de Usuarios y Roles',
    settings: 'Configuración',
  };

  const unreadCount = localNotifs.filter((n) => !n.read).length;

  const severityColor = (sev: string) => {
    if (sev === 'critical') return 'text-red-400';
    if (sev === 'warning') return 'text-amber-400';
    return 'text-blue-400';
  };

  const handleMarkRead = async (id: string) => {
    try {
      await markNotificationRead(id);
      setLocalNotifs((prev) => prev.map((n) => n.id === id ? { ...n, read: true } : n));
    } catch {
      // ignore
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
        <div className="hidden md:flex items-center gap-2 px-3 py-1 rounded-lg bg-cyan-500/8 border border-cyan-500/15">
          <Zap className="w-3.5 h-3.5 text-cyan-400" />
          <span className="text-[11px] font-medium text-cyan-300">IA Activa</span>
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
              <div className="absolute right-0 top-full mt-1 w-80 bg-slate-900 border border-slate-800 rounded-xl shadow-xl z-50 overflow-hidden animate-fade-in">
                <div className="px-4 py-3 border-b border-slate-800 flex items-center justify-between">
                  <span className="text-xs font-semibold text-white">Notificaciones</span>
                  {unreadCount > 0 && (
                    <span className="text-[10px] text-slate-500">{unreadCount} sin leer</span>
                  )}
                </div>
                <div className="max-h-80 overflow-y-auto">
                  {localNotifs.length === 0 && (
                    <div className="px-4 py-6 text-center text-xs text-slate-500">Sin notificaciones</div>
                  )}
                  {localNotifs.map((n) => (
                    <div
                      key={n.id}
                      className={`px-4 py-3 border-b border-slate-800/50 flex items-start gap-3 ${n.read ? 'opacity-50' : 'bg-slate-800/20'}`}
                    >
                      <span className={`w-2 h-2 rounded-full mt-1 flex-shrink-0 ${n.read ? 'bg-slate-600' : severityColor(n.severity).replace('text-', 'bg-')}`} />
                      <div className="flex-1 min-w-0">
                        <p className="text-xs font-medium text-white truncate">{n.title}</p>
                        <p className="text-[11px] text-slate-500 truncate mt-0.5">{n.message}</p>
                        <p className="text-[10px] text-slate-600 mt-1">{new Date(n.created_at).toLocaleString('es-AR')}</p>
                      </div>
                      {!n.read && (
                        <button
                          onClick={() => { void handleMarkRead(n.id); }}
                          className="text-slate-600 hover:text-slate-400 flex-shrink-0"
                        >
                          <X className="w-3.5 h-3.5" />
                        </button>
                      )}
                    </div>
                  ))}
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
