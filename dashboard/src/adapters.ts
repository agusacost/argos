import type { IncidentSummary, IncidentDetail, Severity, UIStatus, IncidentVM, ResolutionStep } from './types';

const ESTADO_TO_STATUS: Record<string, UIStatus> = {
  nuevo: 'ai_analyzing',
  pendiente: 'investigating',
  resuelto: 'resolved',
  auto_documentado: 'documented',
  documentado_previamente: 'documented',
};

function mapStatus(estado?: string): UIStatus {
  return ESTADO_TO_STATUS[estado ?? ''] ?? 'investigating';
}

function mapSeverity(sev?: string): Severity {
  if (sev === 'critical' || sev === 'warning' || sev === 'info') return sev;
  return 'info';
}

export function deriveComponent(tipo_alerta: string): string {
  const t = tipo_alerta.toLowerCase();
  if (t.includes('nginx')) return 'Nginx';
  if (t.includes('postgresql') || t.includes('postgres') || t.includes('sql')) return 'PostgreSQL';
  if (t.includes('redis')) return 'Redis';
  if (t.includes('api') || t.includes('gateway')) return 'API Gateway';
  if (t.includes('kubernetes') || t.includes('k8s') || t.includes('pod')) return 'Kubernetes';
  if (t.includes('loki')) return 'Loki';
  if (t.includes('host') || t.includes('cpu') || t.includes('memory') || t.includes('disk')) return 'Host';
  if (t.includes('alertmanager')) return 'Alertmanager';
  const first = tipo_alerta.split(/(?=[A-Z])|[_\s-]/)[0];
  return first || tipo_alerta;
}

export function toIncidentVM(incident: IncidentSummary | IncidentDetail): IncidentVM {
  const detail = incident as IncidentDetail;
  const pasos = detail.pasos ?? [];
  const isDone = incident.estado === 'resuelto' || incident.estado === 'auto_documentado';

  const aiSteps: ResolutionStep[] = pasos.map((text, i) => ({
    id: `s${i + 1}`,
    text,
    done: isDone,
  }));

  return {
    id: incident.id,
    tipo_alerta: incident.tipo_alerta,
    resumen: incident.resumen,
    estado: incident.estado ?? '',
    fuente: incident.fuente,
    severity_raw: incident.severity,
    detected_at: incident.detected_at,
    resolved_at: incident.resolved_at,
    mttr_minutes: incident.mttr_minutes,
    documento: detail.documento,
    pasos,
    severity: mapSeverity(incident.severity),
    status: mapStatus(incident.estado),
    component: deriveComponent(incident.tipo_alerta),
    title: incident.tipo_alerta,
    description: incident.resumen,
    timestamp: incident.detected_at ?? incident.fecha ?? new Date().toISOString(),
    assignee: null,
    aiSummary: incident.resumen,
    aiSteps,
  };
}
