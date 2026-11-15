'use client';

import { useEffect, useId, useRef, useState, type FormEvent, type KeyboardEvent } from 'react';
import { AlertCircle, BookPlus, X } from 'lucide-react';
import { createKnowledgeDoc } from '@/api';
import type { IncidentDetail } from '@/types';

/** Un paso por línea; se descartan líneas vacías y viñetas/numeración que el operador haya escrito. */
export function parsePasos(text: string): string[] {
  return text
    .split(/\r?\n/)
    .map((l) => l.trim().replace(/^(\d+[.)]|[-*•])\s+/, '').trim())
    .filter(Boolean);
}

type FieldErrors = Partial<Record<'tipo' | 'resumen' | 'contenido', string>>;

const INPUT_CLASS =
  'w-full bg-slate-950/50 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-600 ' +
  'focus:outline-none focus:border-cyan-500/50 disabled:opacity-60';

/**
 * Alta manual de un documento (runbook) en la base de conocimiento. Solo Administrador:
 * el botón que lo abre no se muestra a otros roles y el backend responde 403 igualmente.
 */
export function NewKnowledgeDocModal({ onClose, onCreated }: {
  onClose: () => void;
  onCreated: (doc: IncidentDetail) => void;
}) {
  const uid = useId();
  const id = (name: string) => `${uid}-${name}`;
  const dialogRef = useRef<HTMLDivElement>(null);
  const tipoRef = useRef<HTMLInputElement>(null);

  const [tipo, setTipo] = useState('');
  const [resumen, setResumen] = useState('');
  const [severity, setSeverity] = useState<'' | 'warning' | 'critical'>('');
  const [pasosText, setPasosText] = useState('');
  const [documento, setDocumento] = useState('');
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Foco inicial en el primer campo; al cerrar se devuelve al elemento que abrió el formulario.
  useEffect(() => {
    const opener = document.activeElement as HTMLElement | null;
    tipoRef.current?.focus();
    return () => { opener?.focus?.(); };
  }, []);

  const handleKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    if (e.key === 'Escape' && !saving) {
      e.stopPropagation();
      onClose();
      return;
    }
    if (e.key !== 'Tab') return;
    // Trampa de foco: Tab/Shift+Tab ciclan dentro del formulario.
    const focusables = dialogRef.current?.querySelectorAll<HTMLElement>(
      'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled])',
    );
    if (!focusables || focusables.length === 0) return;
    const first = focusables[0];
    const last = focusables[focusables.length - 1];
    if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
    else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (saving) return;
    const pasos = parsePasos(pasosText);
    const body = documento.trim();
    const errors: FieldErrors = {};
    if (!tipo.trim()) errors.tipo = 'El tipo de alerta es obligatorio (por ejemplo, NginxDown).';
    if (!resumen.trim()) errors.resumen = 'El resumen es obligatorio.';
    if (pasos.length === 0 && !body) {
      errors.contenido = 'Falta contenido: cargue al menos un paso de resolución o el documento en Markdown.';
    }
    setFieldErrors(errors);
    setSubmitError(null);
    if (Object.keys(errors).length > 0) {
      const firstInvalid = errors.tipo ? tipoRef.current : dialogRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]');
      firstInvalid?.focus();
      return;
    }

    setSaving(true);
    try {
      const created = await createKnowledgeDoc({
        tipo_alerta: tipo.trim(),
        resumen: resumen.trim(),
        pasos,
        ...(body ? { documento: body } : {}),
        ...(severity ? { severity } : {}),
      });
      onCreated(created);
    } catch (err: unknown) {
      const msg = err instanceof TypeError
        ? 'no se pudo contactar con el servidor'
        : err instanceof Error ? err.message.trim().replace(/[.\s]+$/, '') : String(err);
      setSubmitError(`No se pudo crear el documento: ${msg}. Los datos cargados se conservan; se puede corregir y reintentar.`);
    } finally {
      setSaving(false);
    }
  };

  const fieldError = (key: keyof FieldErrors) => fieldErrors[key] != null && (
    <p id={id(`${key}-err`)} className="text-xs text-red-400 mt-1">{fieldErrors[key]}</p>
  );
  const invalid = (key: keyof FieldErrors) => ({
    'aria-invalid': fieldErrors[key] != null ? true : undefined,
    'aria-describedby': fieldErrors[key] != null ? id(`${key}-err`) : undefined,
  });

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm animate-fade-in"
        onClick={() => { if (!saving) onClose(); }}
      />
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={id('title')}
        onKeyDown={handleKeyDown}
        className="relative w-full max-w-2xl max-h-[90vh] overflow-y-auto bg-[#0d1117] border border-slate-800 rounded-xl shadow-2xl"
      >
        <form onSubmit={(e) => { void handleSubmit(e); }} noValidate>
          <div className="sticky top-0 bg-[#0d1117]/95 backdrop-blur-sm border-b border-slate-800 px-6 py-4 flex items-center gap-3 z-10">
            <div className="w-9 h-9 rounded-lg bg-cyan-500/10 flex items-center justify-center flex-shrink-0">
              <BookPlus className="w-4 h-4 text-cyan-400" />
            </div>
            <div className="flex-1 min-w-0">
              <h2 id={id('title')} className="text-base font-semibold text-white">Nuevo documento</h2>
              <p className="text-xs text-slate-500">
                Runbook manual para la base de conocimiento; el RAG lo usa para reconocer alertas parecidas.
              </p>
            </div>
            <button
              type="button"
              onClick={onClose}
              disabled={saving}
              aria-label="Cerrar formulario"
              className="text-slate-500 hover:text-white p-1 disabled:opacity-50"
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          <div className="px-6 py-5 space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div className="sm:col-span-2">
                <label htmlFor={id('tipo')} className="block text-xs font-medium text-slate-400 mb-1.5">
                  Tipo de alerta <span className="text-red-400" aria-hidden="true">*</span>
                </label>
                <input
                  ref={tipoRef}
                  id={id('tipo')}
                  type="text"
                  required
                  aria-required="true"
                  disabled={saving}
                  value={tipo}
                  onChange={(e) => { setTipo(e.target.value); setFieldErrors((f) => ({ ...f, tipo: undefined })); }}
                  className={INPUT_CLASS}
                  placeholder="NginxDown"
                  {...invalid('tipo')}
                />
                {fieldError('tipo')}
              </div>
              <div>
                <label htmlFor={id('severity')} className="block text-xs font-medium text-slate-400 mb-1.5">Severidad</label>
                <select
                  id={id('severity')}
                  disabled={saving}
                  value={severity}
                  onChange={(e) => setSeverity(e.target.value as '' | 'warning' | 'critical')}
                  className={INPUT_CLASS}
                >
                  <option value="">Por defecto</option>
                  <option value="warning">Warning</option>
                  <option value="critical">Crítico</option>
                </select>
              </div>
            </div>

            <div>
              <label htmlFor={id('resumen')} className="block text-xs font-medium text-slate-400 mb-1.5">
                Resumen <span className="text-red-400" aria-hidden="true">*</span>
              </label>
              <input
                id={id('resumen')}
                type="text"
                required
                aria-required="true"
                disabled={saving}
                value={resumen}
                onChange={(e) => { setResumen(e.target.value); setFieldErrors((f) => ({ ...f, resumen: undefined })); }}
                className={INPUT_CLASS}
                placeholder="Qué pasa y cómo se manifiesta"
                {...invalid('resumen')}
              />
              {fieldError('resumen')}
            </div>

            <div>
              <label htmlFor={id('pasos')} className="block text-xs font-medium text-slate-400 mb-1.5">
                Pasos de resolución
              </label>
              <textarea
                id={id('pasos')}
                rows={5}
                disabled={saving}
                value={pasosText}
                onChange={(e) => { setPasosText(e.target.value); setFieldErrors((f) => ({ ...f, contenido: undefined })); }}
                className={`${INPUT_CLASS} resize-y`}
                placeholder={'Un paso por línea\nVerificar el estado del contenedor\nReiniciar el servicio'}
                aria-describedby={id('pasos-help')}
              />
              <p id={id('pasos-help')} className="text-[10px] text-slate-600 mt-1">Un paso por línea.</p>
            </div>

            <div>
              <label htmlFor={id('documento')} className="block text-xs font-medium text-slate-400 mb-1.5">
                Documento (Markdown)
              </label>
              <textarea
                id={id('documento')}
                rows={8}
                disabled={saving}
                value={documento}
                onChange={(e) => { setDocumento(e.target.value); setFieldErrors((f) => ({ ...f, contenido: undefined })); }}
                className={`${INPUT_CLASS} resize-y font-mono text-xs`}
                placeholder={'## Contexto\n...\n\n## Diagnóstico\n...'}
                {...invalid('contenido')}
              />
              <p className="text-[10px] text-slate-600 mt-1">
                Opcional si se cargan pasos. Se muestra formateado al abrir el documento.
              </p>
              {fieldError('contenido')}
            </div>

            {submitError != null && (
              <div role="alert" className="flex items-start gap-2 text-xs text-red-300 bg-red-500/10 border border-red-500/30 rounded-lg px-3 py-2.5">
                <AlertCircle className="w-3.5 h-3.5 mt-0.5 flex-shrink-0" />
                <span>{submitError}</span>
              </div>
            )}
          </div>

          <div className="sticky bottom-0 bg-[#0d1117] border-t border-slate-800 px-6 py-4 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={saving}
              className="px-4 py-2.5 text-sm text-slate-300 bg-slate-800 hover:bg-slate-700 border border-slate-700 rounded-lg transition-all disabled:opacity-50"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={saving}
              className="flex items-center gap-2 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white text-sm font-medium px-5 py-2.5 rounded-lg transition-all shadow-lg shadow-cyan-500/20 disabled:opacity-60"
            >
              {saving
                ? <><span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Creando…</>
                : 'Crear documento'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
