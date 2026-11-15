# Fixes pendientes del dashboard Argos

**Fecha:** 2026-10-21 · **Origen:** suite E2E `dashboard/e2e/` (Playwright, stack real) + revisión del código.
**IDs:** los de [`FUNCIONALIDADES_DASHBOARD.md`](FUNCIONALIDADES_DASHBOARD.md).

**Estado al 2026-11-15:** F-02, F-03, F-04, F-05, F-06 y F-10 están **implementados y confirmados** por la suite E2E
completa, que se volvió a correr el 15/11 contra el stack real: **103 pruebas, 101 aprobadas, 0 fallidas, 2 omitidas**
(S-06 y I-07 se omiten solas cuando no hay notificaciones sin leer ni incidentes en estado `nuevo`; I-07, la que llama al
LLM, queda por lo tanto sin confirmar). I-04, K-06 y C-01, que fallaban desde la migración a Next.js, ahora pasan. La
corrida encontró dos cosas: (1) **M-04, defecto real del panel**: la consulta de latencia P50/P95/P99 no agrupaba y Loki
devolvía una serie por combinación de etiquetas; el panel tomaba la primera (la latencia mostrada no era la del conjunto) y
en el rango de 6 h Loki respondía 400 por superar las 500 series. Corregido con `by (service)` en `NginxMetricsService`
(+4 pruebas unitarias, 106 en total). (2) Una prueba de C-01 buscaba cualquier `role=alert` y chocaba con el anunciador
de rutas de Next.js: era un defecto de la prueba. Se agregaron tres verificaciones de C-08 (campo «Contexto del
entorno»: guardar y persistir, tope de 3000 → 422, solo lectura para el Operador). La corrida del 11/11 se había cortado
por falta de memoria. F-12 sigue abierto.

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

### F-02 · "Marcar como Resuelto" no refresca la lista de Incidentes — I-06 · ✅ IMPLEMENTADO (2026-11-11, E2E confirmado el 15/11)
- **Evidencia:** `incidents.spec.ts` I-06: con el filtro "pendiente", después de resolver la fila sigue en la
  lista con el estado viejo (la API sí quedó en `resuelto`).
- **Causa:** `App.resolveIncident` / `documentIncident` llaman a `loadData()` del contexto (Dashboard), pero
  `IncidentsView` tiene sus propias `rows` locales y no se entera.
- **Fix aplicado:** contador `incidentsVersion` en `AppProvider` (más `refreshIncidents()`), que se incrementa tras
  cada mutación y está en las dependencias del `useEffect` de `IncidentsView` y de `KnowledgeBaseView`.
- **Verificación pendiente:** I-06 no se volvió a correr (ver estado arriba).

### F-03 · Errores de "Aprobar y Auto-documentar" / "Marcar como Resuelto" no se muestran — I-06, I-07 · ✅ IMPLEMENTADO (2026-11-11, E2E confirmado el 15/11)
- **Evidencia:** I-07 en la 2.ª corrida: Gemini respondió `503 UNAVAILABLE (high demand)` → API 502. En la UI el
  panel se cierra al hacer click y **no aparece ningún error**; el incidente sigue en `nuevo` sin que el usuario
  sepa por qué.
- **Causa:** `App.tsx` cierra el drawer (`setSelectedIncidentId(null)`) **antes** de esperar la llamada, y el
  drawer invoca `void documentIncident(...)` → la promesa rechazada no la captura nadie.
- **Fix aplicado:** el panel de detalle (`IncidentTable.tsx`) espera la respuesta con el panel abierto; si falla,
  muestra el motivo que devolvió el servidor, aclara que el incidente no se modificó y permite reintentar. Solo se
  cierra si salió bien. Los reintentos con backoff ante 503/429 del proveedor LLM no se implementaron.
- **Verificación pendiente:** I-06 e I-07 (esta última falla igual con Vite, es un pendiente previo) no se
  volvieron a correr. Hay pruebas nuevas de errores visibles al documentar y al resolver, sin ejecutar completas.

### F-04 · Los runbooks manuales no muestran su documentación — K-05 · ✅ IMPLEMENTADO (2026-11-11, E2E confirmado el 15/11)
- **Evidencia:** `knowledge-base.spec.ts` K-05: al abrir un runbook no se muestra ningún markdown. Los 5
  `documentado_previamente` tienen `documento` vacío; los `auto_documentado` sí lo tienen.
- **Causa:** `Argos.Repository/Knowledge/MarkdownKnowledgeSource.cs` solo lee el frontmatter YAML
  (`tipo_alerta`, `resumen`, `pasos`…) y **descarta el cuerpo** del `.md` (secciones Contexto, Diagnóstico, etc.).
- **Fix aplicado:** `MarkdownKnowledgeSource` asigna el cuerpo posterior al frontmatter a `Documento`, y
  `SeedService` completa el documento de los runbooks ya existentes cuando está vacío (sin pisar los que lo tienen),
  así que también corrige instalaciones existentes.
- **Verificado:** en los logs de la API, "documento completado" en los cinco runbooks; un test unitario comprueba que los
  cinco runbooks incluidos traen documento. K-05 (E2E) no se volvió a correr.

