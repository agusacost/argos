import { useCallback, useEffect, useRef, useState } from 'react';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { oneDark } from 'react-syntax-highlighter/dist/esm/styles/prism';
import {
  Gauge, Play, Square, Clock, CheckCircle2, XCircle, AlertTriangle,
  Activity, FileCode2, Loader2,
} from 'lucide-react';
import { listScenarios, listRuns, startLoadTest, stopRun } from '@/api';
import type { LoadScenario, LoadRun, LoadRunStatus } from '@/api';
import { useApp } from '@/types';

const POLL_MS = 3000;

const STATUS_META: Record<LoadRunStatus, { label: string; cls: string; icon: typeof CheckCircle2 }> = {
  running: { label: 'En ejecución', cls: 'text-cyan-400 bg-cyan-500/10 border-cyan-500/20', icon: Loader2 },
  passed: { label: 'OK', cls: 'text-emerald-400 bg-emerald-500/10 border-emerald-500/20', icon: CheckCircle2 },
  thresholds_failed: { label: 'Thresholds', cls: 'text-amber-400 bg-amber-500/10 border-amber-500/20', icon: AlertTriangle },
  failed: { label: 'Falló', cls: 'text-red-400 bg-red-500/10 border-red-500/20', icon: XCircle },
  cancelled: { label: 'Cancelada', cls: 'text-slate-400 bg-slate-500/10 border-slate-500/20', icon: Square },
};

function StatusBadge({ status }: { status: LoadRunStatus }) {
  const m = STATUS_META[status] ?? STATUS_META.failed;
  const Icon = m.icon;
  return (
    <span className={`inline-flex items-center gap-1.5 text-[11px] font-medium px-2 py-0.5 rounded border ${m.cls}`}>
      <Icon className={`w-3 h-3 ${status === 'running' ? 'animate-spin' : ''}`} />
      {m.label}
    </span>
  );
}

function fmtPct(v: number | null): string {
  return v == null ? '—' : `${(v * 100).toFixed(2)}%`;
}
function fmtMs(v: number | null): string {
  return v == null ? '—' : `${v.toFixed(2)} ms`;
}
function fmtNum(v: number | null): string {
  return v == null ? '—' : v.toLocaleString();
}

function elapsed(from: string, to: string | null): string {
  const start = new Date(from).getTime();
  const end = to ? new Date(to).getTime() : Date.now();
  const s = Math.max(0, Math.round((end - start) / 1000));
  const m = Math.floor(s / 60);
  return m > 0 ? `${m}m ${s % 60}s` : `${s}s`;
}

function SummaryTable({ run }: { run: LoadRun }) {
  const s = run.summary;
  if (!s) return null;
  const cells: { label: string; value: string }[] = [
    { label: 'Requests', value: fmtNum(s.reqs) },
    { label: 'Throughput', value: s.throughput_rps == null ? '—' : `${s.throughput_rps.toFixed(2)} req/s` },
    { label: 'p95 latencia', value: fmtMs(s.p95_ms) },
    { label: 'Latencia media', value: fmtMs(s.avg_ms) },
    { label: 'Error rate', value: fmtPct(s.error_rate) },
    { label: 'Checks OK', value: fmtPct(s.checks_rate) },
  ];
  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
      {cells.map((c) => (
        <div key={c.label} className="bg-slate-950/40 border border-slate-800/50 rounded-lg px-3 py-2">
          <p className="text-[10px] text-slate-500 uppercase tracking-wide">{c.label}</p>
          <p className="text-sm font-semibold text-white mt-0.5">{c.value}</p>
        </div>
      ))}
    </div>
  );
}

