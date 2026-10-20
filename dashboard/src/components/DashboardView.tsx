import { useEffect, useState } from 'react';
import { KpiCards, MttrChart } from './KpiCards';
import { IncidentTable } from './IncidentTable';
import { Activity, Zap, Cpu } from 'lucide-react';
import { useApp } from '@/types';
import { getRecentActivity } from '@/api';
import type { AuditEntry } from '@/api';

export function DashboardView() {
  const { incidents, metrics } = useApp();
  const [activity, setActivity] = useState<AuditEntry[]>([]);

  useEffect(() => {
    void getRecentActivity().then(setActivity).catch(() => setActivity([]));
  }, []);

  const criticalCount = incidents.filter(
    (i) => i.severity === 'critical' && i.status !== 'resolved' && i.status !== 'documented'
  ).length;
  const aiActiveCount = incidents.filter((i) => i.status === 'ai_analyzing').length;

  const byStatus = metrics?.by_status ?? {};
  const resolvedCount = byStatus['resuelto'] ?? 0;
  const investigatingCount = byStatus['pendiente'] ?? 0;
  const documentedCount = (byStatus['auto_documentado'] ?? 0) + (byStatus['documentado_previamente'] ?? 0);

  return (
    <div className="space-y-5">
      <KpiCards />

      {/* AI Activity Banner */}
      <div className="bg-gradient-to-r from-cyan-500/8 via-blue-500/5 to-transparent border border-cyan-500/20 rounded-xl p-4 flex items-center gap-4">
        <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-cyan-400 to-blue-600 flex items-center justify-center shadow-md shadow-cyan-500/30 flex-shrink-0">
          <Cpu className="w-5 h-5 text-white" />
        </div>
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <h3 className="text-sm font-semibold text-white">Argos IA está monitoreando activamente</h3>
            <span className="flex items-center gap-1 text-[10px] font-medium text-cyan-400">
              <span className="w-1.5 h-1.5 rounded-full bg-cyan-400 animate-pulse-dot" />
              {aiActiveCount} análisis en curso
            </span>
          </div>
          <p className="text-xs text-slate-400 mt-0.5">
            {criticalCount} incidente(s) crítico(s) activo(s) · Correlacionando métricas con base de conocimiento (RAG)
          </p>
        </div>
        <div className="hidden md:flex items-center gap-2">
          <Zap className="w-4 h-4 text-cyan-400" />
          <span className="text-xs text-slate-400">Uptime IA: 99.99%</span>
        </div>
      </div>

      {/* MTTR Chart + Quick Stats */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2">
          <MttrChart />
        </div>
        <div className="space-y-4">
          <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
            <h3 className="text-sm font-semibold text-white mb-4">Resumen Operacional</h3>
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-400 flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full bg-red-400" />
                  Críticos activos
                </span>
                <span className="text-sm font-bold text-red-400">{criticalCount}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-400 flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full bg-amber-400" />
                  En investigación
                </span>
                <span className="text-sm font-bold text-amber-400">{investigatingCount}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-400 flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full bg-emerald-400" />
                  Resueltos
                </span>
                <span className="text-sm font-bold text-emerald-400">{resolvedCount}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-400 flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full bg-violet-400" />
                  Documentados
                </span>
                <span className="text-sm font-bold text-violet-400">{documentedCount}</span>
              </div>
            </div>
            <div className="mt-4 pt-4 border-t border-slate-800/50">
              <div className="flex items-center justify-between mb-2">
                <span className="text-[11px] text-slate-500">Total incidentes</span>
                <span className="text-xs font-medium text-cyan-400">{metrics?.total_incidents ?? 0}</span>
              </div>
              <div className="w-full h-1.5 bg-slate-800 rounded-full overflow-hidden">
                <div className="h-full bg-gradient-to-r from-emerald-500 to-emerald-400 rounded-full" style={{ width: '99.94%' }} />
              </div>
            </div>
          </div>

          {/* Actividad reciente — real */}
          <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
            <h3 className="text-sm font-semibold text-white mb-3 flex items-center gap-2">
              <Activity className="w-4 h-4 text-cyan-400" />
              Actividad del Sistema
            </h3>
            {activity.length === 0 && (
              <p className="text-xs text-slate-600">Sin actividad registrada aún.</p>
            )}
            <div className="space-y-2.5">
              {activity.slice(0, 5).map((a, i) => {
                const dot = a.user === 'Argos IA' ? 'bg-cyan-400' : i % 2 === 0 ? 'bg-violet-400' : 'bg-emerald-400';
                const ts = new Date(a.timestamp);
                const now = Date.now();
                const diffMin = Math.floor((now - ts.getTime()) / 60000);
                const time = diffMin < 1 ? 'Ahora' : diffMin < 60 ? `${diffMin} min` : ts.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
                return (
                  <div key={a.id} className="flex items-center gap-2.5 text-xs">
                    <div className={`w-2 h-2 rounded-full ${dot} flex-shrink-0`} />
                    <span className="text-slate-300 flex-1 truncate">
                      <span className="font-medium text-white">{a.user}</span> {a.action}
                    </span>
                    <span className="text-slate-600 text-[10px] flex-shrink-0">{time}</span>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      </div>

      <IncidentTable />
    </div>
  );
}
