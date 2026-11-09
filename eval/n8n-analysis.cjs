// Se ejecuta DENTRO del contenedor argos-n8n (lo usa eval/run-auto.mjs):
//   docker exec -i -w /usr/local/lib/node_modules/n8n argos-n8n node - <desde ISO> < eval/n8n-analysis.cjs
// Devuelve (JSON) las ejecuciones del workflow iniciadas después de <desde> con el análisis que se envió por email.
const sqlite3 = require('sqlite3');
const { parse } = require('flatted');
const since = (process.argv[2] || '1970-01-01T00:00:00Z').replace('T', ' ').replace('Z', '');
const db = new sqlite3.Database('/home/node/.n8n/database.sqlite', sqlite3.OPEN_READONLY);
db.all(`select e.id, e.status, e.startedAt, e.stoppedAt, d.data from execution_entity e
        join execution_data d on d.executionId = e.id where e.startedAt >= ? order by e.id`, [since], (err, rows) => {
  if (err) { console.log(JSON.stringify({ error: err.message })); return; }
  const out = rows.map(r => {
    let run = {};
    try { run = parse(r.data).resultData.runData || {}; } catch { }
    const json = name => run[name]?.[0]?.data?.main?.[0]?.[0]?.json;
    const parsed = json('Parse');
    return {
      id: r.id, status: r.status, startedAt: r.startedAt, stoppedAt: r.stoppedAt,
      nodes: Object.keys(run),
      alertname: parsed?.alertname ?? json('Prepare')?.alertname ?? null,
      status_alert: json('Prepare')?.status ?? null,
      analysis: parsed?.analysis ?? null,
      llm: parsed ? `${parsed.llm_provider}/${parsed.llm_model}${parsed.fallback_used ? ' (fallback)' : ''}` : null,
      is_documented: parsed?.is_documented ?? null,
    };
  });
  console.log(JSON.stringify(out));
});
