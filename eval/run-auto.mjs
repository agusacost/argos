// Corridas automáticas del OE3 (condición "con Argos", operador que sigue la propuesta). Por cada falla:
//   1. inyecta (eval/faults.ps1), 2. espera la alerta en Alertmanager, 3. espera la ejecución de n8n y lee el análisis
//   que salió por email, 4. evalúa si la propuesta contiene el arreglo correcto, 5. aplica el arreglo (como un operador
//   que sigue la propuesta), 6. espera la resolución en Alertmanager y 7. deja un enfriamiento antes de la siguiente.
// NO mide un diagnóstico humano: el runner sabe qué falla inyectó. Ver docs/EVALUACION_OE3.md §5.
//
// Uso: node eval/run-auto.mjs [--scenarios F1,F2,F3,F4,F7] [--rounds 2] [--cooldown 300]
import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync, appendFileSync, existsSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const results = join(here, 'results');
mkdirSync(results, { recursive: true });
const AM = 'http://localhost:9093';
const API = process.env.ARGOS_API_URL || 'http://127.0.0.1:8000';

const args = Object.fromEntries(process.argv.slice(2).reduce((a, v, i, all) => (v.startsWith('--') ? [...a, [v.slice(2), all[i + 1]]] : a), []));
const ids = (args.scenarios || 'F1,F2,F3,F4,F7').split(',');
const rounds = Number(args.rounds || 2);
const cooldownMs = Number(args.cooldown || 300) * 1000;

// Contenedor que hay que levantar en cada falla (es lo que tiene que proponer Argos).
const FIX = { F1: 'membresia-api', F2: 'membresia-sqlserver', F3: 'node-exporter', F4: 'cadvisor', F7: 'nginx-prometheus-exporter' };
const catalog = Object.fromEntries(JSON.parse(readFileSync(join(here, 'scenarios.json'), 'utf8')).scenarios.map(s => [s.id, s]));

const stamp = new Date().toISOString().replace(/[:T]/g, '-').slice(0, 19);
const csvPath = join(results, `auto-${stamp}.csv`);
const mdPath = join(results, `auto-${stamp}.md`);
writeFileSync(csvPath, '﻿run;scenario;alertname;injected_at;starts_at;fired_seen_at;proposal_at;fix_at;resolved_seen_at;' +
  'detect_min;proposal_min;mttr_min;proposal_correct;mentions_container;llm;execution;estado_argos;nota\r\n', 'utf8');
writeFileSync(mdPath, `# Corridas automáticas OE3 — ${stamp}\n\nEscenarios ${ids.join(',')} × ${rounds} rondas.\n\n`, 'utf8');

const sleep = ms => new Promise(r => setTimeout(r, ms));
const log = msg => console.log(`[${new Date().toISOString().slice(11, 19)}] ${msg}`);
const minutes = (a, b) => (a && b ? Math.round(((b - a) / 60000) * 100) / 100 : '');
const iso = d => (d ? d.toISOString() : '');

async function activeAlerts() {
  try { return await (await fetch(`${AM}/api/v2/alerts?active=true&silenced=false&inhibited=true`)).json(); } catch { return []; }
}

async function waitFor(fn, timeoutMs, everyMs = 5000) {
  const end = Date.now() + timeoutMs;
  while (Date.now() < end) {
    const v = await fn();
    if (v) return v;
    await sleep(everyMs);
  }
  return null;
}

function faults(id, action) {
  execFileSync('pwsh', ['-NoProfile', '-File', join(here, 'faults.ps1'), '-Scenario', id, '-Action', action], { stdio: 'ignore' });
}

function n8nExecutionsSince(date) {
  const out = execFileSync('docker', ['exec', '-i', '-w', '/usr/local/lib/node_modules/n8n', 'argos-n8n', 'node', '-', date.toISOString()],
    { input: readFileSync(join(here, 'n8n-analysis.cjs')), env: { ...process.env, MSYS_NO_PATHCONV: '1' } });
  return JSON.parse(out.toString());
}

