import { useCallback, useEffect, useRef, useState, type MouseEvent } from 'react';
import { Globe, AlertTriangle, Timer } from 'lucide-react';
import { getNginxMetrics } from '@/api';
import type { NginxMetrics } from '@/api';

const RANGES = ['15m', '1h', '3h', '6h'] as const;

interface Line {
  label: string;
  color: string;
  data: (number | null)[];
}

/** Formatea un valor de serie según la unidad del gráfico (igual criterio en eje y tooltip). */
function fmtValue(v: number, unit: string): string {
  if (unit === '%') return `${v.toFixed(1)}%`;
  if (unit === 's') return `${(v * 1000).toFixed(0)} ms`;
  return v >= 10 ? v.toFixed(1) : v.toFixed(2);
}

const fmtTime = (ts: number) =>
  new Date(ts * 1000).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });

/** Gráfico de líneas multiserie sobre una grilla temporal común, con tooltip al pasar el mouse. */
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
  const svgRef = useRef<SVGSVGElement>(null);
  const [hoverIdx, setHoverIdx] = useState<number | null>(null);

  const allVals = lines.flatMap((l) => l.data.filter((v): v is number => v != null));
  const max = Math.max(...allVals, unit === '%' ? 1 : 0.0001);
  const n = timestamps.length;

  const xAt = (i: number) => (n > 1 ? (i / (n - 1)) * W : W / 2);
  const yAt = (v: number) => plotH - (v / max) * (plotH - 6) - 3;

  // Etiquetas de tiempo: ~5 ticks.
  const tickIdx = n > 1 ? [0, 0.25, 0.5, 0.75, 1].map((f) => Math.round(f * (n - 1))) : [0];

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

  const onMove = (e: MouseEvent<SVGSVGElement>) => {
    const rect = svgRef.current?.getBoundingClientRect();
    if (!rect || n < 1) return;
    const xRatio = (e.clientX - rect.left) / rect.width;
    const i = Math.round(xRatio * (n - 1));
    setHoverIdx(Math.min(n - 1, Math.max(0, i)));
  };

  // Tooltip: se mantiene dentro del ancho del gráfico aunque el cursor esté en el borde.
  const tooltipLeftPct = hoverIdx != null && n > 1 ? (hoverIdx / (n - 1)) * 100 : 50;
  const tooltipAlign = tooltipLeftPct > 70 ? 'translate(-100%, 0)' : tooltipLeftPct < 15 ? 'translate(0, 0)' : 'translate(-50%, 0)';

  return (
    <div className="relative" style={{ height: H }}>
      <svg
        ref={svgRef}
        className="w-full h-full cursor-crosshair"
        viewBox={`0 0 ${W} ${H}`}
        preserveAspectRatio="none"
        onMouseMove={onMove}
        onMouseLeave={() => setHoverIdx(null)}
      >
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
        {hoverIdx != null && (
          <>
            <line x1={xAt(hoverIdx)} y1="0" x2={xAt(hoverIdx)} y2={plotH} stroke="#475569" strokeWidth="1" strokeDasharray="3,3" />
            {lines.map((l) => {
              const v = l.data[hoverIdx];
              if (v == null) return null;
              return <circle key={l.label} cx={xAt(hoverIdx)} cy={yAt(v)} r="3.5" fill="#0a0e14" stroke={l.color} strokeWidth="2" />;
            })}
          </>
        )}
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
            {timestamps[i] != null ? fmtTime(timestamps[i]).slice(0, 5) : ''}
          </span>
        ))}
      </div>
      {hoverIdx != null && timestamps[hoverIdx] != null && (
        <div
          className="absolute top-1 bg-slate-950/95 border border-slate-700 rounded-lg px-2.5 py-1.5 text-[10px] pointer-events-none shadow-xl z-10 whitespace-nowrap"
          style={{ left: `${tooltipLeftPct}%`, transform: tooltipAlign }}
        >
          <div className="text-slate-400 font-mono mb-1">{fmtTime(timestamps[hoverIdx])}</div>
          {lines.map((l) => {
            const v = l.data[hoverIdx];
            return (
              <div key={l.label} className="flex items-center gap-1.5">
                <span className="w-2 h-0.5 rounded-full flex-shrink-0" style={{ backgroundColor: l.color }} />
                <span className="text-slate-300">{l.label}:</span>
                <span className="text-white font-mono">{v != null ? fmtValue(v, unit) : '—'}</span>
              </div>
            );
          })}
        </div>
      )}
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
        // Subconjunto de 4xx: requests cortados por el rate limit de nginx (limit_req/limit_conn).
        // Se grafica aparte para que un flood/DDoS L7 se distinga de otros 4xx (404, 403, etc.).
        { label: '429 (rate limit)', color: '#f472b6', data: data.status_series['429'] },
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
