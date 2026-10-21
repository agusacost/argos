import { test, expect } from '@playwright/test';
import { ADMIN_STORAGE, goToView, openApp } from './helpers';

test.use({ storageState: ADMIN_STORAGE });

test.beforeEach(async ({ page }) => {
  await openApp(page);
  await goToView(page, 'metrics');
  await expect(page.getByText('MTTR Promedio')).toBeVisible();
});

test('M-01/M-02 tarjetas y gráfico diario', async ({ page }) => {
  await expect(page.getByRole('heading', { name: 'Incidentes Totales' })).toBeVisible();
  await expect(page.getByText('Incidentes por día — últimos 7 días')).toBeVisible();
  await expect(page.locator('main svg circle').first()).toBeVisible();
});

test('M-03 salud de servicios: 4 en healthy', async ({ page }) => {
  const card = page.locator('div.rounded-xl').filter({ hasText: 'Salud de Servicios' });
  await expect(card.getByText('Consultando Prometheus…')).toHaveCount(0);
  await expect(card.getByText('healthy', { exact: true })).toHaveCount(4);
});

test('M-04 golden signals Nginx en los 4 rangos', async ({ page }) => {
  const card = page.locator('div.rounded-xl').filter({ hasText: 'Señales de Nginx (Loki)' });
  for (const r of ['15m', '1h', '3h', '6h']) {
    const res = page.waitForResponse((x) => x.url().includes(`nginx-metrics?range=${r}`));
    await card.getByRole('button', { name: r, exact: true }).click();
    expect((await res).status()).toBe(200);
    await expect(card.getByText('Sin datos de Loki disponibles.'), `rango ${r}`).toHaveCount(0);
    await expect(card.locator('svg path[stroke]').first()).toBeVisible();
  }
  await expect(card.getByText('req/s').first()).toBeVisible();
});

test('M-05/M-06 top tipos de alerta y desglose por estado', async ({ page }) => {
  const card = page.locator('div.rounded-xl').filter({ hasText: 'Top tipos de alerta' });
  await expect(card.locator('div.space-y-3 > div')).toHaveCount(5);
  await expect(card.getByText('Desglose por estado')).toBeVisible();
  await expect(card.getByText('resuelto', { exact: true })).toBeVisible();
});
