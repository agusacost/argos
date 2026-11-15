// Evaluación del OE3 (calidad del LLM y del RAG). Para cada escenario de scenarios.json:
//   1. POST /query (RAG, con el nombre de la alerta como en n8n) → scores top-3 + match_reason (similarity |
//      alert_name | none) para la matriz conocida/nueva y el barrido de umbral.
//   2. POST /internal/evidence (como el nodo 'Get Evidence'): evidencia de logs de Loki de la ventana. Solo tiene
//      sentido con la falla inyectada (p. ej. F2: faults.ps1 -Scenario F2 -Action inject + tráfico); si no, sale vacía.
//   3. Arma los prompts ejecutando el código REAL de los nodos 'Prepare', 'Build Prompt' y 'Build Doc Prompt'
//      del workflow de n8n (se leen del JSON): lo que se evalúa es exactamente lo que corre en producción.
//   4. POST /internal/llm/generate forzando cada proveedor (sin fallback) → análisis (y doc si el RAG dice 'nuevo').
//   5. Escribe eval/results/llm-<fecha>.md (salidas para leer) y .csv (rúbrica en blanco para puntuar).
//
// Uso (desde la raíz del repo, con el stack levantado):
//   node eval/run-llm-eval.mjs                       todos los escenarios, todos los proveedores con key
//   node eval/run-llm-eval.mjs --scenario F1,F3      solo esos escenarios
//   node eval/run-llm-eval.mjs --providers groq      solo esos proveedores (provider o provider:modelo)
//   node eval/run-llm-eval.mjs --rag-only            solo RAG (sin LLM): matriz + barrido de umbral
//   node eval/run-llm-eval.mjs --no-evidence         sin evidencia de logs (comparación con el prompt anterior)
//   node eval/run-llm-eval.mjs --evidence-at <ISO>   ventana de evidencia anclada en ese startsAt (default: ahora)
// Variables: ARGOS_API_URL (default http://127.0.0.1:8000), ARGOS_INTERNAL_TOKEN (si no, se lee de docker/monitoring/.env).
import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, '..');
const API = process.env.ARGOS_API_URL || 'http://127.0.0.1:8000';
const DEFAULT_PROVIDERS = ['gemini:gemini-flash-latest', 'gemini:gemini-flash-lite-latest', 'groq', 'openrouter'];
const THRESHOLDS = [0.55, 0.58, 0.6, 0.63, 0.65, 0.68, 0.7, 0.75];

const args = parseArgs(process.argv.slice(2));
const token = process.env.ARGOS_INTERNAL_TOKEN ?? readEnvFile(join(root, 'docker/monitoring/.env')).ARGOS_INTERNAL_TOKEN ?? '';
const scenarios = JSON.parse(readFileSync(join(here, 'scenarios.json'), 'utf8')).scenarios
  .filter(s => !args.scenario || args.scenario.includes(s.id));
const nodes = loadWorkflowNodes(join(root, 'docker/monitoring/n8n/workflows/alert-analysis.json'));

const settings = await call('GET', '/internal/config');
const NAME_FLOOR_DEFAULT = 0.3; // RAG_NAME_MATCH_MIN_SCORE; /query devuelve el vigente en name_match_min_score
const threshold = Number(settings.similarity_threshold ?? 0.63);
const providers = args.providers ?? DEFAULT_PROVIDERS;
const skipped = new Set(); // proveedores sin API key (422): se avisan una vez y no se vuelven a llamar

const rows = [];
const report = [`# Evaluación LLM/RAG — ${new Date().toISOString()}`, '',
  `API: ${API} · umbral vigente: ${threshold} · proveedores: ${args.ragOnly ? '(solo RAG)' : providers.join(', ')}`, ''];
const ragRows = [];

// Igual que el nodo 'Get Config': trae el contexto del entorno vigente (Configuración o ARGOS_ENVIRONMENT_CONTEXT).
const runtimeConfig = await call('GET', '/internal/config').catch(() => ({}));

