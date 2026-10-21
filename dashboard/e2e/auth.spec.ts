import { test, expect } from '@playwright/test';
import {
  ADMIN, ADMIN_STORAGE, OPERADOR_STORAGE, VIEWS,
  collectPageErrors, goToView, openApp, readRunState, sidebar,
} from './helpers';

test.describe('sin sesión', () => {
  test('S-01 login válido → layout con las 7 vistas (Admin)', async ({ page }) => {
    await page.goto('/');
    await page.getByPlaceholder('tu@empresa.com').fill(ADMIN.email);
    await page.getByPlaceholder('••••••••').fill(ADMIN.password);
    await page.getByRole('button', { name: 'Iniciar Sesión' }).click();
    await expect(sidebar(page)).toBeVisible();
    for (const label of Object.values(VIEWS)) {
      await expect(sidebar(page).getByRole('button', { name: label })).toBeVisible();
    }
    expect(await page.evaluate(() => localStorage.getItem('argos_token'))).toBeTruthy();
  });

  test('S-01 credenciales inválidas → error y sin token', async ({ page }) => {
    await page.goto('/');
    await page.getByPlaceholder('tu@empresa.com').fill(ADMIN.email);
    await page.getByPlaceholder('••••••••').fill('incorrecta');
    await page.getByRole('button', { name: 'Iniciar Sesión' }).click();
    await expect(page.locator('.text-red-400').first()).toBeVisible();
    expect(await page.evaluate(() => localStorage.getItem('argos_token'))).toBeNull();
    await expect(sidebar(page)).toHaveCount(0);
  });

  test('S-02 token inválido en localStorage → vuelve al login', async ({ page }) => {
    await page.goto('/');
    await page.evaluate(() => localStorage.setItem('argos_token', 'token.invalido.xxx'));
    await page.reload();
    await expect(page.getByRole('button', { name: 'Iniciar Sesión' })).toBeVisible();
    expect(await page.evaluate(() => localStorage.getItem('argos_token'))).toBeNull();
  });
});

test.describe('Admin', () => {
  test.use({ storageState: ADMIN_STORAGE });

  test('S-02 F5 mantiene la sesión', async ({ page }) => {
    await openApp(page);
    await page.reload();
    await expect(sidebar(page)).toBeVisible();
  });

  test('S-03 logout limpia el token', async ({ page }) => {
    await openApp(page);
    await page.getByTitle('Cerrar sesión').click();
    await expect(page.getByRole('button', { name: 'Iniciar Sesión' })).toBeVisible();
    expect(await page.evaluate(() => localStorage.getItem('argos_token'))).toBeNull();
  });

  test('S-05 el título del header corresponde a cada vista', async ({ page }) => {
    await openApp(page);
    const expected: Record<keyof typeof VIEWS, RegExp> = {
      dashboard: /Dashboard/,
      incidents: /Incidentes/,
      knowledge: /Base de Conocimiento/,
      metrics: /Métricas/,
      loadtests: /Pruebas de Carga/,
      users: /Usuarios/,
      settings: /Configuración/,
    };
    for (const view of Object.keys(VIEWS) as (keyof typeof VIEWS)[]) {
      await goToView(page, view);
      await expect(page.locator('header h1'), view).toHaveText(expected[view]);
    }
  });

  test('S-04 recorrer todas las vistas sin errores de JS', async ({ page }) => {
    const errors = collectPageErrors(page);
    await openApp(page);
    for (const view of Object.keys(VIEWS) as (keyof typeof VIEWS)[]) {
      await goToView(page, view);
      await page.waitForLoadState('networkidle');
    }
    expect(errors).toEqual([]);
  });

  test('M-01 abrir Métricas antes de que carguen las métricas no rompe la vista', async ({ page }) => {
    const errors = collectPageErrors(page);
    // Demora artificial de /incidents/metrics para entrar a la vista con metrics=null.
    await page.route('**/api/incidents/metrics', async (route) => {
      await new Promise((r) => setTimeout(r, 2500));
      await route.continue();
    });
    await page.goto('/');
    await goToView(page, 'metrics');
    await expect(page.getByText('MTTR Promedio')).toBeVisible({ timeout: 15_000 });
    expect(errors.filter((e) => e.includes('pageerror'))).toEqual([]);
  });
});

test.describe('Operador', () => {
  test.use({ storageState: OPERADOR_STORAGE });

  test('S-04 el Operador no ve Gestión de Usuarios', async ({ page }) => {
    await openApp(page);
    await expect(sidebar(page).getByRole('button', { name: VIEWS.users })).toHaveCount(0);
    await expect(sidebar(page).getByText(readRunState().operador.email.split('@')[0])).toHaveCount(0);
    await expect(sidebar(page).getByRole('button', { name: VIEWS.settings })).toBeVisible();
  });
});
