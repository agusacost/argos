import { test, expect, type Page } from '@playwright/test';
import { ADMIN, ADMIN_STORAGE, API_URL, goToView, openApp } from './helpers';

test.use({ storageState: ADMIN_STORAGE });

const userRow = (page: Page, email: string) =>
  page.locator('main div.grid-cols-12').filter({ hasText: email });
const modal = (page: Page) => page.locator('div.fixed.max-w-md');

async function loginStatus(page: Page, email: string, password: string) {
  const res = await page.request.post(`${API_URL}/auth/login`, { data: { email, password } });
  return { status: res.status(), body: res.ok() ? await res.json() : null };
}

test.beforeEach(async ({ page }) => {
  await openApp(page);
  await goToView(page, 'users');
  await expect(userRow(page, ADMIN.email)).toBeVisible();
});

test('U-01 el admin logueado no tiene botón de borrar', async ({ page }) => {
  await expect(userRow(page, ADMIN.email).locator('svg.lucide-trash2')).toHaveCount(0);
});

test('U-02 crear sin email/contraseña → error de validación', async ({ page }) => {
  await page.getByRole('button', { name: 'Nuevo Usuario' }).click();
  await modal(page).getByRole('button', { name: 'Guardar' }).click();
  await expect(modal(page).getByText('Email y contraseña son obligatorios')).toBeVisible();
});

test('U-02/U-03/U-04 ciclo completo: crear, editar, login y borrar', async ({ page }) => {
  const email = `e2e-ui-${Date.now()}@argos.local`;

  // Crear
  await page.getByRole('button', { name: 'Nuevo Usuario' }).click();
  await modal(page).getByPlaceholder('usuario@empresa.com').fill(email);
  await modal(page).getByPlaceholder('Nombre Apellido').fill('E2E Usuario');
  await modal(page).locator('select').selectOption('Operador');
  await modal(page).getByPlaceholder('••••••••').fill('Clave-Inicial-1');
  await modal(page).getByRole('button', { name: 'Guardar' }).click();
  await expect(modal(page)).toHaveCount(0);
  await expect(userRow(page, email)).toContainText('Operador');
  expect((await loginStatus(page, email, 'Clave-Inicial-1')).status).toBe(200);

  // Email duplicado
  await page.getByRole('button', { name: 'Nuevo Usuario' }).click();
  await modal(page).getByPlaceholder('usuario@empresa.com').fill(email);
  await modal(page).getByPlaceholder('••••••••').fill('otra-clave-123');
  await modal(page).getByRole('button', { name: 'Guardar' }).click();
  await expect(modal(page).locator('.text-red-400')).toBeVisible();
  await modal(page).getByRole('button', { name: 'Cancelar' }).click();

  // Editar nombre, rol y contraseña
  await userRow(page, email).locator('button:has(svg.lucide-edit3, svg.lucide-pen-line)').click();
  await modal(page).getByPlaceholder('Nombre Apellido').fill('E2E Editado');
  await modal(page).locator('select').selectOption('Admin');
  await modal(page).getByPlaceholder('••••••••').fill('Clave-Nueva-2');
  await modal(page).getByRole('button', { name: 'Guardar' }).click();
  await expect(modal(page)).toHaveCount(0);
  await expect(userRow(page, email)).toContainText('E2E Editado');
  await expect(userRow(page, email)).toContainText('Admin');
  expect((await loginStatus(page, email, 'Clave-Inicial-1')).status).toBe(401);
  const relogin = await loginStatus(page, email, 'Clave-Nueva-2');
  expect(relogin.status).toBe(200);
  expect(relogin.body.user.role).toBe('Admin');

  // Borrar
  page.once('dialog', (d) => d.accept());
  await userRow(page, email).locator('button:has(svg.lucide-trash2)').click();
  await expect(userRow(page, email)).toHaveCount(0);
  expect((await loginStatus(page, email, 'Clave-Nueva-2')).status).toBe(401);
});