for (const sc of scenarios) {
  const prep = runNode(nodes.Prepare, { input: alertmanagerPayload(sc, args.evidenceAt) });
  const rag = await call('POST', '/query', { text: prep.query_text, top_k: 3, alertname: prep.alertname });
  const results = rag.results ?? [];
  const floor = Number(rag.name_match_min_score ?? NAME_FLOOR_DEFAULT);
  const sameAlert = results.filter(r => r.tipo_alerta === sc.alertname).reduce((m, r) => Math.max(m, Number(r.score)), 0);
  ragRows.push({ sc, top: results[0]?.score ?? 0, topTipo: results[0]?.tipo_alerta ?? '-', isDocumented: !!rag.is_documented,
    reason: rag.match_reason ?? '-', sameAlert, floor });

  report.push(`## ${sc.id} · ${sc.alertname}`, '',
    `- **Falla:** ${sc.inject}`, `- **Causa raíz (referencia):** ${sc.root_cause}`, `- **Arreglo esperado:** ${sc.fix}`,
    `- **RAG:** is_documented=${rag.is_documented} · match_reason=${rag.match_reason ?? '-'} (esperado: ${sc.expected_kb})`,
    ...results.map((r, i) => `  ${i + 1}. ${Number(r.score).toFixed(3)} · ${r.tipo_alerta} · ${r.clasificacion ?? ''}`), '');
  if (args.ragOnly) continue;

  const evidence = args.noEvidence ? {} : await call('POST', '/internal/evidence',
    { alertname: prep.alertname, starts_at: prep.starts_at, labels: prep.labels }).catch(e => ({ error: e.message }));
  report.push(`- **Evidencia de logs:** ${evidence.error ? `❌ ${evidence.error}` : `${evidence.line_count ?? 0} líneas`}`, '',
    ...(evidence.text ? ['```', evidence.text, '```', ''] : []));
  const prompt = runNode(nodes['Build Prompt'], { refs: { Prepare: prep, 'Get Config': runtimeConfig, 'RAG Query': rag, 'Get Evidence': evidence }, json: evidence });
  const doc = rag.is_documented ? null : runNode(nodes['Build Doc Prompt'], { refs: { 'Build Prompt': prompt } });

  for (const spec of providers) {
    const [provider, model] = splitSpec(spec);
    if (skipped.has(provider)) continue;
    for (const [purpose, p] of [['analysis', prompt], ['document', doc]]) {
      if (!p) continue;
      const out = await generate(provider, model, p.system, p.user, purpose, sc);
      if (!out.ok && out.error.includes('no tiene API key')) {
        console.warn(`[skip] ${provider}: sin API key`);
        skipped.add(provider);
        break;
      }
      rows.push({ scenario: sc.id, alertname: sc.alertname, purpose, ...out });
      report.push(`### ${sc.id} · ${purpose} · ${out.provider}/${out.model} · ${out.latency_ms} ms`, '',
        out.ok ? out.text : `> ❌ ${out.error}`, '');
    }
  }
}

report.push('## RAG: clasificación conocida/nueva', '',
  '| Escenario | Alerta | Top score | Top match | Mismo tipo | is_documented | Motivo | Esperado | ¿OK? |',
  '|---|---|---|---|---|---|---|---|---|',
  ...ragRows.map(r => `| ${r.sc.id} | ${r.sc.alertname} | ${r.top.toFixed(3)} | ${r.topTipo} | ${r.sameAlert ? r.sameAlert.toFixed(3) : '-'} | ${r.isDocumented} | ${r.reason} | ${r.sc.expected_kb} | ${(r.isDocumented ? 'known' : 'new') === r.sc.expected_kb ? '✅' : '❌'} |`),
  '', '### Barrido de umbral (aprox.: solo similitud = top score > umbral; con nombre = además, mismo tipo ≥ piso)', '',
  '| Umbral | Aciertos (similitud) | Falsos "conocida" | Falsos "nueva" | Aciertos (+ nombre) | Falsos "conocida" | Falsos "nueva" |',
  '|---|---|---|---|---|---|---|',
  ...THRESHOLDS.map(t => {
    const score = pred => [`${pred.filter(p => p.exp === p.got).length}/${pred.length}`,
      pred.filter(p => p.exp === 'new' && p.got === 'known').length, pred.filter(p => p.exp === 'known' && p.got === 'new').length];
    const sim = score(ragRows.map(r => ({ exp: r.sc.expected_kb, got: r.top > t ? 'known' : 'new' })));
    const name = score(ragRows.map(r => ({ exp: r.sc.expected_kb,
      got: r.top > t || (r.sameAlert > 0 && r.sameAlert >= r.floor) ? 'known' : 'new' })));
    return `| ${t}${t === threshold ? ' (actual)' : ''} | ${sim.join(' | ')} | ${name.join(' | ')} |`;
  }), '');

const stamp = new Date().toISOString().replace(/[:T]/g, '-').slice(0, 19);
const outDir = join(here, 'results');
mkdirSync(outDir, { recursive: true });
writeFileSync(join(outDir, `llm-${stamp}.md`), report.join('\n'), 'utf8');
const header = ['scenario', 'alertname', 'purpose', 'provider', 'model', 'ok', 'latency_ms', 'chars',
  'diagnostico_1a5', 'accionabilidad_1a5', 'completitud_1a5', 'alucinaciones_n', 'claridad_1a5', 'notas'];
