import { test, expect } from '@playwright/test';
import { ADMIN, ADMIN_STORAGE, apiAs, login, openApp } from './helpers';

test.use({ storageState: ADMIN_STORAGE });

function kpi(page: import('@playwright/test').Page, label: string) {
  return page.locator('div.grid > div').filter({ hasText: label }).locator('p.text-2xl');
}

test('D-01 KPIs coinciden con /incidents/metrics', async ({ page }) => {
  const api = await apiAs(await login(ADMIN.email, ADMIN.password));
  const m = await (await api.get('incidents/metrics')).json();
  const nuevos = (await (await api.get('incidents?estado=nuevo&size=100')).json()).total;
  await api.dispose();

  await openApp(page);
  const mttr = m.mttr_average_minutes != null ? `${Math.round(m.mttr_average_minutes)} min` : '— min';
  await expect(kpi(page, 'MTTR promedio')).toHaveText(mttr);
  await expect(kpi(page, 'Auto-Documentados')).toHaveText(String(m.by_status.auto_documentado ?? 0));
  await expect(kpi(page, 'Pendientes de documentar')).toHaveText(String(nuevos));
  await expect(page.getByText(`${m.total_incidents} total`)).toBeVisible();
});

test('D-03/D-04 gráfico por día y resumen operacional', async ({ page }) => {
  await openApp(page);
  await expect(page.getByText('Incidentes por día')).toBeVisible();
  await expect(page.getByText('Sin datos')).toHaveCount(0);
  const resumen = page.locator('div').filter({ hasText: /^Resumen Operacional/ }).first();
  await expect(resumen).toBeVisible();
  for (const label of ['Críticos activos', 'En investigación', 'Resueltos', 'Documentados']) {
    await expect(resumen.getByText(label)).toBeVisible();
  }
});

test('D-05 actividad del sistema refleja la auditoría', async ({ page }) => {
  const api = await apiAs(await login(ADMIN.email, ADMIN.password));
  const activity = (await (await api.get('activity')).json()) as unknown[];
  await api.dispose();

  await openApp(page);
  const card = page.locator('div.rounded-xl').filter({ hasText: 'Actividad del Sistema' });
  if (activity.length === 0) {
    await expect(card.getByText('Sin actividad registrada aún.')).toBeVisible();
  } else {
    await expect(card.getByText('Sin actividad registrada aún.')).toHaveCount(0);
    await expect(card.locator('div.space-y-2\\.5 > div').first()).toBeVisible();
  }
});

test('D-06 incidentes recientes: hasta 8 filas y abre el detalle', async ({ page }) => {
  await openApp(page);
  const table = page.locator('div.rounded-xl').filter({ hasText: 'Incidentes Recientes' });
  const rows = table.locator('div.divide-y > button');
  await expect(rows.first()).toBeVisible();
  expect(await rows.count()).toBeLessThanOrEqual(8);
  await rows.first().click();
  await expect(page.getByText('Análisis de Argos')).toBeVisible();
});

test('S-06 campana: marcar una notificación como leída persiste', async ({ page }) => {
  await openApp(page);
  const bell = page.locator('header button:has(svg.lucide-bell)');
  const badge = bell.locator('span');
  test.skip((await badge.count()) === 0, 'no hay notificaciones sin leer');
  const before = await badge.innerText();
  test.skip(before === '9+', 'más de 9 sin leer: el contador no permite verificar el decremento');

  await bell.click();
  await expect(page.getByText('Notificaciones', { exact: true })).toBeVisible();
  const markRead = page.locator('header button[aria-label="Marcar como leída"]').first();
  const done = page.waitForResponse((r) => r.url().includes('/read') && r.request().method() === 'POST');
  await markRead.click();
  expect((await done).status()).toBe(200);
  const expected = String(Number(before) - 1);
  if (expected === '0') await expect(badge).toHaveCount(0);
  else await expect(badge).toHaveText(expected);

  await page.reload();
  await expect(page.locator('aside nav')).toBeVisible();
  if (expected === '0') await expect(badge).toHaveCount(0);
  else await expect(badge).toHaveText(expected);
});

test('S-07 polling: el dashboard recarga métricas según el intervalo', async ({ page }) => {
  await openApp(page);
  await page.waitForLoadState('networkidle');
  const res = await page.waitForResponse((r) => r.url().includes('/api/incidents/metrics'), { timeout: 30_000 });
  expect(res.status()).toBe(200);
});
