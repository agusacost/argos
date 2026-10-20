import { Clock, AlertTriangle, FileText, TrendingDown, TrendingUp, Cpu } from 'lucide-react';
import { Sparkline } from './Sparkline';
import { useApp } from '@/types';

export function KpiCards() {
  const { metrics, incidents } = useApp();

  const mttrVal = metrics?.mttr_average_minutes != null
    ? `${Math.round(metrics.mttr_average_minutes)} min`
    : '— min';

  const today = new Date().toISOString().split('T')[0];
  const todayCount = metrics?.incidents_per_day.find((d) => d.date === today)?.count ?? 0;
  const volumeSparkline = (metrics?.incidents_per_day ?? []).slice(-8).map((d) => d.count);

  const autoDocCount = (metrics?.by_status['auto_documentado'] ?? 0);
  const aiActiveCount = incidents.filter((i) => i.status === 'ai_analyzing').length;
  const mttrSparkline = metrics
    ? Array(8).fill(Math.round(metrics.mttr_average_minutes ?? 0))
    : [0, 0, 0, 0, 0, 0, 0, 0];

  const kpis = [
    {
      label: 'MTTR Actual',
      value: mttrVal,
      change: metrics?.recent_trend === 'improving' ? 'Mejorando' : metrics?.recent_trend === 'degrading' ? 'Empeorando' : 'Estable',
      trendDown: metrics?.recent_trend === 'improving',
      icon: Clock,
      color: 'cyan',
      sparkline: mttrSparkline,
    },
    {
      label: 'Incidentes Hoy',
      value: String(todayCount),
      change: `${metrics?.total_incidents ?? 0} total`,
      trendDown: false,
      icon: AlertTriangle,
      color: 'amber',
      sparkline: volumeSparkline.length ? volumeSparkline : [0],
    },
    {
      label: 'Auto-Documentados',
      value: String(autoDocCount),
      sublabel: 'total',
      change: '+IA',
      trendDown: false,
      icon: FileText,
      color: 'emerald',
      sparkline: Array(8).fill(autoDocCount),
    },
    {
      label: 'IA Análisis Activos',
      value: String(aiActiveCount),
      sublabel: 'en proceso',
      change: 'Live',
      trendDown: false,
      isLive: true,
      icon: Cpu,
      color: 'blue',
      sparkline: Array(8).fill(aiActiveCount),
    },
  ];

  const colorMap: Record<string, { text: string; bg: string; border: string; stroke: string }> = {
    cyan: { text: 'text-cyan-400', bg: 'bg-cyan-500/10', border: 'border-cyan-500/20', stroke: '#22d3ee' },
    amber: { text: 'text-amber-400', bg: 'bg-amber-500/10', border: 'border-amber-500/20', stroke: '#fbbf24' },
    emerald: { text: 'text-emerald-400', bg: 'bg-emerald-500/10', border: 'border-emerald-500/20', stroke: '#34d399' },
    blue: { text: 'text-blue-400', bg: 'bg-blue-500/10', border: 'border-blue-500/20', stroke: '#60a5fa' },
  };

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {kpis.map((kpi) => {
        const c = colorMap[kpi.color];
        const Icon = kpi.icon;
        return (
          <div
            key={kpi.label}
            className={`bg-slate-900/60 border ${c.border} rounded-xl p-5 hover:bg-slate-900/80 transition-all group relative overflow-hidden`}
          >
            <div className="flex items-start justify-between mb-3">
              <div className={`w-10 h-10 rounded-lg ${c.bg} flex items-center justify-center`}>
                <Icon className={`w-5 h-5 ${c.text}`} />
              </div>
              <div className={`flex items-center gap-1 text-xs font-medium ${
                kpi.isLive ? c.text : kpi.trendDown ? 'text-emerald-400' : kpi.color === 'amber' ? 'text-amber-400' : 'text-emerald-400'
              }`}>
                {kpi.isLive ? (
                  <span className="flex items-center gap-1">
                    <span className="w-1.5 h-1.5 rounded-full bg-cyan-400 animate-pulse-dot" />
                    {kpi.change}
                  </span>
                ) : kpi.trendDown ? (
                  <><TrendingDown className="w-3 h-3" />{kpi.change}</>
                ) : (
                  <><TrendingUp className="w-3 h-3" />{kpi.change}</>
                )}
              </div>
            </div>
            <div className="flex items-end justify-between">
              <div>
                <p className="text-2xl font-bold text-white tracking-tight">{kpi.value}</p>
                <p className="text-xs text-slate-500 mt-0.5">
                  {kpi.label}
                  {'sublabel' in kpi && kpi.sublabel && <span className="text-slate-600"> · {kpi.sublabel}</span>}
                </p>
              </div>
              <div className="opacity-60 group-hover:opacity-100 transition-opacity">
                <Sparkline data={kpi.sparkline} color={c.stroke} width={80} height={32} />
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

export function MttrChart() {
  const { metrics } = useApp();
  const perDay = (metrics?.incidents_per_day ?? []).slice(-6);
  const max = Math.max(...perDay.map((d) => d.count), 1);
  const chartHeight = 180;

  return (
    <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
      <div className="flex items-center justify-between mb-5">
        <div>
          <h3 className="text-sm font-semibold text-white">Incidentes por día</h3>
          <p className="text-xs text-slate-500 mt-0.5">Volumen de alertas detectadas — últimos 6 días</p>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-xs text-slate-500">Total</span>
          <span className="text-xs font-medium text-cyan-400 bg-cyan-500/10 px-2 py-1 rounded-md">
            {metrics?.total_incidents ?? 0}
          </span>
        </div>
      </div>

      <div className="relative" style={{ height: chartHeight }}>
        <div className="absolute inset-0 flex flex-col justify-between">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="border-t border-slate-800/50 w-full" />
          ))}
        </div>
        {perDay.length === 0 ? (
          <div className="absolute inset-0 flex items-center justify-center text-slate-600 text-sm">
            Sin datos
          </div>
        ) : (
          <div className="absolute inset-0 flex items-end justify-around gap-2 px-2">
            {perDay.map((d, i) => {
              const h = (d.count / max) * (chartHeight - 30);
              const isLast = i === perDay.length - 1;
              return (
                <div key={d.date} className="flex-1 flex flex-col items-center gap-2 group cursor-pointer">
                  <div className="relative w-full flex flex-col items-center">
                    <span className={`text-[10px] font-mono mb-1 transition-opacity ${isLast ? 'text-cyan-300 opacity-100' : 'text-slate-400 opacity-0 group-hover:opacity-100'}`}>
                      {d.count}
                    </span>
                    <div
                      className={`w-full max-w-[36px] rounded-t-md transition-all duration-500 ${
                        isLast
                          ? 'bg-gradient-to-t from-cyan-600 to-cyan-400 shadow-lg shadow-cyan-500/20'
                          : 'bg-gradient-to-t from-slate-700 to-slate-600 group-hover:from-cyan-700 group-hover:to-cyan-500'
                      }`}
                      style={{ height: h }}
                    />
                  </div>
                  <span className="text-[10px] text-slate-500 group-hover:text-slate-300 transition-colors">
                    {d.date.slice(5)}
                  </span>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <div className="flex items-center justify-between mt-4 pt-4 border-t border-slate-800/50">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-1.5">
            <div className="w-2.5 h-2.5 rounded-sm bg-cyan-400" />
            <span className="text-[11px] text-slate-400">Más reciente</span>
          </div>
          <div className="flex items-center gap-1.5">
            <div className="w-2.5 h-2.5 rounded-sm bg-slate-600" />
            <span className="text-[11px] text-slate-400">Histórico</span>
          </div>
        </div>
        <span className="text-[11px] text-slate-500">
          Tendencia: <span className={
            metrics?.recent_trend === 'improving' ? 'text-emerald-400' :
            metrics?.recent_trend === 'degrading' ? 'text-red-400' : 'text-slate-400'
          }>
            {metrics?.recent_trend === 'improving' ? '↓ mejorando' : metrics?.recent_trend === 'degrading' ? '↑ empeorando' : '→ estable'}
          </span>
        </span>
      </div>
    </div>
  );
}
