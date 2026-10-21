import { test, expect, type Page } from '@playwright/test';
import { ADMIN, ADMIN_STORAGE, apiAs, goToView, login, openApp } from './helpers';

test.use({ storageState: ADMIN_STORAGE });

const cards = (page: Page) => page.locator('main div.grid > div.cursor-pointer');

test.beforeEach(async ({ page }) => {
  await openApp(page);
  await goToView(page, 'knowledge');
  await expect(cards(page).first()).toBeVisible();
});

test('K-01/K-02 lista runbooks + auto-generados y el banner muestra el total', async ({ page }) => {
  const api = await apiAs(await login(ADMIN.email, ADMIN.password));
  const kb = await (await api.get('incidents/knowledge-base?size=50')).json();
  await api.dispose();
  expect(await cards(page).count()).toBe(Math.min(50, kb.total));
  await expect(page.getByText(`${kb.total} documentos indexados`, { exact: false })).toBeVisible();
  await expect(cards(page).filter({ hasText: 'Manual' }).first()).toBeVisible();
});

test('K-03 búsqueda', async ({ page }) => {
  await page.getByPlaceholder('Buscar en base de conocimiento...').fill('Nginx');
  await expect(page.getByText('Cargando base de conocimiento…')).toHaveCount(0);
  const n = await cards(page).count();
  expect(n).toBeGreaterThan(0);
  for (let i = 0; i < n; i++) await expect(cards(page).nth(i)).toContainText(/nginx/i);
});

test('K-04 filtro por categoría', async ({ page }) => {
  const chip = page.locator('main div.flex-wrap > button').filter({ hasNotText: 'Todos' }).first();
  const cat = (await chip.innerText()).trim();
  await chip.click();
  const n = await cards(page).count();
  expect(n).toBeGreaterThan(0);
  for (let i = 0; i < n; i++) await expect(cards(page).nth(i).locator('span').first()).toHaveText(cat);
});

test('K-05 abrir un documento renderiza el markdown', async ({ page }) => {
  await cards(page).first().click();
  const doc = page.locator('div.prose');
  await expect(doc).toBeVisible();
  await expect(doc.locator('h1, h2, h3').first()).toBeVisible();
  await expect(doc).not.toContainText(/^#\s/m);
});

test('K-06 "Nuevo Documento" abre un formulario', async ({ page }) => {
  const inputsBefore = await page.locator('input, textarea').count();
  await page.getByRole('button', { name: 'Nuevo Documento' }).click();
  await expect.poll(async () => page.locator('input, textarea').count(), { timeout: 3_000 })
    .toBeGreaterThan(inputsBefore);
});
