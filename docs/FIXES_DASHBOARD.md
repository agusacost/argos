# Fixes pendientes del dashboard Argos

**Fecha:** 2026-10-21 · **Origen:** suite E2E `dashboard/e2e/` (Playwright, stack real) + revisión del código.
**IDs:** los de [`FUNCIONALIDADES_DASHBOARD.md`](FUNCIONALIDADES_DASHBOARD.md).

## Resultado de la corrida

`npm run test:e2e:full` (API + UI + LLM + k6): **69 tests → 55 OK, 13 fallan, 1 omitido** (S-06: hay más de 9
notificaciones sin leer y el contador muestra "9+", así que no se puede verificar el decremento).

Funciona de punta a punta: login/logout/sesión, RBAC (Admin vs Operador, en UI y en API), KPIs, gráficos,
actividad, listado/paginado/búsqueda/filtro por estado de incidentes, detalle con logs de Loki y auditoría,
resolver incidente (estado + MTTR + auditoría), auto-documentar con Gemini (pasó en una corrida; en la siguiente
Gemini devolvió 503), Base de Conocimiento (listado, búsqueda, categorías), salud de servicios, golden signals
de Nginx en los 4 rangos, k6 (ver código, ejecutar, 409 concurrente, detener, historial), ABM de usuarios,
configuración (umbral, auto-doc, polling efectivo, pool de emails, modelo Gemini inválido → 422, integraciones
en vivo).

Prioridad: **Alta** = seguridad o pérdida de datos · **Media** = la funcionalidad no hace lo que promete ·
**Baja** = cosmético / deuda.

### Nuevo escenario k6: `flood` (HTTP flood / DDoS L7) · 2026-10-25

Cuarto escenario on-demand (`k6/tests/flood-traffic.js`, registrado en `LoadTestScenarioCatalog`): volumen
sostenido de GETs legítimos (sin paths/headers maliciosos) a `/api/usuarios`, muy por encima del rate limit de
nginx (20→80→20 req/s vs. 30r/s por IP). Valida que `limit_req`/`limit_conn` corten con **429** (no 503) antes
de llegar a la API/SQL Server. **Validado E2E:** 8099 requests, 3027 cortados con 429 (37%), 0 errores 5xx,
p95 1.86ms sobre lo que sí pasó — el backend nunca se degrada, el rate limit hace su trabajo.

**Gaps de detección pendientes de definir con el equipo** (no implementados, solo documentados):
- **Escaneo de vulnerabilidades:** hoy no hay una alerta específica para un barrido de paths/puertos; se
  solapa parcialmente con `NginxSuspiciousIPActivity` (umbral por volumen), pero un escaneo lento/distribuido
  no lo dispara.
- **Inyección (SQLi/XSS/path traversal):** no hay detección por contenido del request (WAF/firma); solo se
  vería si el intento produce 4xx/5xx en volumen suficiente para las alertas existentes. Agregar esto requeriría
  un WAF (ModSecurity/Coraza) delante de nginx o reglas Loki por patrón en el access log.

### Revisión de legibilidad de los dashboards (vista Métricas) · 2026-10-25

Revisión de qué tan fácil es interpretar la información real vs. decorativa en `DashboardView` y `MetricsView`.
Dos hallazgos ya estaban anotados abajo (**F-11**, **F-12**: valores fijos como "Uptime IA: 99.99%" y
sparklines planas que simulan una tendencia que no existe) — siguen pendientes, no se tocaron en esta pasada.

Se implementaron las dos mejoras de mayor impacto sobre `NginxSignals.tsx` (vista Métricas → golden signals de
Nginx), ambas **validadas E2E** con el escenario `flood` de k6 corriendo en vivo:

