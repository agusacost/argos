import { Bell, Shield, Cpu, Database, Webhook, Save, Sliders, CheckCircle2, AlertCircle, Mail, Plus, X, RefreshCw, Activity } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { checkLlmHealth, getIntegrationsStatus, getSettings, updateSettings } from '@/api';
import type { IntegrationStatus, LlmHealth, Settings } from '@/api';
import { useApp } from '@/types';
import { PROVIDER_LABEL } from '@/llmStatus';

const MAX_RECIPIENTS = 50;
const EMAIL_RE = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const DEFAULT_MODEL: Record<string, string> = {
  gemini: 'gemini-flash-lite-latest',
  anthropic: 'claude-sonnet-4-6',
  groq: 'llama-3.3-70b-versatile',
  openrouter: 'nvidia/nemotron-3-ultra-550b-a55b:free',
};
const KEY_ENV: Record<string, string> = {
  gemini: 'GEMINI_API_KEY',
  anthropic: 'ANTHROPIC_API_KEY',
  groq: 'GROQ_API_KEY',
  openrouter: 'OPENROUTER_API_KEY',
};
const INTEGRATIONS_REFRESH_MS = 30_000;

const HEALTH_DOT: Record<LlmHealth['status'], string> = {
  up: 'bg-emerald-400',
  down: 'bg-red-500',
  unknown: 'bg-slate-500',
  unconfigured: 'border border-slate-600',
};

const formatTime = (iso: string | null) =>
  iso ? new Date(iso).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' }) : '';

function healthDetail(h: LlmHealth): { text: string; tone: string } {
  if (h.status === 'unconfigured') return { text: `falta ${KEY_ENV[h.provider] ?? 'la API key'}`, tone: 'text-slate-500' };
  if (h.status === 'down') {
    const retry = h.retry_after ? ` · reintenta ${formatTime(h.retry_after)}` : ' · se reintenta en la próxima alerta';
    return { text: `${h.last_error ?? 'falló'}${retry}`, tone: 'text-red-400' };
  }
  if (h.status === 'up') return { text: `${h.latency_ms ?? '?'} ms · ${formatTime(h.last_checked_at)}`, tone: 'text-slate-500' };
  return { text: 'sin llamadas todavía', tone: 'text-slate-500' };
}

