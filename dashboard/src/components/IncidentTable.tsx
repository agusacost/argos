import { useState, useEffect } from 'react';
import {
  AlertOctagon, AlertTriangle, Info, ChevronRight, Cpu, CheckCircle2,
  FileCheck2, Clock, UserCircle,
} from 'lucide-react';
import ReactMarkdown from 'react-markdown';
import { useApp } from '@/types';
import type { Severity, UIStatus, IncidentVM } from '@/types';
import { getIncident, getIncidentObservability, getIncidentAudit } from '@/api';
import type { IncidentObservability, AuditEntry } from '@/api';
import { toIncidentVM } from '@/adapters';
import { Sparkline } from './Sparkline';

// ─── Config maps ──────────────────────────────────────────────────────────────

const severityConfig: Record<Severity, { icon: React.ElementType; color: string; bg: string; border: string; label: string }> = {
  critical: { icon: AlertOctagon, color: 'text-red-400', bg: 'bg-red-500/10', border: 'border-red-500/30', label: 'Crítico' },
  warning: { icon: AlertTriangle, color: 'text-amber-400', bg: 'bg-amber-500/10', border: 'border-amber-500/30', label: 'Warning' },
  info: { icon: Info, color: 'text-blue-400', bg: 'bg-blue-500/10', border: 'border-blue-500/30', label: 'Info' },
};

const statusConfig: Record<UIStatus, { color: string; bg: string; label: string; icon: React.ElementType }> = {
  ai_analyzing: { color: 'text-cyan-300', bg: 'bg-cyan-500/10 border-cyan-500/20', label: 'Analizando IA', icon: Cpu },
  investigating: { color: 'text-amber-300', bg: 'bg-amber-500/10 border-amber-500/20', label: 'Investigando', icon: AlertTriangle },
  resolved: { color: 'text-emerald-300', bg: 'bg-emerald-500/10 border-emerald-500/20', label: 'Resuelto', icon: CheckCircle2 },
  documented: { color: 'text-violet-300', bg: 'bg-violet-500/10 border-violet-500/20', label: 'Documentado', icon: FileCheck2 },
};

function formatTime(ts: string) {
  const d = new Date(ts);
  const now = new Date();
  const diff = Math.floor((now.getTime() - d.getTime()) / 60000);
  if (diff < 1) return 'Ahora';
  if (diff < 60) return `Hace ${diff} min`;
  const hours = Math.floor(diff / 60);
  if (hours < 24) return `Hace ${hours}h`;
  return `Hace ${Math.floor(hours / 24)}d`;
}

// ─── Dashboard IncidentTable (reads from context, no pagination) ───────────────

export function IncidentTable() {
  const { incidents, selectedIncidentId, setSelectedIncidentId } = useApp();
  const displayed = incidents.slice(0, 8);

  return (
    <div className="bg-slate-900/60 border border-slate-800 rounded-xl overflow-hidden">
      <div className="flex items-center justify-between px-5 py-4 border-b border-slate-800">
        <div>
          <h3 className="text-sm font-semibold text-white">Incidentes Recientes</h3>
          <p className="text-xs text-slate-500 mt-0.5">{incidents.length} incidentes · Click para ver detalle</p>
        </div>
      </div>

      <div className="px-5 py-2.5 grid grid-cols-12 gap-3 text-[10px] font-medium text-slate-600 uppercase tracking-wider border-b border-slate-800/50 bg-slate-950/30">
        <div className="col-span-1">ID</div>
        <div className="col-span-2">Severidad</div>
        <div className="col-span-2">Componente</div>
        <div className="col-span-4">Descripción</div>
        <div className="col-span-2">Estado</div>
        <div className="col-span-1 text-right">Tiempo</div>
      </div>

      <div className="divide-y divide-slate-800/40">
        {displayed.length === 0 && (
          <div className="px-5 py-8 text-center text-slate-500 text-sm">Sin incidentes cargados</div>
        )}
        {displayed.map((inc) => <IncidentRow key={inc.id} inc={inc} selectedId={selectedIncidentId} onSelect={setSelectedIncidentId} />)}
      </div>
    </div>
  );
}

// ─── Shared row component ─────────────────────────────────────────────────────

