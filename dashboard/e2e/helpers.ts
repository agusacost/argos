import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, request, type APIRequestContext, type Page } from '@playwright/test';

export const BASE_URL = process.env.E2E_BASE_URL ?? 'http://localhost:3001';
export const API_URL = `${BASE_URL}/api`;
export const ADMIN = {
  email: process.env.E2E_ADMIN_EMAIL ?? 'admin@argos.local',
  password: process.env.E2E_ADMIN_PASSWORD ?? 'argos-admin-2026',
};

export const STATE_DIR = path.join(path.dirname(fileURLToPath(import.meta.url)), '.state');
export const ADMIN_STORAGE = path.join(STATE_DIR, 'admin.json');
export const OPERADOR_STORAGE = path.join(STATE_DIR, 'operador.json');
const RUN_STATE = path.join(STATE_DIR, 'run.json');

export interface RunState {
  operador: { id: string; email: string; password: string };
  settings: Record<string, unknown>;
}

export function readRunState(): RunState {
  return JSON.parse(fs.readFileSync(RUN_STATE, 'utf8')) as RunState;
}

export function writeRunState(state: RunState): void {
  fs.mkdirSync(STATE_DIR, { recursive: true });
  fs.writeFileSync(RUN_STATE, JSON.stringify(state, null, 2));
}

export async function login(email: string, password: string): Promise<string> {
  const ctx = await request.newContext();
  const res = await ctx.post(`${API_URL}/auth/login`, { data: { email, password } });
  expect(res.status(), `login ${email}`).toBe(200);
  const body = (await res.json()) as { access_token: string };
  await ctx.dispose();
  return body.access_token;
}

/** Cliente HTTP autenticado contra /api (pasa por el nginx del dashboard). */
export async function apiAs(token?: string): Promise<APIRequestContext> {
  return request.newContext({
    baseURL: `${API_URL}/`,
    extraHTTPHeaders: token ? { Authorization: `Bearer ${token}` } : {},
  });
}

export function storageStateFor(token: string) {
  return {
    cookies: [],
    origins: [{ origin: BASE_URL, localStorage: [{ name: 'argos_token', value: token }] }],
  };
}

export const VIEWS = {
  dashboard: 'Dashboard',
  incidents: 'Incidentes Activos',
  knowledge: 'Base de Conocimiento',
  metrics: 'Métricas de Sistema',
  loadtests: 'Pruebas de Carga',
  users: 'Gestión de Usuarios',
  settings: 'Configuración',
} as const;

/** Ruta de Next.js de cada vista (espejo de src/routes.ts). */
export const ROUTES: Record<keyof typeof VIEWS, string> = {
  dashboard: '/',
  incidents: '/incidentes',
  knowledge: '/conocimiento',
  metrics: '/metricas',
  loadtests: '/pruebas-carga',
  users: '/usuarios',
  settings: '/configuracion',
};

export function sidebar(page: Page) {
  return page.locator('aside nav');
}

/** Ítem del sidebar de una vista (un <Link> de Next.js). */
export function navLink(page: Page, view: keyof typeof VIEWS) {
  return sidebar(page).getByRole('link', { name: VIEWS[view] });
}

/** Navega con el sidebar y espera la URL de la vista. */
export async function goToView(page: Page, view: keyof typeof VIEWS): Promise<void> {
  await navLink(page, view).click();
  await expect(page).toHaveURL(new RegExp(`${ROUTES[view] === '/' ? '/' : ROUTES[view]}$`));
}

/** Abre la app ya logueada (storageState) y espera el layout. */
export async function openApp(page: Page): Promise<void> {
  await page.goto('/');
  await expect(sidebar(page)).toBeVisible();
}

/** Errores de consola / excepciones no capturadas durante el test. */
export function collectPageErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (m) => {
    if (m.type() === 'error') errors.push(`console: ${m.text()}`);
  });
  return errors;
}

/** Prefijo de todo lo que crea la suite (usuarios, documentos): permite identificarlo y limpiarlo. */
export const E2E_PREFIX = 'e2e-';

/**
 * Elimina los documentos de la base de conocimiento creados por la suite (tipo_alerta que empieza con `e2e-`).
 * Intenta DELETE /incidents/{id}; si el backend no lo ofrece (404/405) los deja identificables por el prefijo
 * y devuelve cuáles quedaron para que el llamador lo informe. Nunca lanza: es limpieza best-effort.
 */
export async function cleanupE2eDocs(api: APIRequestContext): Promise<string[]> {
  const left: string[] = [];
  try {
    const res = await api.get(`incidents?search=${encodeURIComponent(E2E_PREFIX)}&size=100`);
    if (!res.ok()) return left;
    const { data } = (await res.json()) as { data: { id: string; tipo_alerta: string }[] };
    for (const d of data.filter((d) => d.tipo_alerta.toLowerCase().startsWith(E2E_PREFIX))) {
      const del = await api.delete(`incidents/${d.id}`);
      if (!del.ok()) left.push(`${d.id} (${d.tipo_alerta})`);
    }
  } catch {
    /* limpieza best-effort */
  }
  return left;
}
