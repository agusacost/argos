import { ADMIN, apiAs, cleanupE2eDocs, login, readRunState } from './helpers';

export default async function globalTeardown(): Promise<void> {
  const state = readRunState();
  const api = await apiAs(await login(ADMIN.email, ADMIN.password));

  // Restaurar settings tal como estaban antes de la suite
  const res = await api.patch('settings', { data: state.settings });
  if (!res.ok()) console.error(`[teardown] no se pudieron restaurar los settings: ${res.status()} ${await res.text()}`);

  // Borrar todos los usuarios e2e-* (aunque algún test haya fallado)
  const users = (await (await api.get('users')).json()) as { id: string; email: string }[];
  for (const u of users.filter((u) => u.email.startsWith('e2e-'))) await api.delete(`users/${u.id}`);

  // Documentos de la base de conocimiento creados por la suite (prefijo e2e-)
  const left = await cleanupE2eDocs(api);
  if (left.length > 0) {
    console.warn(`[teardown] ${left.length} documento(s) e2e-* no se pudieron borrar (sin DELETE en la API); quedan identificados por el prefijo: ${left.join(', ')}`);
  }

  // Cortar cualquier corrida k6 que haya quedado viva
  const runs = (await (await api.get('loadtests/runs')).json()) as { id: string; status: string }[];
  for (const r of runs.filter((r) => r.status === 'running')) await api.post(`loadtests/runs/${r.id}/stop`);

  await api.dispose();
}