1. **Serie `429` separada del resto de 4xx.** Antes, un rate-limit cortando tráfico (nuestro propio escenario
   `flood`) quedaba invisible: sumaba al balde genérico "4xx" junto con 404/403/400 y no se distinguía de un
   error de cliente normal. Ahora `GET /observability/nginx-metrics` devuelve `status_series["429"]` como
   serie propia (query Loki `status=~"429"` separada de `status=~"4.."`), graficada en el dashboard con su
   propio color ("429 (rate limit)"). Cambios: `NginxStatusSeries.Status429` (`Argos.Entities`),
   `NginxMetricsService.GetAsync` (`Argos.Business`), tipo `NginxMetrics` en `dashboard/src/api.ts`.
   **Validado:** con el escenario `flood` corriendo, el access log de nginx mostró 96/300 líneas con
   `status=429` en la ventana reciente y la API devolvió ese mismo volumen en `status_series["429"]`
   (`0.175 req/s`, consistente con el ritmo real).
2. **Tooltip con valor exacto al pasar el mouse** en los gráficos de líneas de `NginxSignals` (status codes y
   latencia). Antes solo se podía leer el máximo del eje y las gridlines; ahora, al mover el mouse sobre el
   gráfico aparece una línea guía vertical + un recuadro con la hora exacta y el valor de cada serie en ese
   punto (latencia se muestra en ms, tasas en el valor crudo, porcentajes con `%`).

**Pendiente de esta misma revisión (no implementado, para una próxima pasada):**
- Marcadores de alertas disparadas superpuestos en el gráfico de golden signals (correlacionar visualmente
  "acá subió la latencia" con "acá se abrió el incidente", sin cruzar con la tabla).
- Serie temporal de request rate total (hoy `NginxSignals` solo muestra el valor instantáneo "actual", no su
  evolución, a diferencia de status codes y latencia que sí son series).
- F-11/F-12 (valores decorativos y sparklines planas) siguen sin resolver — ver más abajo.

---

## Alta

### F-01 · Endpoints de incidentes y actividad sin autenticación — I-01, I-06, D-05 · ✅ CORREGIDO (2026-10-21)
- **Evidencia original:** `GET /incidents`, `PATCH /incidents/{id}` y `GET /activity` sin token → 200; el PATCH
  anónimo no quedaba auditado.
- **Fix aplicado:** `[Authorize]` a nivel de clase en `IncidentsController`, `ObservabilityController` y
  `NotificationsController` (incluye `/activity`). `IIncidentService.UpdateAsync` recibe ahora un `CurrentUser`
  obligatorio y **siempre** audita el cambio.
- **Verificado:** `api.smoke.spec.ts` → todos los endpoints de lectura/escritura sin token dan 401; la suite de UI
  sigue pasando (el dashboard ya mandaba el JWT en todas las llamadas).

### F-13 · Endpoints internos de n8n expuestos sin token · ✅ CORREGIDO (2026-10-21)
- **Evidencia original:** `POST /query` y `POST /upsert` no tenían protección y `ARGOS_INTERNAL_TOKEN` estaba vacío, así
  que `/internal/config` y `/notifications/recipients` tampoco estaban protegidos. Se podían llamar desde `:3001/api/...` y desde
  `:8000`. Con `/upsert` cualquiera podía **escribir incidentes falsos en la KB** que después el RAG le pasa al LLM.
- **Fix aplicado:**
  1. `RagController` con `[RequireInternalToken]` (igual que `InternalController`).
  2. Workflow n8n: los nodos `RAG Query` y `Persist Incident` ahora mandan `X-Argos-Token`, igual que `Get Config` y `Get Recipients`.
  3. `dashboard/nginx.conf`: `/api/query`, `/api/upsert`, `/api/internal/*` y `/api/notifications/recipients` → 404.
  4. `ARGOS_INTERNAL_TOKEN` definido en `docker/monitoring/.env` (aleatorio, 32 bytes); `argos-api` y `n8n` recreados;
     workflow re-importado y reactivado (credencial SMTP intacta).
  5. Puerto de `argos-api` publicado solo en loopback (`127.0.0.1:8000:8000`).
