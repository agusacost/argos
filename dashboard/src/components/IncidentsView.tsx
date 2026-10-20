import { useEffect, useState } from 'react';
import { Search, Filter } from 'lucide-react';
import { useApp } from '@/types';
import type { IncidentVM } from '@/types';
import { listIncidents } from '@/api';
import { toIncidentVM } from '@/adapters';
import { IncidentRows } from './IncidentTable';

const ESTADOS = [
  { value: '', label: 'Todos los estados' },
  { value: 'nuevo', label: 'Nuevo' },
  { value: 'pendiente', label: 'Pendiente' },
  { value: 'resuelto', label: 'Resuelto' },
  { value: 'auto_documentado', label: 'Auto-documentado' },
  { value: 'documentado_previamente', label: 'Documentado previamente' },
];

const SIZE = 20;

export function IncidentsView() {
  const { selectedIncidentId, setSelectedIncidentId } = useApp();
  const [rows, setRows] = useState<IncidentVM[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [estado, setEstado] = useState('');
  const [tipo, setTipo] = useState('');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);

  useEffect(() => {
    setLoading(true);
    listIncidents({
      page,
      size: SIZE,
      estado: estado || undefined,
      tipo: tipo || undefined,
      search: search || undefined,
    })
      .then((r) => {
        setRows(r.data.map(toIncidentVM));
        setTotal(r.total);
        setError(null);
      })
      .catch(setError)
      .finally(() => setLoading(false));
  }, [page, estado, tipo, search]);

  const totalPages = Math.max(1, Math.ceil(total / SIZE));

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
          <input
            placeholder="Buscar por resumen o tipo…"
            value={search}
            onChange={(e) => { setPage(1); setSearch(e.target.value); }}
            className="w-full bg-slate-900/60 border border-slate-800 rounded-lg pl-10 pr-4 py-2.5 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-cyan-500/50 transition-all"
          />
        </div>
        <div className="relative">
          <Filter className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
          <select
            value={estado}
            onChange={(e) => { setPage(1); setEstado(e.target.value); }}
            className="bg-slate-900/60 border border-slate-800 rounded-lg pl-10 pr-4 py-2.5 text-sm text-white focus:outline-none focus:border-cyan-500/50 transition-all appearance-none"
          >
            {ESTADOS.map((s) => (
              <option key={s.value} value={s.value}>{s.label}</option>
            ))}
          </select>
        </div>
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
          <input
            placeholder="Filtrar por tipo…"
            value={tipo}
            onChange={(e) => { setPage(1); setTipo(e.target.value); }}
            className="bg-slate-900/60 border border-slate-800 rounded-lg pl-10 pr-4 py-2.5 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-cyan-500/50 transition-all w-48"
          />
        </div>
      </div>

      {/* Table */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl overflow-hidden">
        <div className="flex items-center justify-between px-5 py-4 border-b border-slate-800">
          <div>
            <h3 className="text-sm font-semibold text-white">Historial de Incidentes</h3>
            <p className="text-xs text-slate-500 mt-0.5">{total} incidentes · Click para ver detalle</p>
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

        {loading && (
          <div className="px-5 py-12 text-center text-slate-500 text-sm">Cargando incidentes…</div>
        )}
        {!loading && error != null && (
          <div className="px-5 py-12 text-center text-red-400 text-sm">
            Error al cargar: {error instanceof Error ? error.message : String(error)}
          </div>
        )}
        {!loading && !error && rows.length === 0 && (
          <div className="px-5 py-12 text-center text-slate-500 text-sm">Sin incidentes que coincidan.</div>
        )}
        {!loading && !error && rows.length > 0 && (
          <IncidentRows rows={rows} selectedId={selectedIncidentId} onSelect={setSelectedIncidentId} />
        )}
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-between">
        <span className="text-xs text-slate-500">
          Página {page} de {totalPages} · {total} incidentes
        </span>
        <div className="flex items-center gap-2">
          <button
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="px-3 py-1.5 text-xs bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg border border-slate-700 disabled:opacity-40 disabled:cursor-default transition-all"
          >
            ← Anterior
          </button>
          <button
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="px-3 py-1.5 text-xs bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg border border-slate-700 disabled:opacity-40 disabled:cursor-default transition-all"
          >
            Siguiente →
          </button>
        </div>
      </div>
    </div>
  );
}