function IncidentRow({ inc, selectedId, onSelect }: {
  inc: IncidentVM;
  selectedId: string | null;
  onSelect: (id: string) => void;
}) {
  const sev = severityConfig[inc.severity];
  const stat = statusConfig[inc.status];
  const SevIcon = sev.icon;
  const StatIcon = stat.icon;
  const isSelected = selectedId === inc.id;

  return (
    <button
      onClick={() => onSelect(inc.id)}
      className={`w-full px-5 py-3.5 grid grid-cols-12 gap-3 items-center text-left transition-all hover:bg-slate-800/30 ${
        isSelected ? 'bg-cyan-500/5 border-l-2 border-l-cyan-500' : 'border-l-2 border-l-transparent'
      }`}
    >
      <div className="col-span-1 font-mono text-[10px] text-slate-500 truncate">{inc.id.slice(0, 8)}</div>

      <div className="col-span-2">
        <div className={`inline-flex items-center gap-1.5 px-2 py-1 rounded-md ${sev.bg} ${sev.border} border`}>
          <SevIcon className={`w-3 h-3 ${sev.color}`} />
          <span className={`text-[11px] font-medium ${sev.color}`}>{sev.label}</span>
        </div>
      </div>

      <div className="col-span-2">
        <span className="text-xs font-medium text-slate-300">{inc.component}</span>
      </div>

      <div className="col-span-4">
        <p className="text-xs text-white font-medium truncate">{inc.title}</p>
        <p className="text-[11px] text-slate-500 truncate">{inc.description}</p>
      </div>

      <div className="col-span-2">
        <div className={`inline-flex items-center gap-1.5 px-2 py-1 rounded-md border ${stat.bg}`}>
          <StatIcon className={`w-3 h-3 ${stat.color}`} />
          <span className={`text-[11px] font-medium ${stat.color}`}>{stat.label}</span>
        </div>
      </div>

      <div className="col-span-1 flex items-center justify-end gap-1">
        <span className="text-[10px] text-slate-600 hidden lg:inline">{formatTime(inc.timestamp)}</span>
        <ChevronRight className="w-3.5 h-3.5 text-slate-600" />
      </div>
    </button>
  );
}

// ─── Full paginated incidents table (used in IncidentsView) ───────────────────

