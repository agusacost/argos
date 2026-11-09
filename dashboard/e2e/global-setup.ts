import fs from 'node:fs';
import {
  ADMIN, ADMIN_STORAGE, BASE_URL, OPERADOR_STORAGE, STATE_DIR,
  apiAs, login, storageStateFor, writeRunState,
} from './helpers';

export default async function globalSetup(): Promise<void> {
  // 1. Stack arriba
  const anon = await apiAs();
  const home = await anon.get(BASE_URL);
  if (home.status() !== 200) {
    throw new Error(`Dashboard no responde en ${BASE_URL} (HTTP ${home.status()}). Levantar con: docker compose up -d`);
  }

  const adminToken = await login(ADMIN.email, ADMIN.password);
  const api = await apiAs(adminToken);

  // 2. Datos semilla: los 5 runbooks (los incidentes demo son opcionales, ARGOS_SEED_DEMO)
  const kb = (await (await api.get('incidents/knowledge-base?size=1')).json()) as { total: number };
  if (kb.total < 5) {
    throw new Error(`Seed incompleto: KB=${kb.total} (esperado ≥5 runbooks)`);
  }

  // 3. Snapshot de settings (se restaura en el teardown)
  const settings = (await (await api.get('settings')).json()) as Record<string, unknown>;
  delete settings.llm_keys;
  delete settings.warning;

  // 4. Limpieza de corridas previas + Operador temporal
  const users = (await (await api.get('users')).json()) as { id: string; email: string }[];
  for (const u of users.filter((u) => u.email.startsWith('e2e-'))) await api.delete(`users/${u.id}`);

  const operador = {
    email: `e2e-operador-${Date.now()}@argos.local`,
    password: 'e2e-Operador-2026',
  };
  const created = await api.post('users', {
    data: { ...operador, name: 'E2E Operador', role: 'Operador' },
  });
  if (!created.ok()) throw new Error(`No se pudo crear el Operador: ${created.status()} ${await created.text()}`);
  const { id } = (await created.json()) as { id: string };

  // 5. Tráfico para que Loki tenga series de nginx (golden signals)
  // (sin .catch: si nginx no responde, mejor que el setup falle a que M-04 dé un falso negativo)
  for (let i = 0; i < 30; i++) await anon.get('http://localhost/api/status');

  await Promise.all([api.dispose(), anon.dispose()]);

  const operadorToken = await login(operador.email, operador.password);
  fs.mkdirSync(STATE_DIR, { recursive: true });
  fs.writeFileSync(ADMIN_STORAGE, JSON.stringify(storageStateFor(adminToken)));
  fs.writeFileSync(OPERADOR_STORAGE, JSON.stringify(storageStateFor(operadorToken)));
  writeRunState({ operador: { id, ...operador }, settings });
}
