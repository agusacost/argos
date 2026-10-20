# Argos Dashboard

SPA (React + Vite + TypeScript) — panel propio de Argos con **métricas** (MTTR, tendencia),
**historial de incidentes** y **base de conocimiento** auto-generada.

Consume el `rag-api` (Sprint 3/5) vía `/api` (proxy). Fuente de datos: colección Qdrant `incidentes`.

## Páginas

- **Métricas** (`/metrics`) — total, MTTR promedio, tendencia, barras por tipo, serie por día, desglose por estado.
- **Incidentes** (`/incidents`) — tabla paginada con filtros (estado, tipo) y búsqueda; detalle en modal (markdown).
- **Base de conocimiento** (`/knowledge-base`) — documentos `fuente=auto-generado`, búsqueda + detalle markdown.

## Desarrollo local

```bash
cd dashboard
npm install
# rag-api debe estar en http://localhost:8000 (o exportar VITE_DEV_API_TARGET)
npm run dev            # http://localhost:3001  (proxy /api -> localhost:8000)
npm run typecheck      # chequeo de tipos (opcional)
```

## Docker

Build multi-stage (node → nginx). El nginx del contenedor proxya `/api` → `http://rag-api:8000`
(ambos en `argos-net`), evitando CORS. Se levanta como servicio `argos-dashboard` (puerto 3001)
en `docker/monitoring/docker-compose.yml`.

```bash
docker compose -p argos-platform -f docker-compose.yml up -d --build argos-dashboard
# http://localhost:3001
```