export function IncidentRows({ rows, selectedId, onSelect }: {
  rows: IncidentVM[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}) {
  return (
    <div className="divide-y divide-slate-800/40">
      {rows.map((inc) => <IncidentRow key={inc.id} inc={inc} selectedId={selectedId} onSelect={onSelect} />)}
    </div>
  );
}

// ─── Incident Detail Drawer ───────────────────────────────────────────────────

interface DrawerContentProps {
  vm: IncidentVM;
  canApprove: boolean;
  onClose: () => void;
  onResolve: () => void;
  onDocument: () => void;
}

function DrawerContent({ vm, canApprove, onClose, onResolve, onDocument }: DrawerContentProps) {
  const sev = severityConfig[vm.severity];
  const stat = statusConfig[vm.status];
  const SevIcon = sev.icon;
  const StatIcon = stat.icon;
  const sevColor = sev.color === 'text-red-400' ? '#f87171' : sev.color === 'text-amber-400' ? '#fbbf24' : '#60a5fa';

  const [observability, setObservability] = useState<IncidentObservability | null>(null);
  const [audit, setAudit] = useState<AuditEntry[]>([]);
  const [documenting, setDocumenting] = useState(false);

  useEffect(() => {
    setObservability(null);
    setAudit([]);
    void getIncidentObservability(vm.id).then(setObservability).catch(() => setObservability({ logs: [], sparkline: [] }));
    void getIncidentAudit(vm.id).then(setAudit).catch(() => setAudit([]));
  }, [vm.id]);

  const handleDocument = () => {
    setDocumenting(true);
    onDocument();
  };

  return (
    <>
      {/* Header */}
      <div className="sticky top-0 bg-[#0d1117]/95 backdrop-blur-sm border-b border-slate-800 px-6 py-4 z-10">
        <div className="flex items-start justify-between gap-4">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-2">
              <span className="font-mono text-[10px] text-slate-500">{vm.id.slice(0, 8)}…</span>
              <div className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md ${sev.bg} ${sev.border} border`}>
                <SevIcon className={`w-3 h-3 ${sev.color}`} />
                <span className={`text-[11px] font-medium ${sev.color}`}>{sev.label}</span>
              </div>
              <div className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md border ${stat.bg}`}>
                <StatIcon className={`w-3 h-3 ${stat.color}`} />
                <span className={`text-[11px] font-medium ${stat.color}`}>{stat.label}</span>
              </div>
            </div>
            <h2 className="text-base font-semibold text-white">{vm.title}</h2>
            <p className="text-xs text-slate-500 mt-1">{vm.component} · {formatTime(vm.timestamp)}</p>
          </div>
          <button onClick={onClose} className="text-slate-500 hover:text-white p-1 -mr-1 flex-shrink-0">
            <ChevronRight className="w-5 h-5 rotate-180" />
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="px-6 py-5 space-y-6">
        {/* Métricas — sparkline de Prometheus */}
        <section>
          <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3">Métricas de la Alerta</h3>
          <div className="grid grid-cols-2 gap-3">
            <div className="bg-slate-950/50 border border-slate-800 rounded-lg p-3">
              <p className="text-[10px] text-slate-500 mb-1">MTTR</p>
              <p className="text-sm font-bold text-white font-mono mb-1.5">
                {vm.mttr_minutes != null ? `${Math.round(vm.mttr_minutes)} min` : '—'}
              </p>
              {observability && observability.sparkline.length > 0 && (
                <Sparkline data={observability.sparkline.slice(-12)} width={90} height={24} color={sevColor} />
              )}
              {observability && observability.sparkline.length === 0 && (
                <span className="text-[10px] text-slate-600">Sin datos Prometheus</span>
              )}
              {!observability && <span className="text-[10px] text-slate-600">Cargando…</span>}
            </div>
            <div className="bg-slate-950/50 border border-slate-800 rounded-lg p-3">
              <p className="text-[10px] text-slate-500 mb-1">Componente</p>
              <p className="text-sm font-bold text-white font-mono mb-1.5">{vm.component}</p>
              <p className="text-[10px] text-slate-500">Severidad: <span className={`font-medium ${sev.color}`}>{sev.label}</span></p>
            </div>
          </div>
        </section>

        {/* Logs — Loki */}
        <section>
          <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3">Logs Relevantes (Loki)</h3>
          <div className="bg-black/40 border border-slate-800 rounded-lg p-4 font-mono text-[11px] space-y-1.5 max-h-44 overflow-y-auto">
            {!observability && (
              <span className="text-slate-600">Cargando logs…</span>
            )}
            {observability && observability.logs.length === 0 && (
              <span className="text-slate-600">Sin logs en Loki para esta ventana temporal.</span>
            )}
            {observability && observability.logs.map((log, i) => {
              const isWarn = log.toUpperCase().includes('WARN') || log.toUpperCase().includes('ERROR');
              const isInfo = log.toUpperCase().includes('INFO');
              return (
                <div key={i} className="flex gap-2">
                  <span className={isWarn ? 'text-amber-400' : isInfo ? 'text-cyan-400' : 'text-slate-500'}>
                    {isWarn ? '⚠' : isInfo ? 'ℹ' : '·'}
                  </span>
                  <span className={`break-all ${isWarn ? 'text-amber-300/80' : isInfo ? 'text-slate-300' : 'text-slate-400'}`}>
                    {log}
                  </span>
                </div>
              );
            })}
          </div>
        </section>

        {/* AI Analysis — real resumen + real pasos */}
        <section className="relative">
          <div className="bg-gradient-to-br from-cyan-500/8 via-blue-500/5 to-transparent border border-cyan-500/20 rounded-xl p-5 relative overflow-hidden animate-pulse-glow">
            <div className="absolute top-0 right-0 w-32 h-32 bg-cyan-500/10 rounded-full blur-3xl" />
            <div className="relative">
              <div className="flex items-center gap-2.5 mb-3">
                <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-cyan-400 to-blue-600 flex items-center justify-center shadow-md shadow-cyan-500/30">
                  <Cpu className="w-4 h-4 text-white" />
                </div>
                <div>
                  <h3 className="text-sm font-semibold text-white">Análisis de Argos</h3>
                  <p className="text-[10px] text-cyan-400/70 font-medium">LLM + RAG · Síntesis automática</p>
                </div>
              </div>

              <p className="text-xs text-slate-300 leading-relaxed mb-4">{vm.aiSummary}</p>

              {vm.aiSteps.length > 0 && (
                <div>
                  <p className="text-xs font-medium text-cyan-300 mb-2.5 flex items-center gap-1.5">
                    <CheckCircle2 className="w-3.5 h-3.5" />
                    Pasos de Resolución Recomendados
                  </p>
                  <div className="space-y-2">
                    {vm.aiSteps.map((step) => (
                      <div key={step.id} className="flex items-start gap-2.5">
                        <div className={`w-4 h-4 rounded border flex items-center justify-center flex-shrink-0 mt-0.5 ${
                          step.done ? 'bg-cyan-500/20 border-cyan-500/40' : 'border-slate-700'
                        }`}>
                          {step.done && <CheckCircle2 className="w-3 h-3 text-cyan-400" />}
                        </div>
                        <span className={`text-xs ${step.done ? 'text-slate-400 line-through' : 'text-slate-300'}`}>
                          {step.text}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>
        </section>

        {/* Document (real markdown from backend) */}
        {vm.documento && (
          <section>
            <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3">Documentación Auto-generada</h3>
            <div className="bg-slate-950/40 border border-slate-800 rounded-xl p-5 prose prose-invert prose-sm max-w-none
              prose-headings:text-slate-200 prose-p:text-slate-400 prose-li:text-slate-400
              prose-code:bg-slate-900 prose-code:px-1 prose-code:rounded prose-pre:bg-slate-900">
              <ReactMarkdown>{vm.documento}</ReactMarkdown>
            </div>
          </section>
        )}

        {/* Audit trail — real */}
        <section>
          <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3">Trazabilidad de Auditoría</h3>
          {audit.length === 0 && (
            <p className="text-xs text-slate-600">Sin entradas de auditoría aún.</p>
          )}
          <div className="space-y-0">
            {audit.map((entry, i) => {
              const isAI = entry.user === 'Argos IA' || entry.user.includes('@') === false && entry.user.length < 4;
              return (
                <div key={entry.id} className="flex gap-3 group">
                  <div className="flex flex-col items-center">
                    <div className={`w-7 h-7 rounded-full flex items-center justify-center flex-shrink-0 text-[10px] font-semibold ${
                      isAI
                        ? 'bg-gradient-to-br from-cyan-400 to-blue-600 text-white'
                        : 'bg-slate-800 text-slate-400'
                    }`}>
                      {isAI ? <Cpu className="w-3.5 h-3.5" /> : entry.user.split('@')[0].slice(0, 2).toUpperCase()}
                    </div>
                    {i < audit.length - 1 && <div className="w-px h-6 bg-slate-800 mt-1" />}
                  </div>
                  <div className="pb-4 flex-1">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-medium text-white">{entry.user}</span>
                    </div>
                    <p className="text-xs text-slate-400 mt-0.5">{entry.action}</p>
                    <p className="text-[10px] text-slate-600 mt-0.5 flex items-center gap-1">
                      <Clock className="w-2.5 h-2.5" />
                      {new Date(entry.timestamp).toLocaleString('es-AR')}
                    </p>
                  </div>
                </div>
              );
            })}
          </div>
        </section>

        {/* Assignee */}
        {vm.assignee && (
          <section className="flex items-center gap-3 bg-slate-950/40 border border-slate-800 rounded-lg p-3">
            <UserCircle className="w-5 h-5 text-slate-500" />
            <span className="text-xs text-slate-400">Asignado a</span>
            <span className="text-xs font-medium text-white">{vm.assignee}</span>
          </section>
        )}

        {/* MTTR info */}
        {vm.mttr_minutes != null && (
          <section className="flex items-center gap-3 bg-slate-950/40 border border-slate-800 rounded-lg p-3">
            <Clock className="w-5 h-5 text-slate-500" />
            <span className="text-xs text-slate-400">MTTR</span>
            <span className="text-xs font-medium text-cyan-300">{Math.round(vm.mttr_minutes)} minutos</span>
          </section>
        )}

        {/* Action Buttons */}
        <section className="sticky bottom-0 bg-[#0d1117] -mx-6 px-6 py-4 border-t border-slate-800 flex gap-3">
          {vm.status !== 'resolved' && vm.status !== 'documented' && (
            <>
              {canApprove && (
                <button
                  onClick={handleDocument}
                  disabled={documenting}
                  className="flex-1 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white text-sm font-medium py-2.5 rounded-lg transition-all shadow-lg shadow-cyan-500/20 flex items-center justify-center gap-2 disabled:opacity-70"
                >
                  {documenting ? (
                    <><span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Generando doc…</>
                  ) : (
                    <><FileCheck2 className="w-4 h-4" /> Aprobar y Auto-documentar</>
                  )}
                </button>
              )}
              <button
                onClick={onResolve}
                className="flex-1 bg-slate-800 hover:bg-slate-700 text-slate-200 text-sm font-medium py-2.5 rounded-lg transition-all flex items-center justify-center gap-2 border border-slate-700"
              >
                <CheckCircle2 className="w-4 h-4" />
                Marcar como Resuelto
              </button>
            </>
          )}
          {vm.status === 'resolved' && (
            <div className="flex-1 flex items-center justify-center gap-2 text-sm text-emerald-400 bg-emerald-500/5 border border-emerald-500/20 py-2.5 rounded-lg">
              <CheckCircle2 className="w-4 h-4" />
              Incidente resuelto
            </div>
          )}
          {vm.status === 'documented' && (
            <div className="flex-1 flex items-center justify-center gap-2 text-sm text-violet-300 bg-violet-500/5 border border-violet-500/20 py-2.5 rounded-lg">
              <FileCheck2 className="w-4 h-4" />
              Auto-documentado en base de conocimiento
            </div>
          )}
        </section>
      </div>
    </>
  );
}

export function IncidentDetailDrawer() {
  const { selectedIncidentId, setSelectedIncidentId, user, resolveIncident, documentIncident } = useApp();
  const [vm, setVm] = useState<IncidentVM | null>(null);
  const [loading, setLoading] = useState(false);
  const [fetchError, setFetchError] = useState<string | null>(null);

  useEffect(() => {
    if (!selectedIncidentId) {
      setVm(null);
      return;
    }
    let alive = true;
    setLoading(true);
    setFetchError(null);
    getIncident(selectedIncidentId)
      .then((d) => { if (alive) setVm(toIncidentVM(d)); })
      .catch((e: unknown) => {
        if (alive) setFetchError(e instanceof Error ? e.message : String(e));
      })
      .finally(() => { if (alive) setLoading(false); });
    return () => { alive = false; };
  }, [selectedIncidentId]);

  if (!selectedIncidentId) return null;

  const canApprove = user?.role === 'Admin';

  return (
    <>
      <div
        className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40 animate-fade-in"
        onClick={() => setSelectedIncidentId(null)}
      />
      <div className="fixed right-0 top-0 bottom-0 w-full max-w-xl bg-[#0d1117] border-l border-slate-800 z-50 overflow-y-auto animate-slide-in-right">
        {loading && (
          <div className="flex items-center justify-center h-full">
            <span className="text-slate-500">Cargando incidente…</span>
          </div>
        )}
        {!loading && fetchError && (
          <div className="flex flex-col items-center justify-center h-full gap-3">
            <span className="text-red-400 text-sm">Error: {fetchError}</span>
            <button
              onClick={() => setSelectedIncidentId(null)}
              className="text-xs text-slate-500 hover:text-white"
            >
              Cerrar
            </button>
          </div>
        )}
        {!loading && !fetchError && vm && (
          <DrawerContent
            vm={vm}
            canApprove={canApprove ?? false}
            onClose={() => setSelectedIncidentId(null)}
            onResolve={() => { void resolveIncident(vm.id); }}
            onDocument={() => { void documentIncident(vm.id); }}
          />
        )}
      </div>
    </>
  );
}