export function SettingsView() {
  const { user, setPollingSeconds, llmHealth, setLlmHealth } = useApp();
  const [checkingLlm, setCheckingLlm] = useState(false);
  const [llmCheckError, setLlmCheckError] = useState<string | null>(null);
  const [settings, setSettings] = useState<Settings | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [warning, setWarning] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [newRecipient, setNewRecipient] = useState('');
  const [recipientError, setRecipientError] = useState<string | null>(null);
  const [integrations, setIntegrations] = useState<IntegrationStatus[] | null>(null);
  const [checkingIntegrations, setCheckingIntegrations] = useState(false);

  useEffect(() => {
    getSettings()
      .then(setSettings)
      .catch((e: unknown) => setError(e instanceof Error ? e.message : String(e)))
      .finally(() => setLoading(false));
  }, []);

  const refreshIntegrations = useCallback(async () => {
    setCheckingIntegrations(true);
    try {
      setIntegrations(await getIntegrationsStatus());
    } catch {
      setIntegrations([]);
    } finally {
      setCheckingIntegrations(false);
    }
  }, []);

  useEffect(() => {
    void refreshIntegrations();
    const id = setInterval(() => { void refreshIntegrations(); }, INTEGRATIONS_REFRESH_MS);
    return () => clearInterval(id);
  }, [refreshIntegrations]);

  const runLlmCheck = async () => {
    setCheckingLlm(true);
    setLlmCheckError(null);
    try {
      setLlmHealth(await checkLlmHealth());
    } catch (e: unknown) {
      setLlmCheckError(e instanceof Error ? e.message : 'No se pudo probar los modelos');
    } finally {
      setCheckingLlm(false);
    }
  };

  const handleSave = async () => {
    if (!settings) return;
    setSaving(true);
    setError(null);
    setSaved(false);
    setWarning(null);
    try {
      const updated = await updateSettings(settings);
      setSettings(updated);
      setPollingSeconds(updated.polling_interval_seconds);
      if (updated.warning) setWarning(updated.warning);
      setSaved(true);
      setTimeout(() => setSaved(false), 2500);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Error al guardar');
    } finally {
      setSaving(false);
    }
  };

  const isAdmin = user?.role === 'Admin';

  const addRecipient = () => {
    const email = newRecipient.trim().toLowerCase();
    if (!email) return;
    const current = settings?.alert_recipients ?? [];
    if (!EMAIL_RE.test(email)) { setRecipientError('Email inválido'); return; }
    if (current.includes(email)) { setRecipientError('Ya está en la lista'); return; }
    if (current.length >= MAX_RECIPIENTS) { setRecipientError(`Máximo ${MAX_RECIPIENTS} destinatarios`); return; }
    setSettings((s) => s ? { ...s, alert_recipients: [...current, email] } : s);
    setNewRecipient('');
    setRecipientError(null);
  };

  const removeRecipient = (email: string) =>
    setSettings((s) => s ? { ...s, alert_recipients: (s.alert_recipients ?? []).filter((r) => r !== email) } : s);

  if (loading) {
    return <div className="flex items-center justify-center h-64 text-slate-500 text-sm">Cargando configuración…</div>;
  }

  if (!settings) {
    return <div className="flex items-center justify-center h-64 text-red-400 text-sm">Error al cargar configuración{error ? `: ${error}` : ''}</div>;
  }

  const recipients = settings.alert_recipients ?? [];
  const keyMissing = settings.llm_keys != null && !settings.llm_keys[settings.llm_provider];

  return (
    <div className="space-y-5 max-w-2xl">
      {/* LLM */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <div className="flex items-center gap-3 mb-5">
          <div className="w-10 h-10 rounded-lg bg-cyan-500/10 flex items-center justify-center">
            <Cpu className="w-5 h-5 text-cyan-400" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-white">Modelo de IA</h3>
            <p className="text-xs text-slate-500">Proveedor y modelo LLM para análisis y auto-documentación</p>
          </div>
        </div>
        <div className="space-y-3">
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Proveedor LLM</label>
            <select
              disabled={!isAdmin}
              value={settings.llm_provider}
              onChange={(e) => {
                const provider = e.target.value;
                setSettings((s) => s ? { ...s, llm_provider: provider, llm_model: DEFAULT_MODEL[provider] ?? s.llm_model } : s);
              }}
              className="w-full bg-slate-950/50 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-cyan-500/50 disabled:opacity-60"
            >
              {Object.entries(PROVIDER_LABEL).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
            {keyMissing && (
              <p className="text-xs text-amber-400/80 mt-1.5">
                Falta {KEY_ENV[settings.llm_provider]} en el servidor: no se va a poder guardar este proveedor.
              </p>
            )}
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">Modelo</label>
            <input
              disabled={!isAdmin}
              type="text"
              value={settings.llm_model}
              onChange={(e) => setSettings((s) => s ? { ...s, llm_model: e.target.value } : s)}
              className="w-full bg-slate-950/50 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-cyan-500/50 disabled:opacity-60"
              placeholder={DEFAULT_MODEL[settings.llm_provider] ?? 'gemini-flash-latest'}
            />
            <p className="text-[10px] text-slate-600 mt-1">
              Se usa para el análisis de cada alerta (n8n) y para «Generar documentación». Si el modelo no
              corresponde al proveedor, se usa el default del servidor.
            </p>
          </div>
        </div>
      </div>

      {/* Salud de la cadena LLM */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <div className="flex items-center gap-3 mb-5">
          <div className="w-10 h-10 rounded-lg bg-violet-500/10 flex items-center justify-center">
            <Activity className="w-5 h-5 text-violet-400" />
          </div>
          <div className="flex-1">
            <h3 className="text-sm font-semibold text-white">Estado de los modelos IA</h3>
            <p className="text-xs text-slate-500">
              Si el principal falla (cuota, saturación, timeout) se usa el siguiente de la cadena. El estado se
              actualiza con cada llamada real; no hay pings automáticos.
            </p>
          </div>
          {isAdmin && (
            <button
              onClick={() => { void runLlmCheck(); }}
              disabled={checkingLlm}
              className="flex items-center gap-1.5 bg-slate-800 hover:bg-slate-700 text-white text-xs px-3 py-1.5 rounded-lg transition-colors disabled:opacity-60 flex-shrink-0"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${checkingLlm ? 'animate-spin' : ''}`} />
              {checkingLlm ? 'Probando…' : 'Probar ahora'}
            </button>
          )}
        </div>
        {llmHealth.length === 0 ? (
          <p className="text-xs text-slate-500">Sin datos de la cadena LLM.</p>
        ) : (
          <div className="space-y-2">
            {llmHealth.map((h, i) => {
              const detail = healthDetail(h);
              return (
                <div
                  key={`${h.provider}:${h.model}`}
                  className="flex items-center justify-between gap-3 bg-slate-950/40 border border-slate-800/50 rounded-lg p-3"
                >
                  <div className="flex items-center gap-2.5 min-w-0">
                    <span className="text-[10px] text-slate-600 w-3">{i + 1}</span>
                    <span className={`w-2 h-2 rounded-full flex-shrink-0 ${HEALTH_DOT[h.status]}`} />
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-white truncate">
                        {PROVIDER_LABEL[h.provider] ?? h.provider}
                        <span className={`ml-2 text-[10px] px-1.5 py-0.5 rounded ${h.role === 'primary' ? 'bg-cyan-500/10 text-cyan-300' : 'bg-slate-800 text-slate-400'}`}>
                          {h.role === 'primary' ? 'Principal' : 'Respaldo'}
                        </span>
                      </p>
                      <p className="text-[11px] text-slate-500 truncate">{h.model}</p>
                    </div>
                  </div>
                  <span className={`text-xs text-right ${detail.tone}`}>{detail.text}</span>
                </div>
              );
            })}
          </div>
        )}
        {llmCheckError != null && <p className="text-xs text-red-400 mt-2">{llmCheckError}</p>}
        <p className="text-[10px] text-slate-600 mt-3">
          El orden de respaldo se define en el servidor con LLM_FALLBACK_CHAIN. «Probar ahora» hace una generación
          mínima contra cada proveedor (consume una request de la cuota de cada uno).
        </p>
      </div>

      {/* RAG */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <div className="flex items-center gap-3 mb-5">
          <div className="w-10 h-10 rounded-lg bg-violet-500/10 flex items-center justify-center">
            <Database className="w-5 h-5 text-violet-400" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-white">Base de Conocimiento (RAG)</h3>
            <p className="text-xs text-slate-500">Umbral de similitud y auto-documentación</p>
          </div>
        </div>
        <div className="space-y-3">
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">
              Umbral de similitud — {settings.similarity_threshold.toFixed(2)}
            </label>
            <input
              disabled={!isAdmin}
              type="range"
              min="0.3"
              max="0.95"
              step="0.01"
              value={settings.similarity_threshold}
              onChange={(e) => setSettings((s) => s ? { ...s, similarity_threshold: parseFloat(e.target.value) } : s)}
              className="w-full accent-cyan-400 disabled:opacity-60"
            />
            <div className="flex justify-between text-[10px] text-slate-600 mt-0.5">
              <span>0.30 (permisivo)</span><span>0.95 (estricto)</span>
            </div>
            <p className="text-[10px] text-slate-600 mt-1">
              Si la alerta se parece a un incidente de la base por encima de este valor, se trata como ya
              documentada; si no, es un incidente nuevo. Aplica a las alertas siguientes.
            </p>
          </div>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-white">Auto-documentación</p>
              <p className="text-xs text-slate-500">Generar documentación con IA para incidentes nuevos. Si está apagada, el incidente queda «nuevo» para documentarlo a mano.</p>
            </div>
            <button
              disabled={!isAdmin}
              onClick={() => setSettings((s) => s ? { ...s, auto_doc_enabled: !s.auto_doc_enabled } : s)}
              className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors disabled:opacity-60 ${settings.auto_doc_enabled ? 'bg-cyan-500' : 'bg-slate-700'}`}
            >
              <span className={`inline-block h-4 w-4 rounded-full bg-white shadow transition-transform ${settings.auto_doc_enabled ? 'translate-x-6' : 'translate-x-1'}`} />
            </button>
          </div>
        </div>
      </div>

      {/* Notificaciones por email */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <div className="flex items-center gap-3 mb-5">
          <div className="w-10 h-10 rounded-lg bg-blue-500/10 flex items-center justify-center">
            <Mail className="w-5 h-5 text-blue-400" />
          </div>
          <div className="flex-1">
            <h3 className="text-sm font-semibold text-white">Notificaciones por email</h3>
            <p className="text-xs text-slate-500">Destinatarios del email de alertas con análisis IA (n8n)</p>
          </div>
          <span className="text-xs text-slate-500">{recipients.length}/{MAX_RECIPIENTS}</span>
        </div>
        {isAdmin && (
          <div className="mb-3">
            <div className="flex gap-2">
              <input
                type="email"
                value={newRecipient}
                onChange={(e) => { setNewRecipient(e.target.value); setRecipientError(null); }}
                onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addRecipient(); } }}
                className="flex-1 bg-slate-950/50 border border-slate-800 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-cyan-500/50"
                placeholder="oncall@empresa.com"
              />
              <button
                onClick={addRecipient}
                className="flex items-center gap-1.5 bg-slate-800 hover:bg-slate-700 text-white text-sm px-3 py-2 rounded-lg transition-colors"
              >
                <Plus className="w-4 h-4" /> Agregar
              </button>
            </div>
            {recipientError != null && <p className="text-xs text-red-400 mt-1.5">{recipientError}</p>}
          </div>
        )}
        {recipients.length === 0 ? (
          <p className="text-xs text-amber-400/80">
            Sin destinatarios: se usa el email por defecto del servidor (ALERT_RECIPIENT_EMAIL).
          </p>
        ) : (
          <div className="flex flex-wrap gap-2">
            {recipients.map((email) => (
              <span key={email} className="flex items-center gap-1.5 bg-slate-950/40 border border-slate-800/50 rounded-full pl-3 pr-1.5 py-1 text-xs text-slate-200">
                {email}
                {isAdmin && (
                  <button onClick={() => removeRecipient(email)} className="text-slate-500 hover:text-red-400" aria-label={`Quitar ${email}`}>
                    <X className="w-3.5 h-3.5" />
                  </button>
                )}
              </span>
            ))}
          </div>
        )}
        {isAdmin && <p className="text-[10px] text-slate-600 mt-3">Los cambios se aplican al guardar la configuración.</p>}
      </div>

      {/* Integraciones — estado en vivo */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <div className="flex items-center gap-3 mb-5">
          <div className="w-10 h-10 rounded-lg bg-emerald-500/10 flex items-center justify-center">
            <Webhook className="w-5 h-5 text-emerald-400" />
          </div>
          <div className="flex-1">
            <h3 className="text-sm font-semibold text-white">Integraciones</h3>
            <p className="text-xs text-slate-500">Estado en vivo del stack de observabilidad (se actualiza cada 30 s)</p>
          </div>
          <button
            onClick={() => { void refreshIntegrations(); }}
            disabled={checkingIntegrations}
            className="text-slate-500 hover:text-white disabled:opacity-50"
            aria-label="Refrescar estado"
          >
            <RefreshCw className={`w-4 h-4 ${checkingIntegrations ? 'animate-spin' : ''}`} />
          </button>
        </div>
        {integrations == null ? (
          <p className="text-xs text-slate-500">Verificando…</p>
        ) : integrations.length === 0 ? (
          <p className="text-xs text-red-400">No se pudo obtener el estado de las integraciones.</p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {integrations.map((it) => (
              <div
                key={it.key}
                title={it.detail || undefined}
                className="flex items-center justify-between bg-slate-950/40 border border-slate-800/50 rounded-lg p-3"
              >
                <div className="flex items-center gap-2">
                  <span className={`w-2 h-2 rounded-full ${it.status === 'up' ? 'bg-emerald-400' : 'bg-red-500'}`} />
                  <span className="text-sm font-medium text-white">{it.name}</span>
                </div>
                <span className={`text-xs ${it.status === 'up' ? 'text-slate-500' : 'text-red-400'}`}>
                  {it.status === 'up' ? `${it.latency_ms} ms` : it.http_code != null ? `HTTP ${it.http_code}` : 'sin respuesta'}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Polling */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <div className="flex items-center gap-3 mb-5">
          <div className="w-10 h-10 rounded-lg bg-amber-500/10 flex items-center justify-center">
            <Sliders className="w-5 h-5 text-amber-400" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-white">Refresco automático</h3>
            <p className="text-xs text-slate-500">Cada cuánto el dashboard recarga incidentes, métricas y notificaciones</p>
          </div>
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-400 mb-1.5">
            Intervalo — {settings.polling_interval_seconds}s
          </label>
          <input
            disabled={!isAdmin}
            type="range"
            min="10"
            max="120"
            step="5"
            value={settings.polling_interval_seconds}
            onChange={(e) => setSettings((s) => s ? { ...s, polling_interval_seconds: parseInt(e.target.value) } : s)}
            className="w-full accent-amber-400 disabled:opacity-60"
          />
          <div className="flex justify-between text-[10px] text-slate-600 mt-0.5">
            <span>10s</span><span>120s</span>
          </div>
        </div>
      </div>

      {/* Seguridad */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-xl p-5">
        <div className="flex items-center gap-3 mb-4">
          <div className="w-10 h-10 rounded-lg bg-red-500/10 flex items-center justify-center">
            <Shield className="w-5 h-5 text-red-400" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-white">Seguridad</h3>
            <p className="text-xs text-slate-500">Autenticación JWT · Tokens expiran en 12 h</p>
          </div>
        </div>
        <div className="flex items-center gap-2 text-xs text-emerald-400">
          <Bell className="w-3.5 h-3.5" />
          Emails de alerta enviados por n8n al pool de destinatarios configurado
        </div>
      </div>

      {isAdmin && (
        <div className="flex items-center gap-3">
          {error != null && (
            <div className="flex items-center gap-2 text-xs text-red-400">
              <AlertCircle className="w-3.5 h-3.5" /> {error}
            </div>
          )}
          {warning != null && (
            <div className="flex items-center gap-2 text-xs text-amber-400">
              <AlertCircle className="w-3.5 h-3.5" /> {warning}
            </div>
          )}
          {saved && (
            <div className="flex items-center gap-2 text-xs text-emerald-400">
              <CheckCircle2 className="w-3.5 h-3.5" /> Guardado correctamente
            </div>
          )}
          <button
            onClick={() => { void handleSave(); }}
            disabled={saving}
            className="ml-auto flex items-center gap-2 bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white text-sm font-medium px-5 py-2.5 rounded-lg transition-all shadow-lg shadow-cyan-500/20 disabled:opacity-60"
          >
            {saving
              ? <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              : <><Save className="w-4 h-4" /> Guardar configuración</>}
          </button>
        </div>
      )}
    </div>
  );
}
