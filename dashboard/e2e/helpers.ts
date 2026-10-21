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

export function sidebar(page: Page) {
  return page.locator('aside nav');
}

/** Las vistas no tienen URL: se navega con el sidebar. */
export async function goToView(page: Page, view: keyof typeof VIEWS): Promise<void> {
  await sidebar(page).getByRole('button', { name: VIEWS[view] }).click();
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