writeFileSync(join(outDir, `llm-${stamp}.csv`), '﻿' + [header.join(';'),
  ...rows.map(r => [r.scenario, r.alertname, r.purpose, r.provider, r.model, r.ok, r.latency_ms, r.text?.length ?? 0,
    '', '', '', '', '', csv(r.error ?? '')].join(';'))].join('\r\n'), 'utf8');
console.log(`OK → eval/results/llm-${stamp}.md / .csv (${rows.length} generaciones, ${ragRows.length} escenarios)`);

// ---------- helpers ----------

async function generate(provider, model, system, user, purpose, sc) {
  const started = Date.now();
  try {
    const r = await call('POST', '/internal/llm/generate',
      { system, user, purpose, alertname: sc.alertname, severity: sc.severity, provider, model });
    return { ok: true, provider: r.provider, model: r.model, text: r.text, latency_ms: Date.now() - started };
  } catch (e) {
    return { ok: false, provider, model: model ?? '(default)', error: e.message, latency_ms: Date.now() - started };
  }
}

async function call(method, path, body) {
  const res = await fetch(API + path, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { 'X-Argos-Token': token } : {}) },
    body: body ? JSON.stringify(body) : undefined,
    signal: AbortSignal.timeout(300_000),
  });
  const text = await res.text();
  if (!res.ok) throw new Error(`${method} ${path} → HTTP ${res.status}: ${text.slice(0, 300)}`);
  return text ? JSON.parse(text) : {};
}

// Payload con la forma del webhook de Alertmanager (el que recibe el nodo 'Prepare').
function alertmanagerPayload(sc, startsAtIso) {
  const labels = { alertname: sc.alertname, severity: sc.severity, ...(sc.labels || {}) };
  const annotations = { summary: sc.summary, description: sc.description };
  const startsAt = (startsAtIso ? new Date(startsAtIso) : new Date()).toISOString();
  return {
    body: {
      status: 'firing', commonLabels: labels, commonAnnotations: annotations,
      alerts: Array.from({ length: sc.alerts_count }, () => ({ labels, annotations, startsAt, endsAt: '0001-01-01T00:00:00Z' })),
    },
  };
}

// Ejecuta el jsCode de un nodo Code de n8n con un $() / $json / $input / $env mínimos.
function runNode(code, { input = {}, json = {}, refs = {} }) {
  const $ = name => ({ item: { json: refs[name] }, first: () => ({ json: refs[name] }) });
  const $input = { first: () => ({ json: input }), all: () => [{ json: input }] };
  const out = new Function('$', '$json', '$input', '$env', code)($, json, $input, process.env);
  if (!Array.isArray(out) || out.length === 0) throw new Error('El nodo no devolvió items (¿umbral de Prepare?)');
  return out[0].json;
}

function loadWorkflowNodes(path) {
  const wf = JSON.parse(readFileSync(path, 'utf8'));
  const list = Array.isArray(wf) ? wf[0].nodes : wf.nodes;
  const byName = Object.fromEntries(list.map(n => [n.name, n.parameters?.jsCode]));
  if (!list.some(n => n.name === 'Get Evidence')) throw new Error("El workflow no tiene el nodo 'Get Evidence'");
  for (const name of ['Prepare', 'Build Prompt', 'Build Doc Prompt'])
    if (!byName[name]) throw new Error(`El workflow no tiene el nodo Code '${name}'`);
  return byName;
}

function splitSpec(spec) {
  const i = spec.indexOf(':');
  return i < 0 ? [spec, undefined] : [spec.slice(0, i), spec.slice(i + 1)];
}

function parseArgs(argv) {
  const a = {};
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--scenario') a.scenario = argv[++i].split(',');
    else if (argv[i] === '--providers') a.providers = argv[++i].split(',');
    else if (argv[i] === '--rag-only') a.ragOnly = true;
    else if (argv[i] === '--no-evidence') a.noEvidence = true;
    else if (argv[i] === '--evidence-at') a.evidenceAt = argv[++i];
  }
  return a;
}

function readEnvFile(path) {
  if (!existsSync(path)) return {};
  return Object.fromEntries(readFileSync(path, 'utf8').split(/\r?\n/)
    .filter(l => /^[A-Z0-9_]+=/.test(l)).map(l => [l.slice(0, l.indexOf('=')), l.slice(l.indexOf('=') + 1).trim()]));
}

function csv(v) { return `"${String(v).replace(/"/g, '""').replace(/\r?\n/g, ' ')}"`; }
