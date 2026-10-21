# Funcionalidades del dashboard Argos

**Fecha:** 2026-10-21 · **URL:** http://localhost:3001 · **Código:** `dashboard/src/`

El dashboard es una SPA React + Vite servida por nginx (contenedor `argos-dashboard`). Todo lo que
muestra sale de la Argos API (.NET 10) a través del proxy `/api` → `rag-api:8000`. No usa rutas URL:
la vista activa es estado interno de React (`App.tsx`), así que un F5 siempre vuelve al Dashboard.

Roles: **Admin** (acceso total) y **Operador** (lectura + resolver incidentes).

Columna **ID** = identificador usado en la suite E2E (`dashboard/e2e/`) y en
[`FIXES_DASHBOARD.md`](FIXES_DASHBOARD.md).

---

## 1. Sesión y estructura general

| ID | Funcionalidad | Qué hace | API |
|---|---|---|---|
| S-01 | Login | Formulario email + contraseña. Si es válido guarda el JWT en `localStorage` (`argos_token`) y entra al Dashboard. Si no, muestra el mensaje de error de la API. | `POST /auth/login` |
| S-02 | Restaurar sesión | Al recargar la página, si hay token guardado lo valida con `/auth/me`; si es inválido o venció (12 h) lo borra y muestra el login. | `GET /auth/me` |
| S-03 | Logout | Desde el sidebar o el menú de usuario del header: borra el token y el estado, vuelve al login. | — |
| S-04 | Menú lateral por rol | 7 entradas; "Gestión de Usuarios" solo se muestra a Admin. | — |
| S-05 | Header | Título de la vista activa, badges "Sistema Operativo" / "IA Activa", buscador, campana y menú de usuario (nombre, email, rol). | — |
| S-06 | Campana de notificaciones | Lista las notificaciones (una por incidente nuevo) con severidad y fecha; contador de no leídas; la X marca una como leída. | `GET /notifications`, `POST /notifications/{id}/read` |
| S-07 | Refresco automático | Cada N segundos (Configuración → polling, default 20 s) recarga métricas, los 50 incidentes más recientes y las notificaciones. | `GET /settings` |

## 2. Dashboard

| ID | Funcionalidad | Qué hace | API |
|---|---|---|---|
| D-01 | KPIs | 4 tarjetas: **MTTR actual** (promedio + tendencia mejorando/estable/empeorando), **Incidentes hoy** (+ total), **Auto-documentados**, **Análisis IA activos** (incidentes en estado `nuevo`). | `GET /incidents/metrics` |
| D-02 | Banner de IA | Cantidad de análisis en curso y de críticos activos. | (contexto) |
| D-03 | Incidentes por día | Gráfico de barras de los últimos 6 días + tendencia. | `GET /incidents/metrics` |
| D-04 | Resumen operacional | Conteo de críticos activos, en investigación (`pendiente`), resueltos y documentados; total. | `GET /incidents/metrics` |
| D-05 | Actividad del sistema | Últimas 5 acciones auditadas (quién hizo qué y cuándo). | `GET /activity` |
| D-06 | Incidentes recientes | Los 8 incidentes más recientes; click abre el detalle (I-05). | `GET /incidents?size=50` |

## 3. Incidentes Activos

| ID | Funcionalidad | Qué hace | API |
|---|---|---|---|
| I-01 | Tabla paginada | 20 por página: ID, severidad, componente (derivado del tipo de alerta), descripción, estado y antigüedad. | `GET /incidents?page&size` |
| I-02 | Búsqueda | Filtra por texto en resumen o tipo de alerta (vuelve a la página 1). | `?search=` |
| I-03 | Filtro por estado | nuevo / pendiente / resuelto / auto_documentado / documentado_previamente. | `?estado=` |
| I-04 | Filtro por tipo | Texto libre sobre el tipo de alerta. | `?tipo=` |
| I-05 | Detalle (panel lateral) | Severidad, estado, MTTR, componente, **logs de Loki** y **sparkline de Prometheus** de la ventana del incidente, **análisis de la IA** (resumen + pasos de resolución), documento markdown si existe, **auditoría** del incidente. | `GET /incidents/{id}`, `/{id}/observability`, `/{id}/audit` |
| I-06 | Marcar como resuelto | Pone `estado=resuelto` y `resolved_at=ahora`; la API calcula el MTTR y lo audita. Visible para cualquier rol en incidentes no resueltos/documentados. | `PATCH /incidents/{id}` |
| I-07 | Aprobar y auto-documentar | (Solo Admin) El LLM configurado genera la documentación técnica, se guarda en el incidente, pasa a `auto_documentado` y entra a la Base de Conocimiento. | `POST /incidents/{id}/document` |

## 4. Base de Conocimiento (RAG)

| ID | Funcionalidad | Qué hace | API |
|---|---|---|---|
| K-01 | Listado de documentos | Tarjetas de los incidentes `documentado_previamente` (runbooks manuales) y `auto_documentado` (generados por el LLM), con componente, origen (Manual / Auto-generado) y fecha. | `GET /incidents/knowledge-base` |
| K-02 | Banner RAG | Cantidad de documentos indexados, modelo de embeddings y umbral de similitud. | — |
| K-03 | Búsqueda | Filtra por texto. | `?search=` |
| K-04 | Filtro por categoría | Chips por componente (calculados sobre la página actual). | — |
| K-05 | Ver documento | Click abre el mismo panel de detalle (I-05) con el markdown renderizado. | `GET /incidents/{id}` |
| K-06 | Nuevo documento | Botón "Nuevo Documento". | — |

