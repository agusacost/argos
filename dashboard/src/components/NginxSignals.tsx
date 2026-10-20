import { useCallback, useEffect, useState } from 'react';
import { Globe, AlertTriangle, Timer } from 'lucide-react';
import { getNginxMetrics } from '@/api';
import type { NginxMetrics } from '@/api';

const RANGES = ['15m', '1h', '3h', '6h'] as const;

interface Line {
  label: string;
  color: string;
  data: (number | null)[];
}

/** Gráfico de líneas multiserie sobre una grilla temporal común. */
function MultiLineChart({
  timestamps,
  lines,
  unit = '',
  height = 180,
}: {
  timestamps: number[];
  lines: Line[];
  unit?: string;
  height?: number;
}) {
  const W = 600;
  const H = height;
  const padBottom = 18;
  const plotH = H - padBottom;

  const allVals = lines.flatMap((l) => l.data.filter((v): v is number => v != null));
  const max = Math.max(...allVals, unit === '%' ? 1 : 0.0001);
  const n = timestamps.length;

  const xAt = (i: number) => (n > 1 ? (i / (n - 1)) * W : W / 2);
  const yAt = (v: number) => plotH - (v / max) * (plotH - 6) - 3;

  // Etiquetas de tiempo: ~5 ticks.
  const tickIdx = n > 1 ? [0, 0.25, 0.5, 0.75, 1].map((f) => Math.round(f * (n - 1))) : [0];
  const fmt = (ts: number) =>
    new Date(ts * 1000).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });

  const pathFor = (data: (number | null)[]) => {
    // Conecta solo puntos definidos consecutivos (spanNulls=false).
    const segs: string[] = [];
    let cur: string[] = [];
    data.forEach((v, i) => {
      if (v == null) {
        if (cur.length) segs.push(`M ${cur.join(' L ')}`);
        cur = [];
      } else {
        cur.push(`${xAt(i)},${yAt(v)}`);
      }
    });
    if (cur.length) segs.push(`M ${cur.join(' L ')}`);
    return segs.join(' ');
  };

  if (allVals.length === 0) {
    return (
      <div className="h-44 flex items-center justify-center text-slate-600 text-sm">
        Sin datos de Loki disponibles.
      </div>
    );
  }

  return (
    <div className="relative" style={{ height: H }}>
      <svg className="w-full h-full" viewBox={`0 0 ${W} ${H}`} preserveAspectRatio="none">
        {[0, 0.25, 0.5, 0.75, 1].map((f) => (
          <line key={f} x1="0" y1={f * plotH} x2={W} y2={f * plotH} stroke="#1e293b" strokeWidth="0.5" />
        ))}
        {lines.map((l) => (
          <path
            key={l.label}
            d={pathFor(l.data)}
            fill="none"
            stroke={l.color}
            strokeWidth="1.75"
            strokeLinejoin="round"
            strokeLinecap="round"
          />
        ))}
      </svg>
      <div className="absolute left-0 right-0 flex justify-between px-1" style={{ top: 0 }}>
        <span className="text-[9px] text-slate-600">
          {max >= 1 ? max.toFixed(unit === '%' ? 0 : 1) : max.toFixed(3)}
          {unit}
        </span>
      </div>
      <div className="absolute bottom-0 left-0 right-0 flex justify-between px-1">
        {tickIdx.map((i) => (
          <span key={i} className="text-[9px] text-slate-600">
            {timestamps[i] != null ? fmt(timestamps[i]) : ''}
          </span>
        ))}
      </div>
    </div>
  );
}

function Legend({ lines }: { lines: Line[] }) {
  return (
    <div className="flex flex-wrap items-center gap-3 mt-2">
      {lines.map((l) => (
        <span key={l.label} className="flex items-center gap-1.5 text-[11px] text-slate-400">
          <span className="w-3 h-0.5 rounded-full" style={{ backgroundColor: l.color }} />
          {l.label}
        </span>
      ))}
    </div>
  );
}

