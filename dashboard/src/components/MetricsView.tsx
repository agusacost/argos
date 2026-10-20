import { useEffect, useState } from 'react';
import { Clock, TrendingDown, TrendingUp, Target, Gauge, Activity, Zap, Server, Database, Globe } from 'lucide-react';
import { Sparkline } from './Sparkline';
import { NginxSignals } from './NginxSignals';
import { useApp } from '@/types';
import { getServicesHealth } from '@/api';
import type { ServiceHealth } from '@/api';

export function MetricsView() {
  const { metrics } = useApp();

  if (!metrics) {
    return (
      <div className="flex items-center justify-center h-64">
        <span className="text-slate-500">Cargando métricas…</span>
      </div>
    );
  }

  const mttrVal = metrics.mttr_average_minutes != null
    ? Math.round(metrics.mttr_average_minutes)
    : null;

  // Top 5 alert types by count
  const topTypes = Object.entries(metrics.by_alert_type)
    .sort((a, b) => b[1] - a[1])
    .slice(0, 5);
  const maxTypeCount = Math.max(...topTypes.map(([, c]) => c), 1);

  // Daily volume chart (last 7 days)
  const perDay = metrics.incidents_per_day.slice(-7);
  const maxDay = Math.max(...perDay.map((d) => d.count), 1);

  // Trend indicator
  const trendIsDown = metrics.recent_trend === 'improving';
  const trendLabel = metrics.recent_trend === 'improving' ? '↓ mejorando' : metrics.recent_trend === 'degrading' ? '↑ empeorando' : '→ estable';
  const trendColor = metrics.recent_trend === 'improving' ? 'text-emerald-400' : metrics.recent_trend === 'degrading' ? 'text-red-400' : 'text-slate-400';

  const [services, setServices] = useState<ServiceHealth[]>([]);
  const [servicesLoading, setServicesLoading] = useState(true);

  useEffect(() => {
    getServicesHealth()
      .then(setServices)
      .catch(() => setServices([]))
      .finally(() => setServicesLoading(false));
  }, []);

  const iconByComponent: Record<string, React.ElementType> = {
    nginx: Globe, api: Server, 'rag-api': Database, qdrant: Zap,
  };

  const statusColors: Record<string, { text: string; bg: string; dot: string }> = {
    healthy: { text: 'text-emerald-400', bg: 'bg-emerald-500/10', dot: 'bg-emerald-400' },
    degraded: { text: 'text-red-400', bg: 'bg-red-500/10', dot: 'bg-red-400' },
    unknown: { text: 'text-slate-400', bg: 'bg-slate-500/10', dot: 'bg-slate-500' },
  };

  return (
    <div className="space-y-5">
      {/* Top metrics row */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* MTTR */}
        <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-lg bg-cyan-500/10 flex items-center justify-center">
              <Clock className="w-5 h-5 text-cyan-400" />
            </div>
            <div>
              <h3 className="text-sm font-semibold text-white">MTTR Promedio</h3>
              <p className="text-[11px] text-slate-500">Tiempo medio de resolución</p>
            </div>
          </div>
          <p className="text-3xl font-bold text-white mb-1">
            {mttrVal != null ? mttrVal : '—'}
            <span className="text-lg text-slate-500"> min</span>
          </p>
          <div className="flex items-center gap-2">
            <span className={`flex items-center gap-1 text-xs font-medium ${trendColor}`}>
              {trendIsDown ? <TrendingDown className="w-3 h-3" /> : <TrendingUp className="w-3 h-3" />}
              {trendLabel}
            </span>
          </div>
        </div>

        {/* Total incidents as SLA proxy */}
        <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-lg bg-emerald-500/10 flex items-center justify-center">
              <Target className="w-5 h-5 text-emerald-400" />
            </div>
            <div>
              <h3 className="text-sm font-semibold text-white">Incidentes Totales</h3>
              <p className="text-[11px] text-slate-500">Acumulado en la base vectorial</p>
            </div>
          </div>
          <p className="text-3xl font-bold text-white mb-1">{metrics.total_incidents}</p>
          <div className="w-full h-2 bg-slate-800 rounded-full overflow-hidden mt-2">
            <div
              className="h-full bg-gradient-to-r from-emerald-500 to-emerald-400 rounded-full"
              style={{ width: `${Math.min(100, (metrics.by_status['resuelto'] ?? 0) / Math.max(metrics.total_incidents, 1) * 100)}%` }}
            />
          </div>
          <p className="text-[11px] text-slate-500 mt-1">
            {metrics.by_status['resuelto'] ?? 0} resueltos
          </p>
        </div>

        {/* Auto-documented */}
        <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-lg bg-blue-500/10 flex items-center justify-center">
              <Gauge className="w-5 h-5 text-blue-400" />
            </div>
            <div>
              <h3 className="text-sm font-semibold text-white">Auto-documentados</h3>
              <p className="text-[11px] text-slate-500">Generados por Argos IA</p>
            </div>
          </div>
          <p className="text-3xl font-bold text-white mb-1">
            {metrics.by_status['auto_documentado'] ?? 0}
          </p>
          <span className="text-xs text-slate-500">
            de {metrics.total_incidents} incidentes totales
          </span>
        </div>
      </div>

      {/* Daily volume line chart */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <div className="flex items-center justify-between mb-5">
          <div>
            <h3 className="text-sm font-semibold text-white">Incidentes por día — últimos 7 días</h3>
            <p className="text-xs text-slate-500 mt-0.5">
              Volumen de alertas detectadas · Tendencia: <span className={trendColor}>{trendLabel}</span>
            </p>
          </div>
        </div>

        {perDay.length === 0 ? (
          <div className="h-48 flex items-center justify-center text-slate-600 text-sm">Sin datos</div>
        ) : (
          <div className="relative h-48">
            <svg className="w-full h-full" viewBox="0 0 600 180" preserveAspectRatio="none">
              {[0, 45, 90, 135, 180].map((y) => (
                <line key={y} x1="0" y1={y} x2="600" y2={y} stroke="#1e293b" strokeWidth="0.5" />
              ))}
              <defs>
                <linearGradient id="dayArea" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#22d3ee" stopOpacity="0.2" />
                  <stop offset="100%" stopColor="#22d3ee" stopOpacity="0" />
                </linearGradient>
              </defs>
              {(() => {
                const pts = perDay.map((d, i) => ({
                  x: (i / Math.max(perDay.length - 1, 1)) * 600,
                  y: 180 - (d.count / maxDay) * 160 - 10,
                }));
                const linePath = `M ${pts.map((p) => `${p.x},${p.y}`).join(' L ')}`;
                const areaPath = `${linePath} L 600,180 L 0,180 Z`;
                return (
                  <>
                    <path d={areaPath} fill="url(#dayArea)" />
                    <path d={linePath} fill="none" stroke="#22d3ee" strokeWidth="2" strokeLinejoin="round" strokeLinecap="round" />
                    {pts.map((p, i) => (
                      <circle key={i} cx={p.x} cy={p.y} r="4" fill="#0a0e14" stroke="#22d3ee" strokeWidth="2" />
                    ))}
                  </>
                );
              })()}
            </svg>
            <div className="absolute bottom-0 left-0 right-0 flex justify-around">
              {perDay.map((d) => (
                <span key={d.date} className="text-[10px] text-slate-500">{d.date.slice(5)}</span>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Service health — real (Prometheus) */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <h3 className="text-sm font-semibold text-white mb-4">Salud de Servicios (Prometheus)</h3>
        {servicesLoading && <p className="text-sm text-slate-500">Consultando Prometheus…</p>}
        {!servicesLoading && services.length === 0 && (
          <p className="text-sm text-slate-600">Sin datos de Prometheus disponibles.</p>
        )}
        <div className="space-y-3">
          {services.map((svc) => {
            const sc = statusColors[svc.status] ?? statusColors['unknown'];
            const Icon = iconByComponent[svc.component] ?? Server;
            const sparkColor = svc.status === 'healthy' ? '#34d399' : svc.status === 'degraded' ? '#f87171' : '#94a3b8';
            return (
              <div key={svc.name} className="flex items-center gap-4 bg-slate-950/30 rounded-lg p-3 border border-slate-800/50">
                <div className="w-9 h-9 rounded-lg bg-slate-800 flex items-center justify-center flex-shrink-0">
                  <Icon className="w-4 h-4 text-slate-400" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-white">{svc.name}</span>
                    <span className={`w-1.5 h-1.5 rounded-full ${sc.dot} animate-pulse-dot`} />
                  </div>
                </div>
                <div className={`px-2 py-1 rounded-md ${sc.bg}`}>
                  <span className={`text-[10px] font-medium ${sc.text} capitalize`}>{svc.status}</span>
                </div>
                {svc.sparkline.length > 0 && (
                  <Sparkline data={svc.sparkline.slice(-12)} color={sparkColor} width={80} height={28} />
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* Nginx golden signals — real (Loki) */}
      <NginxSignals />

      {/* Top alert types bar chart */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <div className="flex items-center gap-3 mb-5">
          <div className="w-10 h-10 rounded-lg bg-amber-500/10 flex items-center justify-center">
            <Activity className="w-5 h-5 text-amber-400" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-white">Top tipos de alerta</h3>
            <p className="text-xs text-slate-500 mt-0.5">Por cantidad de incidentes detectados</p>
          </div>
        </div>
        <div className="space-y-3">
          {topTypes.map(([tipo, count]) => (
            <div key={tipo} className="flex items-center gap-3">
              <span className="text-xs text-slate-400 w-40 truncate flex-shrink-0">{tipo}</span>
              <div className="flex-1 h-2 bg-slate-800 rounded-full overflow-hidden">
                <div
                  className="h-full bg-gradient-to-r from-amber-600 to-amber-400 rounded-full transition-all"
                  style={{ width: `${(count / maxTypeCount) * 100}%` }}
                />
              </div>
              <span className="text-xs font-mono text-slate-400 w-6 text-right">{count}</span>
            </div>
          ))}
        </div>

        {/* By status breakdown */}
        <div className="mt-5 pt-5 border-t border-slate-800/50">
          <h4 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">Desglose por estado</h4>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {Object.entries(metrics.by_status).map(([estado, n]) => (
              <div key={estado} className="bg-slate-950/40 border border-slate-800/50 rounded-lg p-3">
                <p className="text-[10px] text-slate-500 capitalize">{estado.replace(/_/g, ' ')}</p>
                <p className="text-xl font-bold text-white mt-1">{n}</p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
