import { test, expect, type Page } from '@playwright/test';
import { ADMIN, ADMIN_STORAGE, OPERADOR_STORAGE, apiAs, goToView, login, openApp, readRunState } from './helpers';

const saveButton = (page: Page) => page.getByRole('button', { name: 'Guardar configuración' });
const card = (page: Page, title: string) => page.locator('main div.rounded-xl').filter({ hasText: title });

async function getSettings() {
  const api = await apiAs(await login(ADMIN.email, ADMIN.password));
  const s = await (await api.get('settings')).json();
  await api.dispose();
  return s;
}

async function restoreSettings() {
  const api = await apiAs(await login(ADMIN.email, ADMIN.password));
  await api.patch('settings', { data: readRunState().settings });
  await api.dispose();
}

test.describe('Admin', () => {
  test.use({ storageState: ADMIN_STORAGE });

  test.beforeEach(async ({ page }) => {
    await openApp(page);
    await goToView(page, 'settings');
    await expect(saveButton(page)).toBeVisible();
  });

  test.afterEach(restoreSettings);

  test('C-02/C-03/C-06/C-07 guardar umbral, auto-doc y polling persiste', async ({ page }) => {
    const before = await getSettings();
    await card(page, 'Base de Conocimiento (RAG)').locator('input[type=range]').fill('0.7');
    await card(page, 'Base de Conocimiento (RAG)').locator('button').click(); // toggle auto-doc
    await card(page, 'Refresco automático').locator('input[type=range]').fill('30');
    await saveButton(page).click();
    await expect(page.getByText('Guardado correctamente')).toBeVisible();

    const after = await getSettings();
    expect(after.similarity_threshold).toBeCloseTo(0.7, 2);
    expect(after.auto_doc_enabled).toBe(!before.auto_doc_enabled);
    expect(after.polling_interval_seconds).toBe(30);

    await page.reload();
    await goToView(page, 'settings');
    await expect(page.getByText('Umbral de similitud — 0.70')).toBeVisible();
    await expect(page.getByText('Intervalo — 30s')).toBeVisible();
  });

  test('K-02 el banner de la Base de Conocimiento refleja el umbral configurado', async ({ page }) => {
    await card(page, 'Base de Conocimiento (RAG)').locator('input[type=range]').fill('0.75');
    await saveButton(page).click();
    await expect(page.getByText('Guardado correctamente')).toBeVisible();
    await goToView(page, 'knowledge');
    await expect(page.getByText('Umbral sim.').locator('..')).toContainText('0.75');
  });

  test('C-01 modelo Gemini inexistente → 422 visible y no se persiste', async ({ page }) => {
    const before = await getSettings();
    await card(page, 'Modelo de IA').locator('input[type=text]').fill('gemini-modelo-inexistente-e2e');
    const res = page.waitForResponse((r) => r.url().endsWith('/api/settings') && r.request().method() === 'PATCH');
    await saveButton(page).click();
    expect((await res).status()).toBe(422);
    await expect(page.getByText(/no existe en Gemini/)).toBeVisible();
    expect((await getSettings()).llm_model).toBe(before.llm_model);
  });

  test('C-01 modelo que no corresponde al proveedor → error o aviso', async ({ page }) => {
    await card(page, 'Modelo de IA').locator('input[type=text]').fill('modelo-sin-prefijo-e2e');
    const res = page.waitForResponse((r) => r.url().endsWith('/api/settings') && r.request().method() === 'PATCH');
    await saveButton(page).click();
    const r = await res;
    const body = r.ok() ? await r.json() : null;
    // Se acepta 422 o un 200 con `warning`; un 200 silencioso deja guardado un modelo que nunca se usa.
    expect(r.status() === 422 || Boolean(body?.warning), 'se guardó sin feedback').toBe(true);
  });

  test('C-04 pool de destinatarios: validaciones, agregar y quitar', async ({ page }) => {
    const pool = card(page, 'Notificaciones por email');
    const input = pool.getByPlaceholder('oncall@empresa.com');
    const initial = (await getSettings()).alert_recipients.length;
    await expect(pool.getByText(`${initial}/50`)).toBeVisible();

    await input.fill('no-es-un-email');
    await pool.getByRole('button', { name: 'Agregar' }).click();
    await expect(pool.getByText('Email inválido')).toBeVisible();

    await input.fill('E2E-Pool@Example.com');
    await input.press('Enter');
    await expect(pool.getByText('e2e-pool@example.com')).toBeVisible();
    await expect(pool.getByText(`${initial + 1}/50`)).toBeVisible();

    await input.fill('e2e-pool@example.com');
    await pool.getByRole('button', { name: 'Agregar' }).click();
    await expect(pool.getByText('Ya está en la lista')).toBeVisible();

    await saveButton(page).click();
    await expect(page.getByText('Guardado correctamente')).toBeVisible();
    expect((await getSettings()).alert_recipients).toContain('e2e-pool@example.com');

    await pool.getByLabel('Quitar e2e-pool@example.com').click();
    await expect(pool.getByText('e2e-pool@example.com')).toHaveCount(0);
    await saveButton(page).click();
    await expect(page.getByText('Guardado correctamente')).toBeVisible();
    expect((await getSettings()).alert_recipients).not.toContain('e2e-pool@example.com');
  });

  test('C-05 integraciones en vivo: todas up', async ({ page }) => {
    const integ = card(page, 'Integraciones');
    await expect(integ.getByText('Verificando…')).toHaveCount(0);
    const items = integ.locator('div.grid > div');
    expect(await items.count()).toBeGreaterThan(0);
    await expect(integ.locator('.bg-red-500')).toHaveCount(0);
    const res = page.waitForResponse((r) => r.url().includes('/integrations/status'));
    await integ.getByLabel('Refrescar estado').click();
    expect((await res).status()).toBe(200);
  });

  test('S-07 el polling configurado se aplica (10 s)', async ({ page }) => {
    test.setTimeout(60_000);
    await card(page, 'Refresco automático').locator('input[type=range]').fill('10');
    await saveButton(page).click();
    await expect(page.getByText('Guardado correctamente')).toBeVisible();
    const hits: number[] = [];
    page.on('request', (r) => { if (r.url().includes('/api/incidents/metrics')) hits.push(Date.now()); });
    await page.waitForTimeout(23_000);
    expect(hits.length).toBeGreaterThanOrEqual(2);
  });
});

test.describe('Operador', () => {
  test.use({ storageState: OPERADOR_STORAGE });

  test('C-07 el Operador ve la configuración en solo lectura', async ({ page }) => {
    await openApp(page);
    await goToView(page, 'settings');
    await expect(page.getByText('Modelo de IA')).toBeVisible();
    await expect(saveButton(page)).toHaveCount(0);
    await expect(page.locator('main select')).toBeDisabled();
    for (const r of await page.locator('main input[type=range]').all()) await expect(r).toBeDisabled();
    await expect(page.getByPlaceholder('oncall@empresa.com')).toHaveCount(0);
  });
});
