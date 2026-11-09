// Resumen del experimento de MTTR del OE3. Cruza:
//   eval/results/runs.csv    hitos manuales (eval/mark.ps1): start / diag / fix por corrida
//   eval/results/blind.csv   falla real de cada corrida en modo ciego (eval/faults.ps1 sin -Scenario)
//   eval/results/alerts.csv  inicio/fin objetivos de cada alerta (eval/timer.ps1, desde Alertmanager)
//   GET /incidents           tiempo hasta la propuesta de Argos = fecha (alta en Qdrant) − detected_at
// y escribe eval/results/mttr-summary.md con la tabla por corrida y las medianas por condición.
//
// Uso: node eval/summarize-mttr.mjs
// Variables: ARGOS_API_URL (default http://127.0.0.1:8000), ARGOS_USER / ARGOS_PASSWORD (default admin del seed).
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const results = join(here, 'results');
const API = process.env.ARGOS_API_URL || 'http://127.0.0.1:8000';
const scenarios = Object.fromEntries(JSON.parse(readFileSync(join(here, 'scenarios.json'), 'utf8'))
  .scenarios.map(s => [s.id, s]));

const blindRows = readCsv(join(results, 'blind.csv'));
const blind = Object.fromEntries(blindRows.map(b => [b.run, b.scenario]));
// En modo ciego el inicio real es el de la inyección (faults.ps1), no la marca manual 'start' (puede llegar tarde).
const injectedAt = Object.fromEntries(blindRows.map(b => [b.run, new Date(b.injected_at)]));
const marks = readCsv(join(results, 'runs.csv')).map(m => ({ ...m, scenario: m.scenario || blind[m.run] || '' }));
const alerts = readCsv(join(results, 'alerts.csv'));
const incidents = await fetchIncidents().catch(e => { console.warn(`[warn] sin incidentes de Argos: ${e.message}`); return []; });

const runs = Object.values(marks.reduce((acc, m) => {
  (acc[m.run] ??= { run: m.run, scenario: m.scenario, condition: m.condition })[m.event] = { at: new Date(m.at), note: m.note };
  return acc;
}, {}));

for (const r of runs) if (injectedAt[r.run]) r.start = { at: injectedAt[r.run], note: 'inyección' };

const rows = runs.filter(r => r.start).map(r => {
  const alertname = scenarios[r.scenario]?.alertname;
  // La alerta de esta corrida: la primera de ese tipo que arrancó después del inicio (con 1 min de tolerancia).
  const alert = alerts
    .filter(a => a.alertname === alertname && new Date(a.starts_at) >= new Date(r.start.at - 60_000))
    .sort((a, b) => new Date(a.starts_at) - new Date(b.starts_at))[0];
  const startsAt = alert ? new Date(alert.starts_at) : null;
  const firedAt = alert ? new Date(alert.first_seen_at) : null;
  const resolvedAt = alert ? new Date(alert.resolved_seen_at) : null;
  const incident = startsAt && incidents.find(i => i.tipo_alerta === alertname && i.detected_at
    && Math.abs(new Date(i.detected_at) - startsAt) < 60_000);
  const proposalAt = incident?.fecha ? new Date(incident.fecha.endsWith('Z') ? incident.fecha : incident.fecha + 'Z') : null;
  return {
    ...r, alertname,
    detect: minutes(r.start.at, firedAt),                  // falla → alerta activa en Alertmanager
    proposal: r.condition === 'argos' ? minutes(startsAt, proposalAt) : null, // startsAt → incidente con propuesta
    diagnose: minutes(firedAt, r.diag?.at),                // alerta visible → causa identificada
    repair: minutes(r.diag?.at, r.fix?.at),                // causa → arreglo aplicado
    mttr: minutes(startsAt, resolvedAt),                   // startsAt → resolución (Alertmanager)
    estado: incident?.estado ?? '-',
  };
});

const fmt = v => v == null ? '–' : v.toFixed(1);
const lines = ['# OE3 — Experimento de MTTR', '', `Generado: ${new Date().toISOString()}`, '',
  '## Corridas (minutos)', '',
  '| Corrida | Falla | Condición | Detección | Propuesta Argos | Diagnóstico | Reparación | MTTR | Estado Argos | Causa anotada |',
  '|---|---|---|---|---|---|---|---|---|---|',
  ...rows.map(r => `| ${r.run} | ${r.scenario} ${r.alertname} | ${r.condition} | ${fmt(r.detect)} | ${fmt(r.proposal)} | ${fmt(r.diagnose)} | ${fmt(r.repair)} | ${fmt(r.mttr)} | ${r.estado} | ${r.diag?.note ?? ''} |`),
  '', '## Medianas por condición', '',
  '| Condición | N | Diagnóstico | Reparación | MTTR |', '|---|---|---|---|---|'];
const byCondition = {};
for (const c of ['argos', 'manual']) {
  const rs = rows.filter(r => r.condition === c);
  byCondition[c] = { diagnose: median(rs.map(r => r.diagnose)), repair: median(rs.map(r => r.repair)), mttr: median(rs.map(r => r.mttr)) };
  lines.push(`| ${c} | ${rs.length} | ${fmt(byCondition[c].diagnose)} | ${fmt(byCondition[c].repair)} | ${fmt(byCondition[c].mttr)} |`);
}
const reduction = (m, a) => m && a != null ? `${(((m - a) / m) * 100).toFixed(0)} %` : '–';
lines.push('', `**Reducción del MTTR (mediana):** ${reduction(byCondition.manual.mttr, byCondition.argos.mttr)} · ` +
  `**del tiempo de diagnóstico:** ${reduction(byCondition.manual.diagnose, byCondition.argos.diagnose)}`,
  `**Tiempo hasta la propuesta de Argos (mediana):** ${fmt(median(rows.map(r => r.proposal)))} min`, '');

writeFileSync(join(results, 'mttr-summary.md'), lines.join('\n'), 'utf8');
console.log(lines.join('\n'));

// ---------- helpers ----------

function minutes(from, to) { return from && to ? Math.round(((to - from) / 60_000) * 100) / 100 : null; }

function median(values) {
  const v = values.filter(x => x != null).sort((a, b) => a - b);
  if (v.length === 0) return null;
  return v.length % 2 ? v[(v.length - 1) / 2] : (v[v.length / 2 - 1] + v[v.length / 2]) / 2;
}

function readCsv(path) {
  if (!existsSync(path)) return [];
  const [head, ...body] = readFileSync(path, 'utf8').replace(/^﻿/, '').split(/\r?\n/).filter(Boolean);
  const cols = head.split(';');
  return body.map(line => {
    const cells = line.split(';');
    // la nota (última columna) puede tener ';'
    const fixed = [...cells.slice(0, cols.length - 1), cells.slice(cols.length - 1).join(';')];
    return Object.fromEntries(cols.map((c, i) => [c, (fixed[i] ?? '').replace(/^"|"$/g, '')]));
  });
}

async function fetchIncidents() {
  const login = await (await fetch(`${API}/auth/login`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: process.env.ARGOS_USER || 'admin@argos.local', password: process.env.ARGOS_PASSWORD || 'argos-admin-2026' }),
  })).json();
  if (!login.access_token) throw new Error('login fallido');
  const all = [];
  for (let page = 1; ; page++) {
    const res = await fetch(`${API}/incidents?page=${page}&size=100`, { headers: { Authorization: `Bearer ${login.access_token}` } });
    if (!res.ok) throw new Error(`GET /incidents → HTTP ${res.status}`);
    const { data } = await res.json();
    all.push(...data);
    if (data.length < 100) return all;
  }
}
