# Resultados de pruebas de carga k6 — Sprint 6

**Fecha de ejecución:** 2026-09-30
**Entorno:** local (Docker Compose, proyecto `argos-platform`), 13 contenedores.
**Herramienta:** k6 (`grafana/k6`) en contenedor dentro de `argos-net`, target `http://nginx-proxy`.
**JSON crudos:** `k6/results/*.json` (ignorados por git por tamaño).

## Resumen por escenario

| Escenario | Reqs | Throughput | p95 latencia | http_req_failed | Resultado |
|-----------|------|-----------|--------------|-----------------|-----------|
| Normal    | 1 625  | 5.40 req/s  | **3.23 ms** | **0.00 %** | ✅ Baseline limpio, sin falsos positivos |
| Spike     | 17 998 | 59.90 req/s | 2.51 ms     | 61.12 %    | ⚠️ Rate-limiting de Nginx bajo 100 VUs |
| Ataque    | 28 454 | 94.40 req/s | —           | 96.45 %    | ✅ Dispara toda la cadena de detección |

## Escenario 1 — Tráfico normal (10 VUs, 5 min)
- p95 = 3.23 ms, error rate = 0.00 %, todos los thresholds en verde.
- **No** se disparó ninguna alerta → validación de ausencia de falsos positivos.

## Escenario 2 — Pico (100 VUs, min 1–2)
- Latencia se mantuvo plana (p95 = 2.51 ms): **no hubo saturación de CPU/latencia**.
- **61 % de errores** por rate-limiting de Nginx (503) ante el pico de conexiones.
- Hallazgo: el pico se comporta como evento de *error rate*, no de latencia. La fase de error
  (~1 min) no sostuvo el umbral `for: 2m`, por lo que no llegó a `firing` (sí quedó registrada).

## Escenario 3 — Patrón de ataque (baseline 5 VUs + 50 VUs atacando, min 1–4)
Distribución de status codes en Nginx (Loki, ventana 5 min):

| Status | Requests |
|--------|----------|
| 200 | 622 |
| 400 | 344 |
| 404 | 4 130 |
| 503 | 14 822 |

Tasa de 5xx ≈ 74 % → cruza ampliamente los umbrales de error rate.

### Alertas disparadas (cadena de detección validada)
Reglas de Loki (`docker/monitoring/loki/rules/fake/nginx-http.yml`), evaluadas por el ruler de Loki:

| Alerta | Severidad | Estado alcanzado |
|--------|-----------|------------------|
| `NginxCriticalErrorRate` | critical | **firing** (>20 % 5xx, `for 1m`) |
| `NginxErrorLogSpike`     | warning  | **firing** (>1 err/s en error.log) |
| `NginxHighErrorRate`     | warning  | **firing** (>5 % 5xx, `for 2m`) |
| `NginxSuspiciousIPActivity` | warning | **firing** (IP k6 >200 req/min, `for 2m`) |

- **Alertmanager** (`http://localhost:9093`): las 4 alertas activas, ruteadas al receiver `webhook-n8n`.
- **n8n**: webhook `POST http://n8n:5678/webhook/argos-alerts` responde **HTTP 200** (workflow activo
  procesa el evento → RAG → LLM → síntesis/email).
- **Auto-documentación**: NO se generó incidente nuevo (total se mantuvo en 21). Correcto: los tipos
  de alerta disparados (`NginxCriticalErrorRate`, `NginxErrorLogSpike`) ya tienen runbook en la base
  de conocimiento (`argos-api/Argos.Api/knowledge/nginx-5xx.md`, `error-log-spike.md`), por lo que RAG los marca como
  documentados y la rama de auto-doc (`Is New?`) no se activa. La auto-doc solo dispara ante
  incidentes no registrados.

## Nota sobre el ruler de Loki
Las alertas basadas en logs **no** aparecen en Prometheus `/api/v1/alerts`. Consultarlas en:
- Loki ruler: `GET http://localhost:3100/prometheus/api/v1/alerts`
- Alertmanager: `GET http://localhost:9093/api/v2/alerts`

## Reproducir
```powershell
docker compose -f k6/docker-compose.yml run --rm k6 run --out json=/results/normal-traffic.json /tests/normal-traffic.js
docker compose -f k6/docker-compose.yml run --rm k6 run --out json=/results/spike-traffic.json  /tests/spike-traffic.js
docker compose -f k6/docker-compose.yml run --rm k6 run --out json=/results/attack-pattern.json /tests/attack-pattern.js
```
