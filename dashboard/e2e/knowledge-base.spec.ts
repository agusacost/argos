import { test, expect, type Page } from '@playwright/test';
import {
  ADMIN, ADMIN_STORAGE, E2E_PREFIX, OPERADOR_STORAGE, apiAs, cleanupE2eDocs, goToView, login, openApp,
} from './helpers';

const cards = (page: Page) => page.locator('main div.grid > div.cursor-pointer');
const drawer = (page: Page) => page.locator('div.fixed.right-0.max-w-xl');
const newDocButton = (page: Page) => page.getByRole('button', { name: 'Nuevo Documento' });
const form = (page: Page) => page.getByRole('dialog', { name: 'Nuevo documento' });

/** Tipo único con el prefijo de la suite: identifica (y permite limpiar) lo que crean las pruebas. */
const uniqueTipo = (tag: string) => `${E2E_PREFIX}doc-${tag}-${Date.now()}`;

test.describe('Admin', () => {
  test.use({ storageState: ADMIN_STORAGE });

  test.beforeEach(async ({ page }) => {
    await openApp(page);
    await goToView(page, 'knowledge');
    await expect(cards(page).first()).toBeVisible();
  });

  // Los documentos creados por las pruebas llevan el prefijo e2e-: se borran (o quedan identificados).
  test.afterEach(async () => {
    const api = await apiAs(await login(ADMIN.email, ADMIN.password));
    await cleanupE2eDocs(api);
    await api.dispose();
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

  test('K-05 abrir un documento muestra su contenido (markdown o, sin cuerpo, los pasos)', async ({ page }) => {
    await cards(page).first().click();
    const doc = drawer(page).locator('div.prose');
    await expect(doc).toBeVisible();
    await expect(doc.locator('h1, h2, h3').first()).toBeVisible();
    await expect(doc).not.toContainText(/^#\s/m);
  });

  test('K-05 un runbook manual sin Markdown muestra sus pasos como lista numerada', async ({ page }) => {
    const tipo = uniqueTipo('pasos');
    await newDocButton(page).click();
    await form(page).getByLabel('Tipo de alerta').fill(tipo);
    await form(page).getByLabel('Resumen').fill('Runbook e2e solo con pasos');
    await form(page).getByLabel('Pasos de resolución').fill('1. Revisar el estado del contenedor\n- Reiniciar el servicio\n\n');
    await form(page).getByRole('button', { name: 'Crear documento' }).click();

    const items = drawer(page).locator('div.prose ol li');
    await expect(items).toHaveCount(2);
    await expect(items.nth(0)).toHaveText('Revisar el estado del contenedor');
    await expect(items.nth(1)).toHaveText('Reiniciar el servicio');
  });

  test('K-06 el Administrador crea un documento: 201, lista actualizada y documento visible', async ({ page }) => {
    const tipo = uniqueTipo('md');
    await newDocButton(page).click();
    await expect(form(page)).toBeVisible();
    await expect(form(page).getByLabel('Tipo de alerta')).toBeFocused();

    await form(page).getByLabel('Tipo de alerta').fill(tipo);
    await form(page).getByLabel('Resumen').fill('Runbook e2e con documento Markdown');
    await form(page).getByLabel('Severidad').selectOption('critical');
    await form(page).getByLabel('Pasos de resolución').fill('Verificar el servicio\nReiniciar el servicio');
    await form(page).getByLabel('Documento (Markdown)').fill('## Diagnóstico e2e\n\nTexto del **runbook** de prueba.');

    const created = page.waitForResponse(
      (r) => r.url().endsWith('/api/incidents/knowledge-base') && r.request().method() === 'POST',
    );
    await form(page).getByRole('button', { name: 'Crear documento' }).click();
    const res = await created;
    expect(res.status(), await res.text()).toBe(201);
    const doc = (await res.json()) as { id: string };

    // El formulario se cierra y se abre el documento nuevo con el Markdown renderizado.
    await expect(form(page)).toHaveCount(0);
    await expect(drawer(page).getByRole('heading', { name: 'Diagnóstico e2e' })).toBeVisible();
    await expect(drawer(page).locator('div.prose strong')).toHaveText('runbook');
    await expect(drawer(page).getByText('Runbook documentado en la base de conocimiento')).toBeVisible();

    // La lista de la base de conocimiento incluye el documento nuevo y el API lo expone como documentado.
    await drawer(page).getByLabel('Cerrar panel').click();
    await expect(page.getByRole('status').filter({ hasText: tipo })).toBeVisible();
    await expect(cards(page).filter({ hasText: tipo })).toBeVisible();
    const api = await apiAs(await login(ADMIN.email, ADMIN.password));
    const inc = await (await api.get(`incidents/${doc.id}`)).json();
    await api.dispose();
    expect(inc.estado).toBe('documentado_previamente');
    expect(inc.documento).toContain('Diagnóstico e2e');
    expect(inc.pasos).toEqual(['Verificar el servicio', 'Reiniciar el servicio']);
  });

  test('K-06 el formulario valida los campos obligatorios sin llamar a la API', async ({ page }) => {
    let posts = 0;
    page.on('request', (r) => {
      if (r.method() === 'POST' && r.url().endsWith('/api/incidents/knowledge-base')) posts++;
    });
    await newDocButton(page).click();
    await form(page).getByRole('button', { name: 'Crear documento' }).click();
    await expect(form(page).getByText('El tipo de alerta es obligatorio', { exact: false })).toBeVisible();
    await expect(form(page).getByText('El resumen es obligatorio')).toBeVisible();
    await expect(form(page).getByText('Falta contenido', { exact: false })).toBeVisible();

    await form(page).getByLabel('Tipo de alerta').fill(uniqueTipo('val'));
    await form(page).getByLabel('Resumen').fill('Sin pasos ni documento');
    await form(page).getByRole('button', { name: 'Crear documento' }).click();
    await expect(form(page).getByText('Falta contenido', { exact: false })).toBeVisible();
    expect(posts).toBe(0);

    // Escape cierra el formulario y devuelve el foco al botón que lo abrió.
    await page.keyboard.press('Escape');
    await expect(form(page)).toHaveCount(0);
    await expect(newDocButton(page)).toBeFocused();
  });

  test('K-06 un 422 del backend se muestra en el formulario y conserva lo cargado', async ({ page }) => {
    await page.route('**/api/incidents/knowledge-base', (route) => {
      if (route.request().method() !== 'POST') return route.fallback();
      return route.fulfill({
        status: 422,
        contentType: 'application/json',
        body: JSON.stringify({ detail: 'Detalle simulado: falta el resumen.' }),
      });
    });
    const tipo = uniqueTipo('422');
    await newDocButton(page).click();
    await form(page).getByLabel('Tipo de alerta').fill(tipo);
    await form(page).getByLabel('Resumen').fill('Resumen e2e');
    await form(page).getByLabel('Pasos de resolución').fill('Un paso');
    await form(page).getByRole('button', { name: 'Crear documento' }).click();

    await expect(form(page).getByRole('alert')).toContainText('No se pudo crear el documento: Detalle simulado: falta el resumen');
    await expect(form(page)).toBeVisible();
    await expect(form(page).getByLabel('Tipo de alerta')).toHaveValue(tipo);
    await expect(form(page).getByRole('button', { name: 'Crear documento' })).toBeEnabled();
  });
});

test.describe('Operador', () => {
  test.use({ storageState: OPERADOR_STORAGE });

  test('K-07 el Operador ve la base de conocimiento pero no el botón "Nuevo Documento"', async ({ page }) => {
    await openApp(page);
    await goToView(page, 'knowledge');
    await expect(cards(page).first()).toBeVisible();
    await expect(newDocButton(page)).toHaveCount(0);
  });
});
