import { test, expect, type Page } from '@playwright/test';
import { ADMIN, ADMIN_STORAGE, OPERADOR_STORAGE, apiAs, goToView, login, openApp } from './helpers';

const rows = (page: Page) => page.locator('main div.divide-y > button');
const drawer = (page: Page) => page.locator('div.fixed.right-0.max-w-xl');

async function adminApi() {
  return apiAs(await login(ADMIN.email, ADMIN.password));
}

test.describe('Admin', () => {
  test.use({ storageState: ADMIN_STORAGE });

  test.beforeEach(async ({ page }) => {
    await openApp(page);
    await goToView(page, 'incidents');
    await expect(rows(page).first()).toBeVisible();
  });

  test('I-01 tabla paginada coincide con el total de la API', async ({ page }) => {
    const api = await adminApi();
    const { total } = await (await api.get('incidents?size=1')).json();
    await api.dispose();
    await expect(page.getByText(`${total} incidentes · Click para ver detalle`)).toBeVisible();
    expect(await rows(page).count()).toBe(Math.min(20, total));

    test.skip(total <= 20, 'una sola página');
    const firstId = await rows(page).first().locator('div').first().innerText();
    await page.getByRole('button', { name: 'Siguiente →' }).click();
    await expect(page.getByText(/Página 2 de/)).toBeVisible();
    await expect(rows(page).first().locator('div').first()).not.toHaveText(firstId);
    await page.getByRole('button', { name: '← Anterior' }).click();
    await expect(page.getByText(/Página 1 de/)).toBeVisible();
  });

  test('I-03 filtro por estado', async ({ page }) => {
    await page.locator('main select').selectOption('resuelto');
    await expect(page.getByText('Cargando incidentes…')).toHaveCount(0);
    const n = await rows(page).count();
    expect(n).toBeGreaterThan(0);
    for (let i = 0; i < n; i++) await expect(rows(page).nth(i)).toContainText('Resuelto');
  });

  test('I-04 filtro por tipo', async ({ page }) => {
    await page.getByPlaceholder('Filtrar por tipo…').fill('Nginx');
    await expect(page.getByText('Cargando incidentes…')).toHaveCount(0);
    const n = await rows(page).count();
    expect(n).toBeGreaterThan(0);
    for (let i = 0; i < n; i++) await expect(rows(page).nth(i).locator('p').first()).toContainText(/nginx/i);
  });

  test('I-02 búsqueda por texto', async ({ page }) => {
    await page.getByPlaceholder('Buscar por resumen o tipo…').fill('SqlServer');
    await expect(page.getByText('Cargando incidentes…')).toHaveCount(0);
    const n = await rows(page).count();
    expect(n).toBeGreaterThan(0);
    for (let i = 0; i < n; i++) await expect(rows(page).nth(i)).toContainText(/sql/i);
    await page.getByPlaceholder('Buscar por resumen o tipo…').fill('zzz-no-existe-zzz');
    await expect(page.getByText('Sin incidentes que coincidan.')).toBeVisible();
  });

  test('I-05 detalle: logs, análisis IA y auditoría', async ({ page }) => {
    await rows(page).first().click();
    const d = drawer(page);
    await expect(d.getByText('Logs Relevantes (Loki)')).toBeVisible();
    await expect(d.getByText('Cargando logs…')).toHaveCount(0, { timeout: 20_000 });
    await expect(d.getByText('Análisis de Argos')).toBeVisible();
    await expect(d.getByText('Trazabilidad de Auditoría')).toBeVisible();
    await expect(d.getByText('Cargando…')).toHaveCount(0);
    // Cerrar con el botón del header del panel
    await d.locator('button').first().click();
    await expect(d).toHaveCount(0);
  });

  test('I-06 marcar como resuelto: estado, MTTR, auditoría y refresco de la lista', async ({ page }) => {
    const api = await adminApi();
    const pend = (await (await api.get('incidents?estado=pendiente&size=1')).json()).data[0];
    test.skip(!pend, 'no hay incidentes pendientes');
    try {
      await page.locator('main select').selectOption('pendiente');
      const row = rows(page).filter({ hasText: pend.id.slice(0, 8) });
      await row.click();
      await drawer(page).getByRole('button', { name: 'Marcar como Resuelto' }).click();

      await expect.poll(async () => (await (await api.get(`incidents/${pend.id}`)).json()).estado).toBe('resuelto');
      const inc = await (await api.get(`incidents/${pend.id}`)).json();
      expect(inc.mttr_minutes).not.toBeNull();
      const audit = (await (await api.get(`incidents/${pend.id}/audit`)).json()) as { user: string; action: string }[];
      expect(audit.some((a) => a.user === ADMIN.email && a.action.includes('resuelto'))).toBe(true);

      // La lista (filtrada por "pendiente") debería reflejar el cambio sin recargar.
      await expect(rows(page).filter({ hasText: pend.id.slice(0, 8) })).toHaveCount(0, { timeout: 5_000 });
    } finally {
      await api.patch(`incidents/${pend.id}`, { data: { estado: 'pendiente' } });
      await api.dispose();
    }
  });

  test('I-07 @llm aprobar y auto-documentar con el LLM', async ({ page }) => {
    test.setTimeout(180_000);
    const api = await adminApi();
    const nuevo = (await (await api.get('incidents?estado=nuevo&size=1')).json()).data[0];
    test.skip(!nuevo, 'no hay incidentes en estado nuevo');
    try {
      await page.locator('main select').selectOption('nuevo');
      await rows(page).filter({ hasText: nuevo.id.slice(0, 8) }).click();
      const done = page.waitForResponse((r) => r.url().includes(`/incidents/${nuevo.id}/document`), { timeout: 150_000 });
      await drawer(page).getByRole('button', { name: /Aprobar y Auto-documentar/ }).click();
      const res = await done;
      expect(res.status(), await res.text()).toBe(200);
      const inc = await (await api.get(`incidents/${nuevo.id}`)).json();
      expect(inc.estado).toBe('auto_documentado');
      expect((inc.documento ?? '').length).toBeGreaterThan(200);
      const audit = (await (await api.get(`incidents/${nuevo.id}/audit`)).json()) as { action: string }[];
      expect(audit.some((a) => a.action.includes('Documentación generada'))).toBe(true);
    } finally {
      await api.patch(`incidents/${nuevo.id}`, { data: { estado: 'nuevo' } });
      await api.dispose();
    }
  });
});

test.describe('Operador', () => {
  test.use({ storageState: OPERADOR_STORAGE });

  test('I-07 el Operador no ve "Aprobar y Auto-documentar" pero sí "Marcar como Resuelto"', async ({ page }) => {
    await openApp(page);
    await goToView(page, 'incidents');
    await page.locator('main select').selectOption('pendiente');
    await expect(rows(page).first()).toBeVisible();
    await rows(page).first().click();
    await expect(drawer(page).getByRole('button', { name: 'Marcar como Resuelto' })).toBeVisible();
    await expect(drawer(page).getByRole('button', { name: /Aprobar y Auto-documentar/ })).toHaveCount(0);
  });
});
