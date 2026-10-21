# Plan — Test de integración E2E del dashboard Argos

**Fecha:** 2026-10-21
**Objetivo:** verificar automáticamente que las 7 vistas del dashboard funcionan contra el stack real
(dashboard → nginx `/api` → Argos API → Qdrant / Loki / Prometheus / LLM / Docker k6), y reemplazar la
checklist manual de `docs/VALIDACION_API.md` §2 por una suite repetible.

---

## 1. Enfoque

| Decisión | Elección | Por qué |
|---|---|---|
| Herramienta | **Playwright** (`@playwright/test`, TypeScript) | Mismo lenguaje que el dashboard; navegador real; trazas/video al fallar |
| Ubicación | `dashboard/e2e/` + `dashboard/playwright.config.ts` | Junto al código que prueba; no entra al build de Vite |
| Target | Stack levantado con `docker compose up -d` → `http://localhost:3001` | Integración real: sin mocks de la API |
| Capas | (A) smoke de API vía `request` de Playwright · (B) E2E de UI | Si falla (A), (B) no corre: aísla si el problema es backend o UI |
| Navegación | Click en el sidebar (las vistas son estado de React, **no hay rutas URL**) | `App.tsx` usa `activeView`; no se puede hacer `goto('/incidents')` |
| Auth | `globalSetup` loguea Admin y Operador vía `POST /api/auth/login` y guarda `storageState` con `argos_token` en localStorage | Evita repetir el login en cada test |

Scripts nuevos en `dashboard/package.json`:
- `test:e2e` → `playwright test` (suite rápida, sin efectos externos)
- `test:e2e:full` → `playwright test --grep-invert @never` (incluye `@llm` y `@k6`)

---

## 2. Precondiciones (`e2e/global-setup.ts`)

1. Stack arriba y sano: `GET :3001/` 200, `GET :3001/api/incidents/metrics` (con token) 200,
   `GET :6333/readyz` 200. Si falla → abortar con mensaje claro (no es un fallo de test).
2. Datos semilla presentes: KB ≥ 5 runbooks, incidentes ≥ 15 (auto-seed de `SeedService`).
3. **Snapshot** de `GET /api/settings` → `e2e/.state/settings.json` para restaurarlo en `globalTeardown`.
4. Crear usuario Operador temporal `e2e-operador-<timestamp>@argos.local` (se borra en teardown).
5. Opcional (`E2E_SNAPSHOT=1`): snapshot de colecciones Qdrant como punto de restauración.

Variables: `E2E_BASE_URL` (default `http://localhost:3001`), `E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD`
(default `admin@argos.local` / `argos-admin-2026`).

---

## 3. Capa A — Smoke de API (`e2e/api.smoke.spec.ts`)

Todas vía `:3001/api` (valida también el proxy nginx del dashboard).

| Endpoint | Esperado |
|---|---|
| `POST /auth/login` ok / password errónea | 200 + token / 401 |
| `GET /auth/me` sin token | 401 |
| `GET /incidents?page=1&size=10`, `/incidents/metrics`, `/incidents/knowledge-base` | 200, shape `Paginated` / `Metrics` |
| `GET /incidents/{id}`, `/{id}/observability`, `/{id}/audit` | 200 |
| `GET /observability/services` | 4 servicios, todos `healthy` |
| `GET /observability/nginx-metrics?range=15m\|1h\|3h\|6h` | 200, `timestamps.length > 0` |
| `GET /notifications`, `/activity`, `/integrations/status` | 200 |
| `GET /loadtests/scenarios` | 3 escenarios con `code` no vacío |
| `GET /users`, `PATCH /settings`, `POST /loadtests/runs` con token Operador | **403** |

---

## 4. Capa B — E2E de UI (un spec por vista)

### `auth.spec.ts`
- Login Admin con credenciales válidas → sidebar visible con las 7 entradas.
- Credenciales inválidas → mensaje de error, sin token en localStorage.
- F5 mantiene la sesión; logout limpia `argos_token` y vuelve al login.
- Login Operador → **no** ve "Gestión de Usuarios".
- Token inválido/expirado en localStorage → vuelve al login (no queda la UI rota).

### `dashboard.spec.ts`
- KPIs con valores numéricos (no `NaN`/vacío), coherentes con `GET /incidents/metrics`.
- Actividad reciente renderiza filas.
- Campana: contador > 0 → abrir → marcar una como leída → contador baja en 1.

