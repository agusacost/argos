import { test, expect, type APIRequestContext } from '@playwright/test';
import { ADMIN, apiAs, login, readRunState } from './helpers';

// Capa A: endpoints que consume el dashboard, vía el proxy nginx :3001/api.

let admin: APIRequestContext;
let operador: APIRequestContext;
let anon: APIRequestContext;
let incidentId: string;

test.beforeAll(async () => {
  const { operador: op } = readRunState();
  admin = await apiAs(await login(ADMIN.email, ADMIN.password));
  operador = await apiAs(await login(op.email, op.password));
  anon = await apiAs();
  const list = (await (await admin.get('incidents?size=1')).json()) as { data: { id: string }[] };
  incidentId = list.data[0].id;
});

test.afterAll(async () => {
  await Promise.all([admin.dispose(), operador.dispose(), anon.dispose()]);
});

test.describe('auth', () => {
  test('S-01 login con password errónea → 401', async () => {
    const res = await anon.post('auth/login', { data: { email: ADMIN.email, password: 'mal' } });
    expect(res.status()).toBe(401);
  });

  test('S-02 /auth/me sin token → 401, con token → usuario', async () => {
    expect((await anon.get('auth/me')).status()).toBe(401);
    const me = await admin.get('auth/me');
    expect(me.status()).toBe(200);
    expect(await me.json()).toMatchObject({ email: ADMIN.email, role: 'Admin' });
  });
});

test.describe('lectura (Admin)', () => {
  test('I-01 /incidents paginado', async () => {
    const body = await (await admin.get('incidents?page=1&size=10')).json();
    expect(body.data.length).toBeGreaterThan(0);
    expect(body.data.length).toBeLessThanOrEqual(10);
    expect(body.total).toBeGreaterThanOrEqual(5);
  });

  test('D-01 /incidents/metrics', async () => {
    const m = await (await admin.get('incidents/metrics')).json();
    expect(m.total_incidents).toBeGreaterThan(0);
    expect(Object.keys(m.by_status).length).toBeGreaterThan(0);
    expect(Array.isArray(m.incidents_per_day)).toBe(true);
  });

  test('K-01 /incidents/knowledge-base solo estados documentados', async () => {
    const body = await (await admin.get('incidents/knowledge-base?size=100')).json();
    expect(body.total).toBeGreaterThanOrEqual(5);
    for (const r of body.data) expect(['documentado_previamente', 'auto_documentado']).toContain(r.estado);
  });

  test('I-05 detalle, observabilidad y auditoría', async () => {
    expect((await admin.get(`incidents/${incidentId}`)).status()).toBe(200);
    const obs = await (await admin.get(`incidents/${incidentId}/observability`)).json();
    expect(Array.isArray(obs.logs)).toBe(true);
    expect(Array.isArray(obs.sparkline)).toBe(true);
    expect((await admin.get(`incidents/${incidentId}/audit`)).status()).toBe(200);
  });

  test('I-05 incidente inexistente → 404', async () => {
    expect((await admin.get('incidents/00000000-0000-0000-0000-000000000000')).status()).toBe(404);
  });

  test('M-03 salud de servicios: 4 healthy', async () => {
    const svcs = (await (await admin.get('observability/services')).json()) as { name: string; status: string }[];
    expect(svcs).toHaveLength(4);
    for (const s of svcs) expect(s.status, s.name).toBe('healthy');
  });

  for (const range of ['15m', '1h', '3h', '6h']) {
    test(`M-04 nginx-metrics range=${range}`, async () => {
      // Loki tarda en ingerir tras un arranque en frío: se reintenta hasta 60 s.
      await expect.poll(async () => {
        const m = await (await admin.get(`observability/nginx-metrics?range=${range}`)).json();
        expect(m.range).toBe(range);
        expect(m.timestamps.length).toBeGreaterThan(0);
        return m.request_rate.reduce((a: number, b: number) => a + b, 0);
      }, { message: 'sin tráfico de nginx en Loki', timeout: 60_000, intervals: [5_000] }).toBeGreaterThan(0);
    });
  }

  test('S-06 / D-05 notificaciones y actividad', async () => {
    expect((await admin.get('notifications')).status()).toBe(200);
    expect((await admin.get('activity')).status()).toBe(200);
  });

  test('C-06 salud de la cadena LLM: principal primero y estados válidos', async () => {
    const chain = (await (await admin.get('llm/health')).json()) as { role: string; status: string }[];
    expect(chain.length).toBeGreaterThan(0);
    expect(chain[0].role).toBe('primary');
    for (const h of chain) expect(['up', 'down', 'unknown', 'unconfigured']).toContain(h.status);
  });

  test('C-05 integraciones en vivo: todas up', async () => {
    const its = (await (await admin.get('integrations/status')).json()) as { name: string; status: string }[];
    expect(its.length).toBeGreaterThan(0);
    for (const it of its) expect(it.status, it.name).toBe('up');
  });

  test('L-01 4 escenarios con código', async () => {
    const sc = (await (await admin.get('loadtests/scenarios')).json()) as { id: string; code: string }[];
    expect(sc.map((s) => s.id).sort()).toEqual(['attack', 'flood', 'normal', 'spike']);
    for (const s of sc) expect(s.code.length).toBeGreaterThan(50);
  });
});

