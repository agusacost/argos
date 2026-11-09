# Validación de la Argos API

**API:** `argos-api/` (.NET 10), contenedor `argos-rag-api`, alias de red `rag-api:8000`.
**Criterio:** la app (dashboard + n8n + k6) funciona completa sobre la API.

---

## 1. Validación técnica (hecha, 2026-10-19)

| # | Prueba | Resultado |
|---|---|---|
| 1 | Endpoints del dashboard vía `:3001/api` (incidentes, métricas, KB, settings, usuarios, notificaciones, actividad, observabilidad, integraciones, pruebas de carga) | ✅ 200 |
| 2 | Endpoints de n8n (`/query`, `/upsert`, `/internal/config`, `/notifications/recipients` con `X-Argos-Token`) | ✅ |
| 3 | n8n: alerta simulada critical → Get Config → RAG → LLM → auto-doc → `/upsert` → email al pool | ✅ |
| 4 | n8n: alertas reales del escenario k6 `attack` → Alertmanager → n8n → email al pool | ✅ |
| 5 | "Generar documentación" con Gemini: documento guardado, MTTR calculado, auditoría registrada | ✅ |
| 6 | k6 on-demand: corrida `attack` `passed`; segunda corrida simultánea → 409; detener → `cancelled` y contenedor eliminado | ✅ |
| 7 | Arranque desde cero (Qdrant vacío): colecciones + admin + 5 runbooks + 15 demo; reinicio sin duplicados | ✅ |
| 8 | Errores: 401/403 de auth, 404 ruta inexistente, 405 método, 422 body/parámetros inválidos, 400 UTF-8 inválido | ✅ |

---

## 2. Validación funcional en la UI (pendiente: recorrer en http://localhost:3001)

Marcar ✅/❌.

| Área | Qué probar | ✅/❌ |
|---|---|---|
| Sesión | Login admin; F5 mantiene la sesión; logout; login con un Operador | |
| Dashboard | KPIs, actividad reciente, campana de notificaciones, marcar como leída | |
| Incidentes | Lista, paginado, filtros estado/tipo, búsqueda, detalle, logs y sparkline | |
| Incidentes | Cambiar estado (aparece en auditoría); "Generar documentación" con LLM | |
| Base de Conocimiento | Runbooks + docs auto-generados; abrir el markdown | |
| Métricas | Salud de servicios (4 en healthy); golden signals 15m/1h/3h/6h con datos | |
| Pruebas de Carga | Ver código de los 3 escenarios; correr `normal`; detener una corrida; Operador sin botón/acceso | |
| Usuarios | Crear, editar rol y contraseña, login con el usuario nuevo, borrar | |
| Configuración | Umbral, auto-doc, polling, modelo LLM inválido → error, pool de emails, Integraciones en vivo | |
| Email | Llegan los emails de alertas al pool configurado | |

---

## 3. Recuperación

Punto de restauración de datos: snapshots de Qdrant de las 6 colecciones del 2026-10-19 20:48 UTC
(`GET http://localhost:6333/collections/<col>/snapshots`).

Tras recrear el contenedor de la API: `docker restart argos-dashboard` (su nginx cachea la IP de `rag-api`).
