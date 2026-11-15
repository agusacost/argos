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

  test('I-04 filtro por tipo: coincidencia parcial y sin distinguir mayúsculas', async ({ page }) => {
    // El texto de ayuda de la vista describe la coincidencia parcial.
    await expect(page.getByText('coincide con cualquier parte del nombre', { exact: false })).toBeVisible();
    // «gInX» (parcial, mayúsculas mezcladas) debe encontrar NginxDown, NginxHighErrorRate, etc.
    await page.getByPlaceholder('Filtrar por tipo…').fill('gInX');
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

  /** Abre el detalle de un incidente que todavía muestra las acciones (nuevo o pendiente). */
  async function openActionableIncident(page: Page): Promise<string> {
    const api = await adminApi();
    let inc = (await (await api.get('incidents?estado=pendiente&size=1')).json()).data[0];
    let estado = 'pendiente';
    if (!inc) {
      inc = (await (await api.get('incidents?estado=nuevo&size=1')).json()).data[0];
      estado = 'nuevo';
    }
    await api.dispose();
    test.skip(!inc, 'no hay incidentes nuevos ni pendientes');
    await page.locator('main select').selectOption(estado);
    await rows(page).filter({ hasText: inc.id.slice(0, 8) }).click();
    await expect(drawer(page).getByRole('button', { name: 'Marcar como Resuelto' })).toBeVisible();
    return inc.id as string;
  }

  test('I-07 un error al documentar con IA se muestra en el panel (que sigue abierto) y se puede reintentar', async ({ page }) => {
    const id = await openActionableIncident(page);
    let calls = 0;
    await page.route(`**/api/incidents/${id}/document`, (route) => {
      calls++;
      return route.fulfill({
        status: 502,
        contentType: 'application/json',
        body: JSON.stringify({ detail: 'El proveedor de IA respondió 503 (alta demanda).' }),
      });
    });
    const boton = drawer(page).getByRole('button', { name: /Aprobar y Auto-documentar/ });
    await boton.click();
    const alerta = drawer(page).getByRole('alert');
    await expect(alerta).toContainText('No se pudo generar la documentación: El proveedor de IA respondió 503');
    await expect(alerta).toContainText('se puede reintentar');
    // El panel no se cerró y el botón volvió a estar disponible.
    await expect(drawer(page)).toBeVisible();
    await expect(boton).toBeEnabled();
    await boton.click();
    await expect.poll(() => calls).toBe(2);
    await expect(alerta).toBeVisible();
  });

  test('I-06 un error al marcar como resuelto se muestra en el panel y el incidente no cambia', async ({ page }) => {
    const id = await openActionableIncident(page);
    await page.route(`**/api/incidents/${id}`, (route) => {
      if (route.request().method() !== 'PATCH') return route.fallback();
      return route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ detail: 'No se pudo guardar el cambio en Qdrant.' }),
      });
    });
    await drawer(page).getByRole('button', { name: 'Marcar como Resuelto' }).click();
    await expect(drawer(page).getByRole('alert')).toContainText(
      'No se pudo marcar el incidente como resuelto: No se pudo guardar el cambio en Qdrant',
    );
    await expect(drawer(page)).toBeVisible();
    await expect(drawer(page).getByRole('button', { name: 'Marcar como Resuelto' })).toBeEnabled();
  });

  test('I-07 @llm aprobar y auto-documentar con el LLM (éxito, o motivo visible si el proveedor falla)', async ({ page }) => {
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
      if (res.status() >= 500) {
        // El proveedor de IA puede estar saturado (503 → 502): el operador debe ver el motivo y poder reintentar.
        await expect(drawer(page).getByRole('alert')).toContainText('No se pudo generar la documentación');
        await expect(drawer(page).getByRole('button', { name: /Aprobar y Auto-documentar/ })).toBeEnabled();
        test.info().annotations.push({ type: 'proveedor-ia', description: `HTTP ${res.status()}: ${await res.text()}` });
        return;
      }
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
