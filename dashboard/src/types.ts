import { createContext, useContext } from 'react';
import type { Notification } from './api';

// ─── Backend types (preserved) ────────────────────────────────────────────────
export interface IncidentSummary {
  id: string;
  tipo_alerta: string;
  resumen: string;
  fecha?: string;
  estado?: string;
  fuente?: string;
  severity?: string;
  detected_at?: string;
  resolved_at?: string;
  mttr_minutes?: number;
}

export interface IncidentDetail extends IncidentSummary {
  pasos?: string[];
  documento?: string;
}

export interface Paginated<T> {
  data: T[];
  total: number;
  page: number;
  size: number;
}

export interface Metrics {
  total_incidents: number;
  by_status: Record<string, number>;
  by_alert_type: Record<string, number>;
  mttr_average_minutes: number | null;
  incidents_per_day: { date: string; count: number }[];
  recent_trend: 'improving' | 'stable' | 'degrading';
}

// ─── UI View Model ─────────────────────────────────────────────────────────────
export type Severity = 'critical' | 'warning' | 'info';
export type UIStatus = 'ai_analyzing' | 'investigating' | 'resolved' | 'documented';

export interface User {
  id: string;
  name: string;
  email: string;
  role: 'Admin' | 'Operador';
  avatar: string;
}

export interface AuditEntry {
  id: string;
  user: string;
  action: string;
  timestamp: string;
}

export interface ResolutionStep {
  id: string;
  text: string;
  done: boolean;
}

export interface MetricPoint {
  label: string;
  value: string;
  sparkline: number[];
}

export interface IncidentVM {
  id: string;
  tipo_alerta: string;
  resumen: string;
  estado: string;
  fuente?: string;
  severity_raw?: string;
  detected_at?: string;
  resolved_at?: string;
  mttr_minutes?: number;
  documento?: string;
  pasos?: string[];
  // Derived for UI
  severity: Severity;
  status: UIStatus;
  component: string;
  title: string;
  description: string;
  timestamp: string;
  assignee: string | null;
  aiSummary: string;
  aiSteps: ResolutionStep[];
}

// ─── App Context ──────────────────────────────────────────────────────────────
export interface AppContextType {
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  activeView: string;
  setActiveView: (v: string) => void;
  selectedIncidentId: string | null;
  setSelectedIncidentId: (id: string | null) => void;
  incidents: IncidentVM[];
  metrics: Metrics | null;
  loadingIncidents: boolean;
  notifications: Notification[];
  resolveIncident: (id: string) => Promise<void>;
  documentIncident: (id: string) => Promise<void>;
  refreshIncidents: () => void;
  /** Intervalo de polling (Configuración → Refresco automático); se aplica en vivo al guardar. */
  pollingSeconds: number;
  setPollingSeconds: (s: number) => void;
}

export const AppContext = createContext<AppContextType | null>(null);

export function useApp() {
  const ctx = useContext(AppContext);
  if (!ctx) throw new Error('useApp must be used within AppContext');
  return ctx;
}
