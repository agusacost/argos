import type { IncidentDetail, IncidentSummary, Metrics, Paginated } from "./types";

const BASE = import.meta.env.VITE_API_BASE ?? "/api";

// ─── Token ───────────────────────────────────────────────────────────────────

const TOKEN_KEY = "argos_token";

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setStoredToken(token: string | null): void {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

function authHeader(): Record<string, string> {
  const t = getStoredToken();
  return t ? { Authorization: `Bearer ${t}` } : {};
}

// ─── HTTP helpers ─────────────────────────────────────────────────────────────

function qs(params: Record<string, string | number | undefined>): string {
  const sp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== "") sp.set(k, String(v));
  }
  const s = sp.toString();
  return s ? `?${s}` : "";
}

async function getJson<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE}${path}`, { headers: authHeader() });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  return res.json() as Promise<T>;
}

async function postJson<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeader() },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const detail = await res.json().catch(() => ({})) as { detail?: string };
    throw new Error(detail.detail ?? `${res.status} ${res.statusText}`);
  }
  return res.json() as Promise<T>;
}

async function patchJson<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json", ...authHeader() },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const detail = await res.json().catch(() => ({})) as { detail?: string };
    throw new Error(detail.detail ?? `${res.status} ${res.statusText}`);
  }
  return res.json() as Promise<T>;
}

async function delJson(path: string): Promise<void> {
  const res = await fetch(`${BASE}${path}`, {
    method: "DELETE",
    headers: authHeader(),
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface AuthUser { id: string; email: string; name: string; role: string; }
export interface LoginResponse { access_token: string; token_type: string; user: AuthUser; }

export const loginApi = (email: string, password: string) =>
  postJson<LoginResponse>("/auth/login", { email, password });

export const getMe = () => getJson<AuthUser>("/auth/me");

// ─── Incidents ────────────────────────────────────────────────────────────────

export interface ListParams {
  page?: number;
  size?: number;
  estado?: string;
  tipo?: string;
  search?: string;
}

export interface PatchIncidentBody {
  estado?: string;
  resolved_at?: string;
  documento?: string;
  pasos?: string[];
}

export const getMetrics = () => getJson<Metrics>("/incidents/metrics");

export const listIncidents = (p: ListParams = {}) =>
  getJson<Paginated<IncidentSummary>>(`/incidents${qs({ ...p })}`);

export const listKnowledgeBase = (p: { page?: number; size?: number; search?: string } = {}) =>
  getJson<Paginated<IncidentSummary>>(`/incidents/knowledge-base${qs({ ...p })}`);

export const getIncident = (id: string) => getJson<IncidentDetail>(`/incidents/${id}`);

export const patchIncident = (id: string, body: PatchIncidentBody) =>
  patchJson<IncidentDetail>(`/incidents/${id}`, body);

export const generateDocument = (id: string) =>
  postJson<IncidentDetail>(`/incidents/${id}/document`);

// ─── Observabilidad ───────────────────────────────────────────────────────────

export interface IncidentObservability { logs: string[]; sparkline: number[]; }
export interface ServiceHealth { name: string; component: string; status: string; sparkline: number[]; }

export const getIncidentObservability = (id: string) =>
  getJson<IncidentObservability>(`/incidents/${id}/observability`);

export const getServicesHealth = () =>
  getJson<ServiceHealth[]>("/observability/services");

export interface NginxMetrics {
  range: string;
  step_seconds: number;
  timestamps: number[];
  status_series: { "2xx": number[]; "3xx": number[]; "4xx": number[]; "5xx": number[] };
  latency: { p50: (number | null)[]; p95: (number | null)[]; p99: (number | null)[] };
  error_rate: number[];
  request_rate: number[];
}

export const getNginxMetrics = (range = "1h") =>
  getJson<NginxMetrics>(`/observability/nginx-metrics${qs({ range })}`);

// ─── Auditoría ────────────────────────────────────────────────────────────────

export interface AuditEntry { id: string; incident_id: string; user: string; action: string; timestamp: string; }

export const getIncidentAudit = (id: string) =>
  getJson<AuditEntry[]>(`/incidents/${id}/audit`);

export const getRecentActivity = () =>
  getJson<AuditEntry[]>("/activity");

// ─── Notificaciones ───────────────────────────────────────────────────────────

export interface Notification {
  id: string;
  incident_id: string;
  title: string;
  message: string;
  severity: string;
  read: boolean;
  created_at: string;
}

export const getNotifications = () => getJson<Notification[]>("/notifications");

export const markNotificationRead = (id: string) =>
  postJson<Notification>(`/notifications/${id}/read`);

// ─── Usuarios ─────────────────────────────────────────────────────────────────

export interface UserRecord { id: string; email: string; name: string; role: string; created_at?: string; }

export const listUsers = () => getJson<UserRecord[]>("/users");

export const createUser = (body: { email: string; name: string; role: string; password: string }) =>
  postJson<UserRecord>("/users", body);

export const updateUser = (id: string, body: { name?: string; role?: string; password?: string }) =>
  patchJson<UserRecord>(`/users/${id}`, body);

export const deleteUser = (id: string) => delJson(`/users/${id}`);

// ─── Configuración ────────────────────────────────────────────────────────────

export interface Settings {
  llm_provider: string;
  llm_model: string;
  similarity_threshold: number;
  auto_doc_enabled: boolean;
  polling_interval_seconds: number;
  /** Pool de destinatarios del email de alertas (n8n). Vacío → fallback ALERT_RECIPIENT_EMAIL. */
  alert_recipients: string[];
  /** Solo lectura (no se persiste): qué API keys de LLM están cargadas en el servidor. */
  llm_keys?: Record<string, boolean>;
  /** Solo en la respuesta del PATCH: aviso no bloqueante (p. ej. modelo no encontrado). */
  warning?: string;
}

export const getSettings = () => getJson<Settings>("/settings");

export const updateSettings = (body: Partial<Settings>) => {
  const { llm_keys: _keys, warning: _warning, ...rest } = body;
  return patchJson<Settings>("/settings", rest);
};

export interface IntegrationStatus {
  key: string;
  name: string;
  status: "up" | "down";
  http_code: number | null;
  latency_ms: number;
  detail: string;
}

export const getIntegrationsStatus = () => getJson<IntegrationStatus[]>("/integrations/status");

// ─── Pruebas de carga (k6) ──────────────────────────────────────────────────

export interface LoadScenario {
  id: string;
  name: string;
  descripcion: string;
  perfil: string;
  duracion_aprox: string;
  archivo: string;
  code: string;
}

export interface LoadRunSummary {
  reqs: number | null;
  throughput_rps: number | null;
  p95_ms: number | null;
  avg_ms: number | null;
  error_rate: number | null;
  checks_rate: number | null;
}

export type LoadRunStatus =
  | "running" | "passed" | "thresholds_failed" | "failed" | "cancelled";

export interface LoadRun {
  id: string;
  scenario: string;
  scenario_name: string;
  status: LoadRunStatus;
  base_url: string;
  started_at: string;
  finished_at: string | null;
  exit_code: number | null;
  summary: LoadRunSummary | null;
  log_tail: string | null;
}

export const listScenarios = () => getJson<LoadScenario[]>("/loadtests/scenarios");

export const listRuns = () => getJson<LoadRun[]>("/loadtests/runs");

export const getRun = (id: string) => getJson<LoadRun>(`/loadtests/runs/${id}`);

export const startLoadTest = (scenario: string, base_url?: string) =>
  postJson<LoadRun>("/loadtests/runs", { scenario, base_url });

export const stopRun = (id: string) =>
  postJson<LoadRun>(`/loadtests/runs/${id}/stop`);