let token = null;
async function incidentFor(alertname, startsAt) {
  try {
    token ??= (await (await fetch(`${API}/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: 'admin@argos.local', password: 'argos-admin-2026' }) })).json()).access_token;
    const { data } = await (await fetch(`${API}/incidents?tipo=${alertname}&size=100`, { headers: { Authorization: `Bearer ${token}` } })).json();
    return data.find(i => i.detected_at && Math.abs(new Date(i.detected_at) - startsAt) < 60_000) ?? null;
  } catch { return null; }
}

// ¿La propuesta dice cómo levantar el contenedor correcto? (docker start/restart <contenedor>, o compose up de ese servicio)
function evaluate(text, container) {
  const t = String(text || '');
  const esc = container.replace(/[-]/g, '\\-');
  const fix = new RegExp(`docker\\s+(compose[^\\n]*\\s)?(re)?start\\b[^\\n]*\\b${esc}\\b`, 'i').test(t);
  return { correct: fix, mentions: new RegExp(`\\b${esc}\\b`, 'i').test(t) };
}

let run = 0;
for (let round = 1; round <= rounds; round++) {
  for (const id of ids) {
    run++;
    const runId = `A${String(run).padStart(2, '0')}`;
    const sc = catalog[id];
    const container = FIX[id];
    let note = '';
    log(`${runId} ${id} (${sc.alertname}): inyectando`);
    const injectedAt = new Date();
    faults(id, 'inject');

    // 2. Alerta activa en Alertmanager
    const alert = await waitFor(async () => (await activeAlerts()).find(a => a.labels.alertname === sc.alertname
      && new Date(a.startsAt) >= new Date(injectedAt - 30_000)), 12 * 60_000);
    const firedSeen = alert ? new Date() : null;
    const startsAt = alert ? new Date(alert.startsAt) : null;
    log(`${runId} ${alert ? `FIRING startsAt ${iso(startsAt)}` : 'sin alerta en 12 min'}`);

    // 3. Ejecución de n8n con el análisis (lo que llega por email)
    let exec = null;
    if (alert) {
      exec = await waitFor(() => n8nExecutionsSince(new Date(injectedAt - 5_000))
        .find(e => e.status_alert === 'firing' && e.analysis && e.alertname !== 'NginxSuspiciousIPActivity'), 6 * 60_000, 10_000);
      if (!exec) note += 'sin análisis de n8n en 6 min; ';
    }
    const proposalAt = exec ? new Date(exec.stoppedAt.replace(' ', 'T') + 'Z') : null;
    const ev = evaluate(exec?.analysis, container);
    log(`${runId} propuesta ${exec ? `#${exec.id} ${exec.llm} correcta=${ev.correct}` : '—'}`);

    // 5. El operador aplica el arreglo (si la propuesta no lo tenía, igual se aplica y queda anotado)
    if (exec && !ev.correct) note += 'la propuesta no incluye el arreglo exacto; ';
    const fixAt = new Date();
    execFileSync('docker', ['start', container], { stdio: 'ignore' });

    // 6. Resolución
    const resolved = alert ? await waitFor(async () => !(await activeAlerts()).some(a => a.fingerprint === alert.fingerprint), 12 * 60_000) : null;
    const resolvedSeen = resolved ? new Date() : null;
    if (alert && !resolved) note += 'no se resolvió en 12 min; ';
    faults(id, 'restore');
    log(`${runId} ${resolvedSeen ? `RESOLVED (MTTR ${minutes(startsAt, resolvedSeen)} min)` : 'sin resolución'}`);

    const incident = startsAt ? await incidentFor(sc.alertname, startsAt) : null;
    appendFileSync(csvPath, [runId, id, sc.alertname, iso(injectedAt), iso(startsAt), iso(firedSeen), iso(proposalAt), iso(fixAt),
      iso(resolvedSeen), minutes(injectedAt, firedSeen), minutes(startsAt, proposalAt), minutes(startsAt, resolvedSeen),
      exec ? ev.correct : '', exec ? ev.mentions : '', exec?.llm ?? '', exec?.id ?? '', incident?.estado ?? '',
      `"${note.trim()}"`].join(';') + '\r\n', 'utf8');
    appendFileSync(mdPath, `## ${runId} · ${id} · ${sc.alertname}\n\n- Causa: ${sc.root_cause}\n- Arreglo esperado: \`docker start ${container}\`\n` +
      `- Propuesta: ejecución n8n #${exec?.id ?? '—'} · ${exec?.llm ?? '—'} · arreglo exacto: **${exec ? ev.correct : '—'}** · ` +
      `menciona el contenedor: ${exec ? ev.mentions : '—'}\n- Estado en Argos: ${incident?.estado ?? '—'}\n\n` +
      `${exec?.analysis ?? '_sin análisis_'}\n\n`, 'utf8');

    // 7. Enfriamiento: que Alertmanager cierre el grupo y no queden alertas críticas activas
    if (!(round === rounds && id === ids.at(-1))) {
      log(`${runId} enfriamiento ${cooldownMs / 1000}s`);
      await sleep(cooldownMs);
      await waitFor(async () => !(await activeAlerts()).some(a => a.labels.severity === 'critical'), 5 * 60_000);
    }
  }
}
log(`listo → ${csvPath}`);
