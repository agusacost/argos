import type { LlmHealth } from './api';

export type AiTone = 'ok' | 'fallback' | 'down';

export interface AiStatus {
  tone: AiTone;
  label: string;
  detail: string;
}

/** Resumen de la cadena LLM para el badge del header ("unknown" = sin llamadas aún, se asume sano). */
export function summarizeAi(health: LlmHealth[]): AiStatus {
  const usable = health.filter((h) => h.configured && h.status !== 'down');
  const primary = health.find((h) => h.role === 'primary');
  if (primary && primary.configured && primary.status !== 'down') {
    return { tone: 'ok', label: 'IA Activa', detail: `${primary.provider} · ${primary.model}` };
  }
  if (usable.length > 0) {
    const backup = usable[0];
    const reason = primary?.last_error ? `${primary.provider}: ${primary.last_error}` : `${primary?.provider ?? 'principal'} no disponible`;
    const name = backup.provider === primary?.provider ? backup.model : backup.provider;
    return { tone: 'fallback', label: `IA en respaldo (${name})`, detail: reason };
  }
  if (health.length === 0) return { tone: 'ok', label: 'IA Activa', detail: 'Sin datos de salud todavía' };
  return { tone: 'down', label: 'IA no disponible', detail: 'Ningún proveedor LLM responde' };
}

export const PROVIDER_LABEL: Record<string, string> = {
  gemini: 'Google Gemini',
  anthropic: 'Anthropic Claude',
  groq: 'Groq',
  openrouter: 'OpenRouter',
};