- **Verificado:** sin token los 4 endpoints responden 401 (desde n8n y desde el host); con token, 200. Por el proxy
  del dashboard, 404. Alerta simulada `NginxHighErrorRate` (ejecución n8n #52) → todos los nodos `success`,
  `recipients_source=pool`, email enviado.

---

## Media

### F-02 · "Marcar como Resuelto" no refresca la lista de Incidentes — I-06
- **Evidencia:** `incidents.spec.ts` I-06: con el filtro "pendiente", después de resolver la fila sigue en la
  lista con el estado viejo (la API sí quedó en `resuelto`).
- **Causa:** `App.resolveIncident` / `documentIncident` llaman a `loadData()` del contexto (Dashboard), pero
  `IncidentsView` tiene sus propias `rows` locales y no se entera.
- **Fix sugerido:** exponer un contador `incidentsVersion` en el contexto que se incremente tras cada mutación y
  agregarlo a las dependencias del `useEffect` de `IncidentsView` (y de `KnowledgeBaseView`).

### F-03 · Errores de "Aprobar y Auto-documentar" / "Marcar como Resuelto" no se muestran — I-06, I-07
- **Evidencia:** I-07 en la 2.ª corrida: Gemini respondió `503 UNAVAILABLE (high demand)` → API 502. En la UI el
  panel se cierra al hacer click y **no aparece ningún error**; el incidente sigue en `nuevo` sin que el usuario
  sepa por qué.
- **Causa:** `App.tsx` cierra el drawer (`setSelectedIncidentId(null)`) **antes** de esperar la llamada, y el
  drawer invoca `void documentIncident(...)` → la promesa rechazada no la captura nadie.
- **Fix sugerido:** esperar la respuesta con el panel abierto (spinner ya existe), mostrar el `detail` del error
  y cerrar solo si salió bien. Opcional en la API: 1-2 reintentos con backoff ante 503/429 del proveedor LLM.

### F-04 · Los runbooks manuales no muestran su documentación — K-05
- **Evidencia:** `knowledge-base.spec.ts` K-05: al abrir un runbook no se muestra ningún markdown. Los 5
  `documentado_previamente` tienen `documento` vacío; los `auto_documentado` sí lo tienen.
- **Causa:** `Argos.Repository/Knowledge/MarkdownKnowledgeSource.cs` solo lee el frontmatter YAML
  (`tipo_alerta`, `resumen`, `pasos`…) y **descarta el cuerpo** del `.md` (secciones Contexto, Diagnóstico, etc.).
- **Fix sugerido:** asignar el cuerpo después del frontmatter a `Documento`. Como el seed es idempotente y no
  pisa registros existentes, hay que re-seedear los 5 runbooks (o hacer que el seed actualice si `documento` está vacío).

### F-05 · Filtro "por tipo" exige el nombre exacto — I-04
- **Evidencia:** `incidents.spec.ts` I-04: escribir `Nginx` → 0 resultados (`GET /incidents?tipo=Nginx` → `total: 0`);
  `tipo=NginxDown` sí funciona.
- **Causa:** la API filtra `tipo_alerta` por igualdad en Qdrant, pero el input es texto libre ("Filtrar por tipo…")
  y se dispara en cada tecla.
- **Fix sugerido (elegir uno):** (a) en la UI, reemplazar el input por un `<select>` con los tipos de
  `metrics.by_alert_type`; o (b) en la API, filtro por substring case-insensitive igual que `search`.
  La opción (a) es más simple y no toca la API.

### F-06 · Botón "Nuevo Documento" sin funcionalidad — K-06
- **Evidencia:** `knowledge-base.spec.ts` K-06: el click no hace nada.
- **Dónde:** `KnowledgeBaseView.tsx`: el `<button>` no tiene `onClick`.
- **Fix sugerido:** quitarlo, o implementarlo: modal (tipo de alerta, resumen, pasos, markdown) →
  `POST /upsert` con `estado=documentado_previamente`, `fuente=manual`.

### F-07 · El umbral de la Base de Conocimiento está fijo en 0.63 — K-02
- **Evidencia:** `settings.spec.ts` K-02: se guarda 0.75 en Configuración y el banner sigue mostrando `0.63`.
- **Dónde:** `KnowledgeBaseView.tsx` (literal `0.63`).
- **Fix sugerido:** leer `similarity_threshold` con `getSettings()` (todos los roles pueden leerlo).

### F-08 · El Operador podía generar documentación por API aunque la UI se lo ocultara — I-07 · ✅ CORREGIDO (2026-10-21)
- **Fix aplicado:** `POST /incidents/{id}/document` → `[Authorize(Roles = UserRoles.Admin)]`, igual que el botón en la UI.
  Si se decide que el Operador también apruebe, hay que cambiar ambos lados (API y `canApprove` en `IncidentTable.tsx`).
- **Verificado:** `api.smoke.spec.ts` → Operador 403 (sale de la autorización, sin llamar al LLM).

---

## Baja

### F-09 · El header no tiene título para "Pruebas de Carga" — S-05
- **Evidencia:** `auth.spec.ts` S-05: el `<h1>` muestra `Argos`.
- **Fix:** agregar `loadtests: 'Pruebas de Carga'` al mapa `titles` de `Header.tsx`.

### F-10 · Modelo LLM de otro proveedor se guarda sin aviso — C-01
- **Evidencia:** `settings.spec.ts` C-01 (2.º): con proveedor Gemini, `modelo-sin-prefijo-e2e` → **200** sin
  `warning`. En la configuración queda guardado un modelo que nunca se usa (`LlmService.Resolve` usa el default
  si no empieza con el prefijo del proveedor). Un `gemini-*` inexistente sí da 422 ✅.
- **Fix sugerido:** en `SettingsService.UpdateAsync`, si el modelo no tiene el prefijo del proveedor → 422 o
  `warning` ("se usará <default>").

### F-11 · Elementos decorativos / datos fijos
- **Header:** el buscador "Buscar incidentes..." no hace nada; los badges "Sistema Operativo" e "IA Activa" son
  fijos (podrían usar `/integrations/status`).
- **Dashboard:** "Uptime IA: 99.99%" y la barra de progreso del Resumen (`width: 99.94%`) son constantes; los
  sparklines de MTTR y Auto-documentados son líneas planas (`Array(8).fill(valor)`).
- **Fix sugerido:** conectar el buscador a la vista Incidentes (`setActiveView('incidents')` + search) o quitarlo;
  quitar o calcular los valores fijos.

### F-12 · Detalles menores de UX
- **Actividad del sistema** (Dashboard) se carga una sola vez; no entra en el polling.
- **Búsqueda / filtro por tipo** disparan una request por tecla (sin debounce).
- **Panel de un runbook manual** muestra "Auto-documentado en base de conocimiento" (el estado
  `documentado_previamente` también se mapea a `documented`).
- **Categorías de la KB** se calculan solo con la página actual.
- **Notificaciones leídas** son globales (no por usuario): si un usuario marca una como leída, deja de figurar
  como no leída para todos.
- **`MetricsView`** llama a `useState`/`useEffect` después de un `return` condicional (viola las reglas de hooks).
  Hoy no rompe (M-01 pasa porque el primer render no tiene hooks), pero es frágil: mover los hooks arriba del `if`.

---

## Cómo re-verificar

```powershell
cd dashboard
npm run test:e2e        # sin LLM ni k6 (~4 min)
npm run test:e2e:full   # incluye @llm (Gemini) y @k6 (corrida normal + stop)
npx playwright show-report
```

Efectos de la suite: crea/borra usuarios `e2e-*`, restaura los settings al terminar, devuelve el incidente
resuelto a `pendiente`. Marcar notificaciones como leídas no se revierte. Con `@llm`, el incidente documentado
vuelve a `nuevo` pero conserva el documento generado y la entrada de auditoría.