### F-05 · Filtro "por tipo" exige el nombre exacto — I-04 · ✅ IMPLEMENTADO (2026-11-11, E2E confirmado el 15/11)
- **Evidencia:** `incidents.spec.ts` I-04: escribir `Nginx` → 0 resultados (`GET /incidents?tipo=Nginx` → `total: 0`);
  `tipo=NginxDown` sí funciona.
- **Causa:** la API filtra `tipo_alerta` por igualdad en Qdrant, pero el input es texto libre ("Filtrar por tipo…")
  y se dispara en cada tecla.
- **Fix sugerido (elegir uno):** (a) en la UI, reemplazar el input por un `<select>` con los tipos de
  `metrics.by_alert_type`; o (b) en la API, filtro por substring case-insensitive igual que `search`.
  La opción (a) es más simple y no toca la API.
- **Fix aplicado:** se tomó la opción (b): `GET /incidents?tipo=` es coincidencia parcial y no distingue mayúsculas
  (`IncidentFilter` / `IncidentRepository`); lo cubren tests unitarios. `eval/run-auto.mjs` pasó a exigir el tipo
  exacto del lado del cliente. Sigue disparándose una request por tecla (ver F-12). I-04 (E2E) no se volvió a correr.

### F-06 · Botón "Nuevo Documento" sin funcionalidad — K-06 · ✅ IMPLEMENTADO (2026-11-11, E2E confirmado el 15/11)
- **Evidencia:** `knowledge-base.spec.ts` K-06: el click no hace nada.
- **Dónde:** `KnowledgeBaseView.tsx`: el `<button>` no tiene `onClick`.
- **Fix sugerido:** quitarlo, o implementarlo: modal (tipo de alerta, resumen, pasos, markdown) →
  `POST /upsert` con `estado=documentado_previamente`, `fuente=manual`.
- **Fix aplicado:** "Nuevo documento" abre `NewKnowledgeDocModal.tsx` (solo Administrador) que crea un runbook manual
  con `POST /incidents/knowledge-base` (no `/upsert`, que es interno). Valida los campos, muestra los errores del
  servidor sin perder lo cargado y deja auditoría. Se agregó `DELETE /incidents/{id}` (solo Administrador).
- **Verificación pendiente:** K-06 y las pruebas nuevas de alta de documento y borrado no se volvieron a correr
  completas. El documento de prueba `DotnetMigrationE2E` sigue en la base de conocimiento; ahora se puede borrar con
  ese endpoint.

### F-07 · El umbral de la Base de Conocimiento está fijo en 0.63 — K-02 · ✅ CORREGIDO (2026-10-26)
- **Evidencia:** `settings.spec.ts` K-02: se guarda 0.75 en Configuración y el banner sigue mostrando `0.63`.
- **Fix aplicado:** `KnowledgeBaseView.tsx` lee `similarity_threshold` con `getSettings()`.
- **Verificado:** `settings.spec.ts` K-02 en verde.

### F-08 · El Operador podía generar documentación por API aunque la UI se lo ocultara — I-07 · ✅ CORREGIDO (2026-10-21)
- **Fix aplicado:** `POST /incidents/{id}/document` → `[Authorize(Roles = UserRoles.Admin)]`, igual que el botón en la UI.
  Si se decide que el Operador también apruebe, hay que cambiar ambos lados (API y `canApprove` en `IncidentTable.tsx`).
- **Verificado:** `api.smoke.spec.ts` → Operador 403 (sale de la autorización, sin llamar al LLM).

---

## Baja

### F-09 · El header no tiene título para "Pruebas de Carga" — S-05 · ✅ CORREGIDO (2026-10-26)
- **Evidencia:** `auth.spec.ts` S-05: el `<h1>` muestra `Argos`.
- **Fix aplicado:** `loadtests: 'Pruebas de Carga'` en el mapa `titles` de `Header.tsx`. Verificado: `auth.spec.ts` en verde.

### F-10 · Modelo LLM de otro proveedor se guarda sin aviso — C-01 · ✅ IMPLEMENTADO (2026-11-11, E2E confirmado el 15/11)
- **Evidencia:** `settings.spec.ts` C-01 (2.º): con proveedor Gemini, `modelo-sin-prefijo-e2e` → **200** sin
  `warning`. En la configuración queda guardado un modelo que nunca se usa (`LlmService.Resolve` usa el default
  si no empieza con el prefijo del proveedor). Un `gemini-*` inexistente sí da 422 ✅.
- **Fix sugerido:** en `SettingsService.UpdateAsync`, si el modelo no tiene el prefijo del proveedor → 422 o
  `warning` ("se usará <default>").
- **Fix aplicado:** se tomó el 422. `SettingsValidator.ValidateModelForProvider` rechaza un modelo que no corresponde
  al proveedor, con un mensaje claro; `SettingsView.tsx` lo muestra junto al botón de guardar y ofrece descartar los
  cambios. Cubierto por tests unitarios. C-01 (E2E) no se volvió a correr.

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