export function LoadTestsView() {
  const { user } = useApp();
  const isAdmin = user?.role === 'Admin';

  const [scenarios, setScenarios] = useState<LoadScenario[]>([]);
  const [selected, setSelected] = useState<string | null>(null);
  const [runs, setRuns] = useState<LoadRun[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [starting, setStarting] = useState(false);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const activeRun = runs.find((r) => r.status === 'running') ?? null;
  const selectedScenario = scenarios.find((s) => s.id === selected) ?? null;

  const refreshRuns = useCallback(async () => {
    try {
      setRuns(await listRuns());
    } catch {
      // silencioso: el token puede no estar listo todavía
    }
  }, []);

  useEffect(() => {
    listScenarios()
      .then((sc) => {
        setScenarios(sc);
        setSelected((prev) => prev ?? sc[0]?.id ?? null);
      })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : String(e)));
    void refreshRuns();
  }, [refreshRuns]);

  // Polling mientras hay una corrida activa
  useEffect(() => {
    if (activeRun && !pollRef.current) {
      pollRef.current = setInterval(() => { void refreshRuns(); }, POLL_MS);
    }
    if (!activeRun && pollRef.current) {
      clearInterval(pollRef.current);
      pollRef.current = null;
    }
    return () => {
      if (pollRef.current) { clearInterval(pollRef.current); pollRef.current = null; }
    };
  }, [activeRun, refreshRuns]);

  const handleRun = async () => {
    if (!selected) return;
    setStarting(true);
    setError(null);
    try {
      await startLoadTest(selected);
      await refreshRuns();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al iniciar la prueba');
    } finally {
      setStarting(false);
    }
  };

  const handleStop = async (id: string) => {
    try {
      await stopRun(id);
      await refreshRuns();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al detener la prueba');
    }
  };

  return (
    <div className="space-y-5">
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-lg bg-cyan-500/10 flex items-center justify-center">
          <Gauge className="w-5 h-5 text-cyan-400" />
        </div>
        <div>
          <h2 className="text-base font-semibold text-white">Pruebas de Carga</h2>
          <p className="text-xs text-slate-500">Ejecutá escenarios k6 contra la plataforma y validá la cadena de detección</p>
        </div>
      </div>

      {error && (
        <div className="flex items-center gap-2 text-xs text-red-400 bg-red-500/10 border border-red-500/20 rounded-lg px-3 py-2">
          <AlertTriangle className="w-3.5 h-3.5" /> {error}
        </div>
      )}

      <div className="grid lg:grid-cols-2 gap-5">
        {/* Escenarios */}
        <div className="space-y-3">
          {scenarios.map((sc) => {
            const active = sc.id === selected;
            return (
              <div
                key={sc.id}
                onClick={() => setSelected(sc.id)}
                className={`cursor-pointer rounded-xl border p-4 transition-all ${
                  active
                    ? 'bg-cyan-500/5 border-cyan-500/30'
                    : 'bg-slate-900/60 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <h3 className="text-sm font-semibold text-white">{sc.name}</h3>
                    <p className="text-xs text-slate-500 mt-1">{sc.descripcion}</p>
                    <div className="flex flex-wrap items-center gap-2 mt-2 text-[10px] text-slate-400">
                      <span className="inline-flex items-center gap-1 bg-slate-800/50 px-1.5 py-0.5 rounded"><Activity className="w-3 h-3" />{sc.perfil}</span>
                      <span className="inline-flex items-center gap-1 bg-slate-800/50 px-1.5 py-0.5 rounded"><Clock className="w-3 h-3" />{sc.duracion_aprox}</span>
                    </div>
                  </div>
                </div>
              </div>
            );
          })}

          <button
            onClick={() => { void handleRun(); }}
            disabled={!isAdmin || !selected || !!activeRun || starting}
            title={!isAdmin ? 'Requiere rol Admin' : activeRun ? 'Hay una prueba en ejecución' : ''}
            className="w-full flex items-center justify-center gap-2 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white text-sm font-medium px-5 py-2.5 rounded-lg transition-all shadow-lg shadow-cyan-500/20 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {starting || activeRun
              ? <><Loader2 className="w-4 h-4 animate-spin" /> {activeRun ? 'Ejecutando…' : 'Iniciando…'}</>
              : <><Play className="w-4 h-4" /> Ejecutar escenario</>}
          </button>
          {!isAdmin && <p className="text-[10px] text-slate-500 text-center">Solo los administradores pueden disparar pruebas.</p>}
        </div>

        {/* Visor de código */}
        <div className="bg-slate-900/60 border border-slate-800 rounded-xl overflow-hidden flex flex-col">
          <div className="flex items-center gap-2 px-4 py-2.5 border-b border-slate-800 text-xs text-slate-400">
            <FileCode2 className="w-3.5 h-3.5 text-cyan-400" />
            {selectedScenario?.archivo ?? 'Seleccioná un escenario'}
          </div>
          <div className="overflow-auto max-h-[480px] text-xs">
            {selectedScenario ? (
              <SyntaxHighlighter
                language="javascript"
                style={oneDark}
                customStyle={{ margin: 0, background: 'transparent', fontSize: '12px' }}
                showLineNumbers
              >
                {selectedScenario.code}
              </SyntaxHighlighter>
            ) : (
              <div className="p-4 text-slate-600">—</div>
            )}
          </div>
        </div>
      </div>

      {/* Historial de corridas */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <h3 className="text-sm font-semibold text-white mb-4">Historial de corridas</h3>
        {runs.length === 0 ? (
          <p className="text-xs text-slate-500">Todavía no se ejecutó ninguna prueba.</p>
        ) : (
          <div className="space-y-3">
            {runs.map((run) => (
              <div key={run.id} className="bg-slate-950/40 border border-slate-800/50 rounded-lg p-3">
                <div className="flex items-center justify-between gap-3 mb-2">
                  <div className="flex items-center gap-3 min-w-0">
                    <StatusBadge status={run.status} />
                    <span className="text-sm font-medium text-white truncate">{run.scenario_name}</span>
                    <span className="text-[11px] text-slate-500 inline-flex items-center gap-1">
                      <Clock className="w-3 h-3" />{elapsed(run.started_at, run.finished_at)}
                    </span>
                  </div>
                  {run.status === 'running' && isAdmin && (
                    <button
                      onClick={() => { void handleStop(run.id); }}
                      className="inline-flex items-center gap-1 text-[11px] text-red-400 hover:text-red-300 border border-red-500/20 hover:border-red-500/40 rounded px-2 py-1 transition-colors"
                    >
                      <Square className="w-3 h-3" /> Detener
                    </button>
                  )}
                </div>
                {run.summary && <SummaryTable run={run} />}
                {run.log_tail && run.status !== 'running' && (
                  <details className="mt-2">
                    <summary className="text-[11px] text-slate-500 cursor-pointer hover:text-slate-300">Ver salida de k6</summary>
                    <pre className="mt-2 text-[10px] text-slate-400 bg-black/40 rounded p-2 overflow-auto max-h-48 whitespace-pre-wrap">{run.log_tail}</pre>
                  </details>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
