# Argos Dashboard

Next.js 16 (App Router, export estático) + React 19 + TypeScript — panel propio de Argos con **métricas** (MTTR, tendencia),
**historial de incidentes** y **base de conocimiento** auto-generada.

Consume el `rag-api` (Sprint 3/5) vía `/api` (proxy). Fuente de datos: colección Qdrant `incidentes`.

## Rutas

Una ruta de Next.js por vista (`src/app/`), con el layout compartido (sidebar, header, drawer de
incidente) en `src/app-shell/`. El mapa ruta ↔ vista está en `src/routes.ts`.

| Ruta | Vista |
|---|---|
| `/` | Dashboard (KPIs, incidentes recientes, actividad) |
| `/incidentes` (`?estado=nuevo` opcional) | Historial paginado con filtros y detalle |
| `/conocimiento` | Base de conocimiento (runbooks + auto-documentados) |
| `/metricas` | MTTR, volumen, golden signals de Nginx, salud de servicios |
| `/pruebas-carga` | Escenarios k6 on-demand (ejecutar solo Admin) |
| `/usuarios` | Gestión de usuarios (solo Admin; un Operador vuelve a `/`) |
| `/configuracion` | LLM, umbral, auto-doc, polling, destinatarios, integraciones |

Sin sesión, cualquier ruta muestra el login y, al entrar, queda en la vista pedida. Una ruta
inexistente devuelve 404 (`src/app/not-found.tsx`).

## Desarrollo local

```bash
cd dashboard
npm install
# rag-api debe estar en http://localhost:8000 (o exportar DEV_API_TARGET)
npm run dev            # next dev en http://localhost:3001 (rewrite /api -> localhost:8000)
npm run typecheck      # chequeo de tipos
npm run build          # export estático en out/ (una página HTML por ruta)
```

## Docker

Build multi-stage (node → nginx): `next build` genera el export estático (`out/`) y nginx lo sirve
(`/metricas` → `metricas.html`, 404 → `404.html`). El nginx del contenedor proxya `/api` →
`http://rag-api:8000` (ambos en `argos-net`), evitando CORS, y bloquea los endpoints internos de n8n. Se levanta como servicio `argos-dashboard` (puerto 3001)
en `docker/monitoring/docker-compose.yml`.

```bash
docker compose -p argos-platform up -d --build dashboard
# http://localhost:3001
```

## Tests E2E (Playwright)

Corren contra el stack real (`docker compose up -d`) en `http://localhost:3001`.

```bash
npx playwright install chromium   # una vez
npm run test:e2e                  # API + UI, sin LLM ni k6
npm run test:e2e:full             # + @llm (Gemini) y @k6 (corrida normal + stop)
npx playwright show-report
```

Variables: `E2E_BASE_URL`, `E2E_ADMIN_EMAIL`, `E2E_ADMIN_PASSWORD`. Funcionalidades cubiertas en
`docs/FUNCIONALIDADES_DASHBOARD.md`; fallas conocidas en `docs/FIXES_DASHBOARD.md`.