export function NginxSignals() {
  const [range, setRange] = useState<(typeof RANGES)[number]>('1h');
  const [data, setData] = useState<NginxMetrics | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback((r: string) => {
    setLoading(true);
    getNginxMetrics(r)
      .then(setData)
      .catch(() => setData(null))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    load(range);
    const id = setInterval(() => load(range), 20000);
    return () => clearInterval(id);
  }, [range, load]);

  const statusLines: Line[] = data
    ? [
        { label: '2xx', color: '#34d399', data: data.status_series['2xx'] },
        { label: '3xx', color: '#60a5fa', data: data.status_series['3xx'] },
        { label: '4xx', color: '#fbbf24', data: data.status_series['4xx'] },
        { label: '5xx', color: '#f87171', data: data.status_series['5xx'] },
      ]
    : [];

  const latencyLines: Line[] = data
    ? [
        { label: 'p50', color: '#22d3ee', data: data.latency.p50 },
        { label: 'p95', color: '#a78bfa', data: data.latency.p95 },
        { label: 'p99', color: '#f87171', data: data.latency.p99 },
      ]
    : [];

  const lastErr = data?.error_rate.length ? data.error_rate[data.error_rate.length - 1] : 0;
  const lastReq = data?.request_rate.length ? data.request_rate[data.request_rate.length - 1] : 0;

  return (
    <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg bg-cyan-500/10 flex items-center justify-center">
            <Globe className="w-5 h-5 text-cyan-400" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-white">Señales de Nginx (Loki)</h3>
            <p className="text-[11px] text-slate-500">Golden signals del reverse proxy en tiempo real</p>
          </div>
        </div>
        <div className="flex items-center gap-1 bg-slate-950/50 border border-slate-800 rounded-lg p-0.5">
          {RANGES.map((r) => (
            <button
              key={r}
              onClick={() => setRange(r)}
              className={`px-2.5 py-1 rounded-md text-[11px] font-medium transition-colors ${
                range === r ? 'bg-cyan-500/20 text-cyan-300' : 'text-slate-500 hover:text-slate-300'
              }`}
            >
              {r}
            </button>
          ))}
        </div>
      </div>

      {/* Mini stats */}
      <div className="grid grid-cols-2 gap-3 mb-4">
        <div className="bg-slate-950/40 border border-slate-800/50 rounded-lg p-3 flex items-center gap-3">
          <Timer className="w-4 h-4 text-cyan-400" />
          <div>
            <p className="text-[10px] text-slate-500">Request rate (actual)</p>
            <p className="text-lg font-bold text-white">
              {lastReq.toFixed(2)} <span className="text-xs text-slate-500">req/s</span>
            </p>
          </div>
        </div>
        <div className="bg-slate-950/40 border border-slate-800/50 rounded-lg p-3 flex items-center gap-3">
          <AlertTriangle className={`w-4 h-4 ${lastErr >= 5 ? 'text-red-400' : 'text-emerald-400'}`} />
          <div>
            <p className="text-[10px] text-slate-500">Error 5xx (actual)</p>
            <p className={`text-lg font-bold ${lastErr >= 5 ? 'text-red-400' : 'text-white'}`}>
              {lastErr.toFixed(1)} <span className="text-xs text-slate-500">%</span>
            </p>
          </div>
        </div>
      </div>

      {loading && !data && <p className="text-sm text-slate-500">Consultando Loki…</p>}

      {data && (
        <div className="space-y-5">
          <div>
            <h4 className="text-xs font-semibold text-slate-400 mb-2">Requests por código de estado (req/s)</h4>
            <MultiLineChart timestamps={data.timestamps} lines={statusLines} unit="" />
            <Legend lines={statusLines} />
          </div>
          <div className="pt-4 border-t border-slate-800/50">
            <h4 className="text-xs font-semibold text-slate-400 mb-2">Latencia upstream (s)</h4>
            <MultiLineChart timestamps={data.timestamps} lines={latencyLines} unit="s" />
            <Legend lines={latencyLines} />
          </div>
        </div>
      )}
    </div>
  );
}