### `incidents.spec.ts`
- Tabla con filas; paginado siguiente/anterior cambia filas.
- Filtro por estado y por tipo → todas las filas visibles cumplen el filtro.
- Búsqueda por texto de un título conocido del seed → aparece.
- Abrir detalle → logs y sparkline renderizan (o estado vacío explícito, sin error).
- Cambiar estado → aparece en auditoría del incidente y en actividad reciente.
  **Restaurar** el estado original al final (`PATCH /incidents/{id}`).
- `@llm` "Generar documentación" sobre un incidente sin doc → documento markdown visible, MTTR calculado,
  entrada de auditoría. Timeout 120 s (Gemini thinking).

### `knowledge-base.spec.ts`
- Lista ≥ 5 runbooks + docs `auto_documentado`.
- Abrir uno → el markdown renderiza (headings/listas), sin texto crudo `#`.
- Búsqueda filtra resultados.

### `metrics.spec.ts`
- "Salud de servicios": 4 tarjetas en `healthy`.
- Golden signals Nginx: para cada rango 15m/1h/3h/6h el gráfico se redibuja sin error.
  Precondición de datos: el setup genera tráfico (`GET http://localhost/api/status` × 50) para que
  haya series en Loki.

### `loadtests.spec.ts`
- Ver el código de los 3 escenarios (syntax highlighter con contenido).
- Operador: botón "Ejecutar" deshabilitado.
- `@k6` Admin: correr `normal` → estado `running` → **Detener** → `cancelled`; segunda corrida
  simultánea vía API → 409. **No** se corre `attack` (dispara alertas reales → emails al pool).

### `users.spec.ts`
- Crear usuario `e2e-*` → aparece en la tabla.
- Editar rol y contraseña → login con el nuevo usuario en un contexto aislado funciona.
- Borrar → desaparece; login con él → 401.

### `settings.spec.ts`
- Operador: controles deshabilitados, sin botón Guardar.
- Admin: cambiar umbral, auto-doc, polling → guardar → recargar → persisten.
- Modelo LLM inexistente → error 422 visible, no se persiste.
- Pool de emails: agregar `e2e@example.com`, email inválido → rechazado, contador N/50, quitar.
- Integraciones en vivo: cada fila muestra `up`/`down` + latencia.
- Teardown restaura settings desde el snapshot (pool incluido).

---

## 5. Aislamiento y efectos secundarios

| Riesgo | Mitigación |
|---|---|
| Mutar datos demo (estado de incidentes, notificaciones leídas) | Restaurar el estado en `afterEach`; notificaciones: aceptado (no críticas) |
| Romper la config real (pool de emails, modelo LLM) | Snapshot + restore de `/settings` en global setup/teardown |
| Costo/latencia del LLM | Tests `@llm` fuera de la suite por defecto |
| Generar carga/alertas/emails | Solo `normal` y detenido enseguida; `attack` excluido |
| Usuarios basura | Prefijo `e2e-`; teardown borra todo `e2e-*` aunque el test falle |
| Paralelismo | `workers: 1` (estado compartido en Qdrant, una sola corrida k6 a la vez) |

---

## 6. Ejecución y reportes

```powershell
docker compose up -d --build
cd dashboard
npm i -D @playwright/test && npx playwright install chromium
npm run test:e2e           # rápida (~2-3 min)
npm run test:e2e:full      # + LLM + k6 (~6-8 min)
npx playwright show-report
```

Config: `retries: 1`, `trace: 'on-first-retry'`, `video: 'retain-on-failure'`, `screenshot: 'only-on-failure'`,
reporter `html` + `list`. Resultados → `docs/VALIDACION_API.md` §2 (marcar ✅/❌ por área).

---

## 7. Orden de implementación

1. Instalar Playwright, `playwright.config.ts`, `global-setup.ts` / `global-teardown.ts`, helpers (`login`, `goToView`, `apiClient`).
2. `api.smoke.spec.ts` (capa A).
3. `auth` → `dashboard` → `incidents` → `knowledge-base` → `metrics`.
4. `users` → `settings` (los que mutan config; con restore).
5. `loadtests` y tests `@llm`.
6. Agregar `data-testid` en componentes donde el texto sea ambiguo (KPIs, contador de la campana, filas de tabla, estado de corrida).
7. Correr la suite completa, volcar resultados en `docs/VALIDACION_API.md`.

## 8. Fuera de alcance
- Email recibido en el pool (se valida a nivel n8n: `recipients_source=pool`; no automatizable sin acceso al buzón).
- Flujo Alertmanager → n8n con `attack` (ya validado en `k6/RESULTS.md`).
- CI (no hay pipeline aún; queda para el deploy cloud del Sprint 6).