## 5. Métricas de Sistema

| ID | Funcionalidad | Qué hace | API |
|---|---|---|---|
| M-01 | Tarjetas | MTTR promedio + tendencia, incidentes totales (barra de % resueltos), auto-documentados. | `GET /incidents/metrics` |
| M-02 | Incidentes por día | Gráfico de líneas de los últimos 7 días. | ídem |
| M-03 | Salud de servicios | Probe HTTP directo a nginx, API de membresía, Qdrant y la propia Argos API: `healthy` / `degraded` / `unknown`. | `GET /observability/services` |
| M-04 | Golden signals Nginx | Desde Loki: requests/s por código (2xx–5xx), latencia upstream p50/p95/p99, request rate y % de 5xx actuales. Selector 15m/1h/3h/6h; refresco cada 20 s. | `GET /observability/nginx-metrics?range=` |
| M-05 | Top tipos de alerta | Los 5 tipos con más incidentes. | `GET /incidents/metrics` |
| M-06 | Desglose por estado | Cantidad de incidentes por estado. | ídem |

## 6. Pruebas de Carga (k6)

| ID | Funcionalidad | Qué hace | API |
|---|---|---|---|
| L-01 | Escenarios | Lista `normal`, `spike` y `attack` con descripción, perfil y duración. | `GET /loadtests/scenarios` |
| L-02 | Ver código | Visor con resaltado de sintaxis del script k6 seleccionado. | (incluido en L-01) |
| L-03 | Ejecutar escenario | (Solo Admin) La API lanza un contenedor `grafana/k6` hermano vía Docker socket. Una sola corrida a la vez (otra → 409). | `POST /loadtests/runs` |
| L-04 | Seguimiento | Mientras hay una corrida `running`, refresca cada 3 s. | `GET /loadtests/runs` |
| L-05 | Detener | (Solo Admin) Mata el contenedor y marca la corrida `cancelled`. | `POST /loadtests/runs/{id}/stop` |
| L-06 | Historial y resultados | Estado (OK / Thresholds / Falló / Cancelada), duración, requests, throughput, p95, latencia media, error rate, checks y salida de k6. | `GET /loadtests/runs` |

> El escenario `attack` dispara alertas reales → Alertmanager → n8n → **emails al pool**.

## 7. Gestión de Usuarios (solo Admin)

| ID | Funcionalidad | Qué hace | API |
|---|---|---|---|
| U-01 | Listado | Nombre, fecha de alta, email y rol. | `GET /users` |
| U-02 | Crear | Modal: email, nombre, rol (Admin/Operador), contraseña. | `POST /users` |
| U-03 | Editar | Nombre, rol y contraseña opcional (el email no se edita). | `PATCH /users/{id}` |
| U-04 | Eliminar | Con confirmación; no se puede borrar al usuario logueado. | `DELETE /users/{id}` |

## 8. Configuración

Todos la ven; solo Admin puede editar y guardar (el Operador ve los controles deshabilitados).

| ID | Funcionalidad | Qué hace | API |
|---|---|---|---|
| C-01 | Modelo de IA | Proveedor (Gemini / Claude) y modelo. Al guardar la API valida contra el proveedor (key o modelo inválido → 422). Aviso si falta la API key del proveedor. Lo usan n8n y "Generar documentación". | `GET/PATCH /settings` |
| C-02 | Umbral de similitud RAG | Slider 0.30–0.95: por encima, la alerta se considera ya documentada. | ídem |
| C-03 | Auto-documentación | ON: n8n documenta los incidentes nuevos con el LLM. OFF: quedan en `nuevo`. | ídem |
| C-04 | Pool de destinatarios | Emails que reciben el análisis de cada alerta (n8n). Validación de formato y duplicados, máximo 50; vacío → `ALERT_RECIPIENT_EMAIL`. | ídem |
| C-05 | Integraciones en vivo | Estado up/down + latencia de Prometheus, Loki, Qdrant, etc.; refresco cada 30 s y manual. | `GET /integrations/status` |
| C-06 | Refresco automático | Slider 10–120 s para el polling del dashboard (S-07). | `PATCH /settings` |
| C-07 | Guardar | Persiste todo junto; muestra "Guardado correctamente", aviso no bloqueante o error. | `PATCH /settings` |

---

## Estado tras la suite E2E (2026-10-21)

Detalle, evidencia y fix sugerido de cada falla en [`FIXES_DASHBOARD.md`](FIXES_DASHBOARD.md).

| Estado | IDs |
|---|---|
| ✅ Funciona | S-01, S-02, S-03, S-04, S-07, D-01…D-06, I-01, I-02, I-03, I-05, K-01, K-03, K-04, M-01…M-06, L-01…L-06, U-01…U-04, C-02…C-07 |
| ⚠️ Funciona con observaciones | S-06 (lectura global, no por usuario; no verificable con >9 sin leer), I-06 (no refresca la lista, F-02), I-07 (anduvo con Gemini; sin feedback ante error, F-03), C-01 (modelo de otro proveedor sin aviso, F-10) |
| ❌ No funciona | S-05 título de Pruebas de Carga (F-09), I-04 filtro por tipo (F-05), K-02 umbral fijo (F-07), K-05 runbooks sin markdown (F-04), K-06 Nuevo Documento (F-06) |
| 🔒 Seguridad | ✅ F-01, F-08 y F-13 corregidos: JWT en incidentes, actividad y observabilidad; documentar solo Admin; token interno en los endpoints de n8n y puerto 8000 solo en loopback |
