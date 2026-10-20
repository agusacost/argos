# k6 — Pruebas de carga (Sprint 6)

Escenarios de tráfico para validar la plataforma Argos: detección de incidentes por umbral,
calidad de la síntesis del LLM y persistencia de la auto-documentación. Cubre el objetivo
específico 3 de la propuesta (tráfico normal, picos de carga y patrones de ataque).

## Requisitos

- Stack Argos levantado (`docker compose -p argos-platform up -d`) y la red `argos-net` creada.
- No requiere instalar k6 en el host: se ejecuta con la imagen `grafana/k6` vía Docker Compose.

## Escenarios

| Script | Perfil | Objetivo |
|--------|--------|----------|
| `tests/normal-traffic.js` | 10 VUs, 5 min | Línea base: el flujo Argos **no** se dispara (sin falsos positivos). |
| `tests/spike-traffic.js` | pico a 100 VUs | Dispara alertas de latencia → Argos propone escalado. |
| `tests/attack-pattern.js` | ~50 VUs atacando + fondo legítimo | Paths 404, UA vacío, IPs rotativas → alertas de error rate / IP sospechosa → auto-doc. |

Todos apuntan por defecto al reverse proxy interno `http://nginx-proxy` (dentro de `argos-net`).
Override con `-e BASE_URL=...` si se corre contra otra URL.

## Ejecución

```powershell
cd D:\DEV\argos

docker compose -f k6/docker-compose.yml run --rm k6 run --out json=/results/normal-traffic.json /tests/normal-traffic.js
docker compose -f k6/docker-compose.yml run --rm k6 run --out json=/results/spike-traffic.json  /tests/spike-traffic.js
docker compose -f k6/docker-compose.yml run --rm k6 run --out json=/results/attack-pattern.json /tests/attack-pattern.js
```

Los JSON quedan en `k6/results/` (ignorados por git).

## Observación durante las pruebas

- **Grafana** (`http://localhost:3000`): dashboards nginx-overview e infrastructure (request rate,
  status codes, latencia P95, CPU/memoria).
- **Prometheus** (`http://localhost:9090/alerts`): ver alertas pasar a `firing`.
- **Alertmanager** (`http://localhost:9093`): agrupación y ruteo hacia el webhook de n8n.
- **n8n** (`docker logs -f argos-n8n`): recepción del webhook y ejecución del workflow.
- **Dashboard Argos** (`http://localhost:3001`): incidentes nuevos y auto-documentación generada.

## Métricas a registrar (para la memoria académica)

- Response times p50 / p95 / p99, error rate, throughput (req/s) por escenario.
- Timestamps de: disparo de alerta → webhook n8n → respuesta del LLM → persistencia de auto-doc.
- Estimación de reducción de MTTR frente al proceso manual de referencia.
