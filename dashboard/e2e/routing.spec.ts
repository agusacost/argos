import { test, expect } from '@playwright/test';
import {
  ADMIN, ADMIN_STORAGE, OPERADOR_STORAGE, ROUTES, VIEWS,
  collectPageErrors, goToView, navLink, sidebar,
} from './helpers';

// Routing del panel en Next.js (App Router, export estático servido por nginx).

type View = keyof typeof VIEWS;
const VIEW_IDS = Object.keys(VIEWS) as View[];

const TITLES: Record<View, RegExp> = {
  dashboard: /Dashboard/,
  incidents: /Incidentes/,
  knowledge: /Base de Conocimiento/,
  metrics: /Métricas/,
  loadtests: /Pruebas de Carga/,
  users: /Usuarios/,
  settings: /Configuración/,
};

const estadoSelect = (page: import('@playwright/test').Page) =>
  page.locator('select').filter({ has: page.locator('option', { hasText: 'Todos los estados' }) });

test.describe('Admin', () => {
  test.use({ storageState: ADMIN_STORAGE });

  test('R-01 cada ruta abre su vista por URL, marca el ítem activo y sobrevive a F5', async ({ page }) => {
    const errors = collectPageErrors(page);
    for (const view of VIEW_IDS) {
      const res = await page.goto(ROUTES[view]);
      expect(res?.status(), ROUTES[view]).toBe(200);
      await expect(page.locator('header h1'), view).toHaveText(TITLES[view]);
      await expect(navLink(page, view)).toHaveAttribute('aria-current', 'page');
      await page.reload();
      await expect(page.locator('header h1'), `${view} tras F5`).toHaveText(TITLES[view]);
    }
    // Incluye errores de hidratación de React, que se reportan por consola.
    expect(errors).toEqual([]);
  });

  test('R-02 atrás y adelante del navegador recorren las vistas', async ({ page }) => {
    await page.goto('/');
    await goToView(page, 'metrics');
    await goToView(page, 'knowledge');
    await page.goBack();
    await expect(page).toHaveURL(/\/metricas$/);
    await expect(page.locator('header h1')).toHaveText(TITLES.metrics);
    await page.goBack();
    await expect(page.locator('header h1')).toHaveText(TITLES.dashboard);
    await page.goForward();
    await expect(page).toHaveURL(/\/metricas$/);
  });

  test('R-05 /incidentes?estado=nuevo aplica el filtro; el sidebar lo limpia', async ({ page }) => {
    const filtered = page.waitForRequest((r) => r.url().includes('/api/incidents?') && r.url().includes('estado=nuevo'));
    await page.goto('/incidentes?estado=nuevo');
    await filtered;
    await expect(estadoSelect(page)).toHaveValue('nuevo');

    const unfiltered = page.waitForRequest((r) => r.url().includes('/api/incidents?') && !r.url().includes('estado='));
    await goToView(page, 'incidents');
    await unfiltered;
    await expect(estadoSelect(page)).toHaveValue('');
  });

  test('R-06 el KPI "Pendientes de documentar" navega a /incidentes?estado=nuevo', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('button').filter({ hasText: 'Pendientes de documentar' }).click();
    await expect(page).toHaveURL(/\/incidentes\?estado=nuevo$/);
    await expect(page.locator('header h1')).toHaveText(TITLES.incidents);
    await expect(estadoSelect(page)).toHaveValue('nuevo');
  });

  test('R-07 una ruta inexistente responde 404 con enlace al Dashboard', async ({ page }) => {
    const res = await page.goto('/no-existe');
    expect(res?.status()).toBe(404);
    await expect(page.getByText('Página no encontrada')).toBeVisible();
    await page.getByRole('link', { name: 'Volver al Dashboard' }).click();
    await expect(page).toHaveURL(/\/$/);
    await expect(page.locator('header h1')).toHaveText(TITLES.dashboard);
  });
});

test.describe('sin sesión', () => {
  test('R-03 un deep link pide login y luego abre la vista pedida', async ({ page }) => {
    await page.goto('/metricas');
    await expect(page.getByRole('button', { name: 'Iniciar Sesión' })).toBeVisible();
    await expect(sidebar(page)).toHaveCount(0);
    await page.getByPlaceholder('tu@empresa.com').fill(ADMIN.email);
    await page.getByPlaceholder('••••••••').fill(ADMIN.password);
    await page.getByRole('button', { name: 'Iniciar Sesión' }).click();
    await expect(page).toHaveURL(/\/metricas$/);
    await expect(page.locator('header h1')).toHaveText(TITLES.metrics);
  });
});

test.describe('Operador', () => {
  test.use({ storageState: OPERADOR_STORAGE });

  test('R-04 /usuarios por URL redirige al Dashboard', async ({ page }) => {
    await page.goto('/usuarios');
    await expect(page).toHaveURL(/\/$/);
    await expect(page.locator('header h1')).toHaveText(TITLES.dashboard);
    await expect(navLink(page, 'users')).toHaveCount(0);
  });
});
