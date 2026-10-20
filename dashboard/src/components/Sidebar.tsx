import { LayoutDashboard, AlertTriangle, BookOpen, BarChart3, Users, Settings, LogOut, ChevronRight, Gauge } from 'lucide-react';
import { useApp } from '@/types';

const navItems = [
  { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { id: 'incidents', label: 'Incidentes Activos', icon: AlertTriangle },
  { id: 'knowledge', label: 'Base de Conocimiento', icon: BookOpen, badge: 'RAG' },
  { id: 'metrics', label: 'Métricas de Sistema', icon: BarChart3 },
  { id: 'loadtests', label: 'Pruebas de Carga', icon: Gauge, badge: 'k6' },
  { id: 'users', label: 'Gestión de Usuarios', icon: Users, adminOnly: true },
  { id: 'settings', label: 'Configuración', icon: Settings },
];

export function Sidebar() {
  const { user, activeView, setActiveView, logout } = useApp();
  if (!user) return null;

  const items = navItems.filter((item) => !item.adminOnly || user.role === 'Admin');

  return (
    <aside className="w-60 bg-[#0d1117] border-r border-slate-800/60 flex flex-col h-full flex-shrink-0">
      <div className="px-5 py-5 border-b border-slate-800/60 flex items-center gap-3">
        <img src="/argos-logo.png" alt="Argos" className="w-9 h-9 rounded-lg object-contain" />
        <div>
          <span className="text-white font-semibold text-sm tracking-tight block">Argos</span>
          <span className="text-cyan-400 text-[10px] font-medium tracking-wide">AIOps Platform</span>
        </div>
      </div>

      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        {items.map((item) => {
          const Icon = item.icon;
          const active = activeView === item.id;
          return (
            <button
              key={item.id}
              onClick={() => setActiveView(item.id)}
              className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all group ${
                active
                  ? 'bg-cyan-500/10 text-cyan-300 border border-cyan-500/20'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/40 border border-transparent'
              }`}
            >
              <Icon className={`w-4 h-4 flex-shrink-0 ${active ? 'text-cyan-400' : ''}`} />
              <span className="flex-1 text-left text-[13px]">{item.label}</span>
              {item.badge && (
                <span className="text-[9px] font-bold px-1.5 py-0.5 rounded bg-cyan-500/15 text-cyan-400 tracking-wider">
                  {item.badge}
                </span>
              )}
              {active && <ChevronRight className="w-3 h-3 text-cyan-400" />}
            </button>
          );
        })}
      </nav>

      <div className="px-3 py-4 border-t border-slate-800/60">
        <div className="flex items-center gap-3 px-3 py-2 rounded-lg bg-slate-800/30">
          <div className="w-8 h-8 rounded-full bg-gradient-to-br from-cyan-400 to-blue-600 flex items-center justify-center text-white text-xs font-semibold flex-shrink-0">
            {user.name.split(' ').map((n) => n[0]).join('')}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-xs font-medium text-white truncate">{user.name}</p>
            <p className="text-[10px] text-slate-500">{user.role}</p>
          </div>
          <button
            onClick={logout}
            className="text-slate-500 hover:text-red-400 transition-colors p-1"
            title="Cerrar sesión"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </div>
    </aside>
  );
}