test.describe('validaciones', () => {
  test('C-07 PATCH /settings inválido → 422', async () => {
    expect((await admin.patch('settings', { data: { similarity_threshold: 5 } })).status()).toBe(422);
    expect((await admin.patch('settings', { data: { alert_recipients: ['no-es-email'] } })).status()).toBe(422);
  });
});

test.describe('RBAC: Operador', () => {
  test('U-01 /users → 403', async () => {
    expect((await operador.get('users')).status()).toBe(403);
  });
  test('C-07 PATCH /settings → 403', async () => {
    expect((await operador.patch('settings', { data: { polling_interval_seconds: 30 } })).status()).toBe(403);
  });
  test('L-03 POST /loadtests/runs → 403', async () => {
    expect((await operador.post('loadtests/runs', { data: { scenario: 'normal' } })).status()).toBe(403);
  });
  test('I-07 POST /incidents/{id}/document → 403 (solo Admin, como la UI)', async () => {
    // El 403 sale de la autorización, antes de llamar al LLM: no modifica nada.
    expect((await operador.post(`incidents/${incidentId}/document`)).status()).toBe(403);
  });
});

test.describe('seguridad: endpoints sin token', () => {
  test('I-01 GET /incidents sin token → 401', async () => {
    expect((await anon.get('incidents?size=1')).status()).toBe(401);
  });
  test('I-06 PATCH /incidents/{id} sin token → 401', async () => {
    // Body vacío: si llega a la lógica no modifica nada.
    expect((await anon.patch(`incidents/${incidentId}`, { data: {} })).status()).toBe(401);
  });
  test('D-05 GET /activity sin token → 401', async () => {
    expect((await anon.get('activity')).status()).toBe(401);
  });
  for (const path of [
    'incidents/metrics', 'incidents/knowledge-base', 'observability/services',
    'observability/nginx-metrics?range=1h',
  ]) {
    test(`GET /${path} sin token → 401`, async () => {
      expect((await anon.get(path)).status()).toBe(401);
    });
  }
  test('I-05 detalle/observabilidad/auditoría de un incidente sin token → 401', async () => {
    for (const p of ['', '/observability', '/audit']) {
      expect((await anon.get(`incidents/${incidentId}${p}`)).status(), p || 'detalle').toBe(401);
    }
  });
});

test.describe('seguridad: endpoints internos de n8n', () => {
  for (const [method, path] of [
    ['POST', 'query'], ['POST', 'upsert'], ['GET', 'internal/config'], ['GET', 'notifications/recipients'],
    ['POST', 'internal/llm/generate'],
  ] as const) {
    test(`${method} /${path} no se expone por el proxy del dashboard`, async () => {
      const res = await anon.fetch(path, { method, data: method === 'POST' ? {} : undefined });
      expect(res.status()).toBe(404);
    });
  }
});
