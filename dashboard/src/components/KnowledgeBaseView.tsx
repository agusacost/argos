import { useEffect, useState } from 'react';
import { BookOpen, Search, FileText, Cpu, ChevronRight, Plus, Sparkles, Database } from 'lucide-react';
import { useApp } from '@/types';
import type { IncidentSummary } from '@/types';
import { listKnowledgeBase } from '@/api';
import { deriveComponent } from '@/adapters';

function fmtDate(iso?: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return isNaN(d.getTime()) ? iso : d.toLocaleString('es-AR');
}

const SIZE = 50;

export function KnowledgeBaseView() {
  const { setSelectedIncidentId } = useApp();
  const [rows, setRows] = useState<IncidentSummary[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('Todos');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);

  useEffect(() => {
    setLoading(true);
    listKnowledgeBase({ page, size: SIZE, search: search || undefined })
      .then((r) => {
        setRows(r.data);
        setTotal(r.total);
        setError(null);
      })
      .catch(setError)
      .finally(() => setLoading(false));
  }, [page, search]);

  const categories = ['Todos', ...Array.from(new Set(rows.map((r) => deriveComponent(r.tipo_alerta))))];

  const filtered = selectedCategory === 'Todos'
    ? rows
    : rows.filter((r) => deriveComponent(r.tipo_alerta) === selectedCategory);

  const totalPages = Math.max(1, Math.ceil(total / SIZE));

  return (
    <div className="space-y-5">
      {/* RAG Status Banner */}
      <div className="bg-gradient-to-r from-cyan-500/8 via-blue-500/5 to-transparent border border-cyan-500/20 rounded-xl p-5 flex items-center gap-4">
        <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-cyan-400 to-blue-600 flex items-center justify-center shadow-lg shadow-cyan-500/20 flex-shrink-0">
          <Sparkles className="w-6 h-6 text-white" />
        </div>
        <div className="flex-1">
          <h3 className="text-sm font-semibold text-white">Base de Conocimiento con RAG</h3>
          <p className="text-xs text-slate-400 mt-0.5">
            Retrieval-Augmented Generation · {total} documentos indexados · embeddings ONNX 384 dims · paraphrase-multilingual-MiniLM-L12-v2
          </p>
        </div>
        <div className="flex items-center gap-3">
          <div className="text-right">
            <p className="text-lg font-bold text-cyan-300">0.63</p>
            <p className="text-[10px] text-slate-500">Umbral sim.</p>
          </div>
          <div className="w-px h-10 bg-slate-800" />
          <div className="text-right">
            <p className="text-lg font-bold text-white">{total}</p>
            <p className="text-[10px] text-slate-500">Documentos</p>
          </div>
        </div>
      </div>

      {/* Search + Add */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
          <input
            type="text"
            placeholder="Buscar en base de conocimiento..."
            value={search}
            onChange={(e) => { setPage(1); setSearch(e.target.value); }}
            className="w-full bg-slate-900/60 border border-slate-800 rounded-lg pl-10 pr-4 py-2.5 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-cyan-500/50 transition-all"
          />
        </div>
        <button className="flex items-center gap-2 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white text-sm font-medium px-4 py-2.5 rounded-lg transition-all shadow-lg shadow-cyan-500/20">
          <Plus className="w-4 h-4" />
          Nuevo Documento
        </button>
      </div>

      {/* Category filters */}
      <div className="flex items-center gap-2 flex-wrap">
        {categories.map((cat) => (
          <button
            key={cat}
            onClick={() => setSelectedCategory(cat)}
            className={`text-xs font-medium px-3 py-1.5 rounded-lg transition-all ${
              selectedCategory === cat
                ? 'bg-cyan-500/15 text-cyan-300 border border-cyan-500/20'
                : 'bg-slate-900/40 text-slate-400 border border-slate-800 hover:bg-slate-800/50'
            }`}
          >
            {cat}
          </button>
        ))}
      </div>

      {/* Documents grid */}
      {loading && (
        <div className="py-12 text-center text-slate-500 text-sm">Cargando base de conocimiento…</div>
      )}
      {!loading && error != null && (
        <div className="py-12 text-center text-red-400 text-sm">
          Error: {error instanceof Error ? error.message : String(error)}
        </div>
      )}
      {!loading && !error && filtered.length === 0 && (
        <div className="py-12 text-center text-slate-500 text-sm">
          Todavía no hay documentos auto-generados.
        </div>
      )}
      {!loading && !error && filtered.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {filtered.map((doc) => {
            const component = deriveComponent(doc.tipo_alerta);
            const isAuto = doc.fuente === 'auto-generado';
            return (
              <div
                key={doc.id}
                onClick={() => setSelectedIncidentId(doc.id)}
                className="bg-slate-900/60 border border-slate-800 rounded-xl p-4 hover:border-slate-700 transition-all group cursor-pointer"
              >
                <div className="flex items-start gap-3">
                  <div className="w-9 h-9 rounded-lg bg-slate-800 flex items-center justify-center flex-shrink-0">
                    <FileText className="w-4 h-4 text-slate-400" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <h4 className="text-sm font-medium text-white group-hover:text-cyan-300 transition-colors truncate">
                      {doc.tipo_alerta}
                    </h4>
                    <p className="text-[11px] text-slate-500 mt-1 truncate">{doc.resumen}</p>
                    <div className="flex items-center gap-2 mt-2 flex-wrap">
                      <span className="text-[10px] font-medium px-2 py-0.5 rounded bg-slate-800 text-slate-400">{component}</span>
                      <span className={`text-[10px] font-medium px-2 py-0.5 rounded flex items-center gap-1 ${
                        isAuto ? 'bg-cyan-500/10 text-cyan-400' : 'bg-slate-800 text-slate-500'
                      }`}>
                        {isAuto && <Cpu className="w-2.5 h-2.5" />}
                        {isAuto ? 'Auto-generado' : 'Manual'}
                      </span>
                    </div>
                    <p className="text-[10px] text-slate-600 mt-2 flex items-center gap-1">
                      <Database className="w-2.5 h-2.5" />
                      384-dim · {fmtDate(doc.detected_at ?? doc.fecha)}
                    </p>
                  </div>
                  <ChevronRight className="w-4 h-4 text-slate-600 group-hover:text-cyan-400 transition-colors flex-shrink-0" />
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <span className="text-xs text-slate-500">Página {page} de {totalPages} · {total} documentos</span>
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
      )}

      {/* BookOpen icon if empty */}
      {!loading && !error && rows.length === 0 && (
        <div className="flex flex-col items-center gap-3 py-8 text-slate-600">
          <BookOpen className="w-10 h-10 opacity-30" />
          <p className="text-sm">La base de conocimiento está vacía</p>
        </div>
      )}
    </div>
  );
}
