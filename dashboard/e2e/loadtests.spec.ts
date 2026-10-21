import { test, expect, type Page } from '@playwright/test';
import { ADMIN, ADMIN_STORAGE, OPERADOR_STORAGE, apiAs, goToView, login, openApp } from './helpers';

const scenarioCards = (page: Page) => page.locator('main div.cursor-pointer.rounded-xl');
const runButton = (page: Page) => page.getByRole('button', { name: /Ejecutar escenario|Ejecutando|Iniciando/ });

test.describe('Admin', () => {
  test.use({ storageState: ADMIN_STORAGE });

  test.beforeEach(async ({ page }) => {
    await openApp(page);
    await goToView(page, 'loadtests');
    await expect(scenarioCards(page)).toHaveCount(3);
  });

  test('L-01/L-02 cada escenario muestra su código', async ({ page }) => {
    const header = page.locator('main div.border-b').filter({ has: page.locator('svg.lucide-file-code2') });
    const seen = new Set<string>();
    for (let i = 0; i < 3; i++) {
      await scenarioCards(page).nth(i).click();
      const file = (await header.innerText()).trim();
      seen.add(file);
      await expect(page.locator('main pre code')).toContainText('export const options');
    }
    expect(seen.size).toBe(3);
  });

  test('L-03/L-04/L-05 @k6 ejecutar "normal", 409 concurrente y detener', async ({ page }) => {
    test.setTimeout(120_000);
    const api = await apiAs(await login(ADMIN.email, ADMIN.password));
    try {
      await scenarioCards(page).filter({ hasText: /normal/i }).first().click();
      await runButton(page).click();
      const runRow = page.locator('main div.bg-slate-950\\/40').filter({ hasText: 'En ejecución' }).first();
      await expect(runRow).toBeVisible({ timeout: 30_000 });
      await expect(runButton(page)).toBeDisabled();

      const second = await api.post('loadtests/runs', { data: { scenario: 'spike' } });
      expect(second.status()).toBe(409);

      await runRow.getByRole('button', { name: 'Detener' }).click();
      await expect(page.locator('main').getByText('Cancelada').first()).toBeVisible({ timeout: 30_000 });
      await expect(runButton(page)).toBeEnabled();
    } finally {
      const runs = (await (await api.get('loadtests/runs')).json()) as { id: string; status: string }[];
      for (const r of runs.filter((r) => r.status === 'running')) await api.post(`loadtests/runs/${r.id}/stop`);
      await api.dispose();
    }
  });

  test('L-06 historial con métricas de corridas terminadas', async ({ page }) => {
    const finished = page.locator('main div.bg-slate-950\\/40').filter({ hasText: /OK|Thresholds|Falló/ });
    test.skip((await finished.count()) === 0, 'sin corridas terminadas en el historial');
    await expect(finished.first().getByText('Requests')).toBeVisible();
    await finished.first().getByText('Ver salida de k6').click();
    await expect(finished.first().locator('pre')).toBeVisible();
  });
});

test.describe('Operador', () => {
  test.use({ storageState: OPERADOR_STORAGE });

  test('L-03 el Operador ve el código pero no puede ejecutar', async ({ page }) => {
    await openApp(page);
    await goToView(page, 'loadtests');
    await expect(scenarioCards(page)).toHaveCount(3);
    await expect(runButton(page)).toBeDisabled();
    await expect(page.getByText('Solo los administradores pueden disparar pruebas.')).toBeVisible();
  });
});
