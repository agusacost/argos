# Argos: Product Backlog / Plan de Evolución del Trabajo

**Proyecto:** Argos — Plataforma inteligente de AIOps  
**Alumno:** Juan Agustín Acosta (LU 57851)  
**Profesor Orientador:** Rios, Leopoldo Jose  
**Fecha de creación:** 2026-08-04  
**Versión:** 1.0  

---

## Propósito de este documento

Este es el **product backlog operativo** del proyecto Argos. Complementa la propuesta académica formal
(`docs/Anexoll_Acosta_Juan_Agustin_v2.md`, Anexo II) que define el alcance y las fases; este
documento es el **registro vivo de qué está hecho, qué falta y cómo se desglosa en subtareas** por
sprint, con estado marcado.

Se mantiene actualizado conforme avanza la implementación y sirve como base para la toma de
decisiones, estimaciones y seguimiento de la deuda técnica.

---

## Estado actual (Snapshot a 2026-08-04)

| Sprint | Objetivo | Entregable esperado | Estado | % |
|--------|----------|----------------------|--------|-----|
| **1** | Infraestructura base (Terraform/LocalStack + Docker stacks) | Flujo end-to-end Nginx → Alloy → Loki → Grafana | ✅ Hecho | 100% |
| **2** | Stack observabilidad + reglas de detección | Tableros + alertas (18 reglas) + Alloy | ✅ Hecho | 100% |
| **3** | Base de conocimiento vectorial + RAG | Qdrant + pipeline RAG funcional | ✅ Hecho | 100% |
| **4** | Orquestación n8n + Claude API | Workflows → análisis IA + notificaciones | ⏳ No iniciado | 0% |
| **5** | Auto-documentación + dashboard Argos | Dashboard propio + docs auto-generadas | ⏳ No iniciado | 0% |
| **6** | Simulación k6 + validación + cloud | k6 scenarios + cloud deploy (GCP/AWS) | ⏳ No iniciado | 0% |

**Próximo hito:** Iniciar Sprint 4 (n8n + Claude API + webhook Alertmanager).

---

## Leyenda de estados

- `[x]` — Completado / funcional
- `[ ]` — Pendiente / no iniciado
- `[~]` — Parcial / requiere configuración o tiene bloqueante menor

---

## Sprint 1 — Infraestructura base (IaC + Docker)

**Objetivo:** Provisionar la infraestructura mediante Terraform sobre LocalStack Pro, orquestando
los stacks Docker interconectados. Entregable: flujo end-to-end Nginx → Alloy → Loki → Grafana
operativo.

**Estado:** ✅ Completo (con residuales de seguridad)

### Terraform / LocalStack

- [x] Configurar provider AWS v5.0+ hacia `http://localhost:4566` (LocalStack endpoint)
- [x] Definir `aws_instance.observability` (EC2 simulada, t3.medium, tag `argos-node`)
- [x] Script PowerShell `get_or_create_ami.ps1` — registra/consulta AMI Ubuntu 22.04 en LocalStack
- [x] `null_resource.provision` — orquesta los 3 stacks Docker en secuencia
- [x] Crear red `argos-net` (bridge) con `docker network create`
- [x] Outputs Terraform: instance_id, private_ip, URLs (Grafana, Prometheus, Loki)
- [~] **Credenciales:** AWS test/test (aceptable para LocalStack, cambiar si se usa cloud real)

### Docker: Stack Monitoring

- [x] Servicio Prometheus v2.54.1 (puerto 9090)
- [x] Servicio Loki v3.2.0 (puerto 3100)
- [x] Servicio Grafana v11.3.0 (puerto 3000)
- [x] Servicio Alertmanager v0.27.0 (puerto 9093)
- [x] Volúmenes: prometheus_data, loki_data, grafana_data, alertmanager_data
- [x] Red externa `argos-net` conectada

### Docker: Stack Proxy

- [x] Servicio Nginx (reverse proxy, puertos 80 / 8443)
- [x] Servicio Nginx Exporter v1.1.0 (puerto 9113)
- [x] Servicio Node Exporter v1.8.2 (puerto 9100)
- [x] Servicio cAdvisor v0.49.1 (puerto 8080)
- [x] Servicio Alloy v1.4.3 (puerto 12345) — colector de logs
- [x] Volúmenes: nginx-logs (compartido con Alloy), alloy-data
- [x] Red externa `argos-net` conectada

### Docker: Stack Apps

- [x] Servicio SQL Server 2022 (puerto 1433)
- [x] Servicio API .NET 10 (puerto 8080, Alpine multi-stage build)
- [x] Health checks configurados
- [x] Red externa `argos-net` conectada
- [x] **Base de datos:** esquema `MembresiaDB` inicializado vía contenedor `db-init` (docker/apps/db/init-db.sql)

### Validación / Smoke Tests

- [x] `terraform plan` / `terraform apply` ejecuta sin errores
- [x] 11 contenedores en ejecución (`docker ps --filter "name=argos-"`)
- [x] Nginx accesible en `http://localhost` y `https://localhost:8443`
- [x] Grafana accesible en `http://localhost:3000`

---

## Sprint 2 — Stack de observabilidad + Reglas de detección

**Objetivo:** Desplegar Prometheus, Loki, Grafana y Alertmanager; validar recolección de
métricas/logs; definir reglas de detección (umbrales) que actúan como trigger. Entregable: tableros
funcionales con datos reales + alertas configuradas.

**Estado:** ✅ Completo (con residuales de configuración y webhook pendiente)

### Prometheus: Scrape targets y configuración

- [x] `prometheus.yml` — 4 scrape jobs configurados:
  - prometheus (self-monitoring)
  - node-exporter (host metrics)
  - cadvisor (container metrics)
  - nginx-exporter (Nginx metrics)
- [x] Intervalo de scrape: 15s
- [x] Endpoint Alertmanager configurado
- [x] Archivos de reglas apuntados: `/etc/prometheus/rules/*.yml`

### Prometheus: Alert Rules (13 total)

**Archivo: `prometheus/rules/nginx.yml`** (4 alertas)

- [x] `NginxDown` (critical) — nginx-exporter no responde (scrape failure)
- [x] `NginxHighActiveConnections` (warning) — active connections > 800
- [x] `NginxHighRequestRate` (warning) — request rate > 500 req/s durante 5m
- [x] `NginxWaitingConnections` (warning) — waiting connections > 100

**Archivo: `prometheus/rules/infrastructure.yml`** (9 alertas)

- [x] `HostHighCPU` (warning) — CPU host > 80% durante 5m
- [x] `HostCriticalCPU` (critical) — CPU host > 95% durante 2m
- [x] `HostHighMemory` (warning) — available memory < 15% (>85% usado)
- [x] `HostHighDisk` (warning) — disk usage > 85%
- [x] `HostDiskCritical` (critical) — disk usage > 95%
- [x] `NodeExporterDown` (critical) — node-exporter scrape failure
- [x] `ContainerHighCPU` (warning) — container CPU > 80% durante 5m
- [x] `ContainerHighMemory` (warning) — container memory > 85% de límite
- [x] `CAdvisorDown` (critical) — cadvisor scrape failure

### Loki: Configuración y Log Alert Rules (5 total)

**Archivo: `loki/config.yml`**

- [x] HTTP listen port: 3100
- [x] Storage: filesystem (chunks + rules)
- [x] Schema: tsdb v13 desde 2023-01-01
- [x] Retención: 168h (7 días)
- [x] Ruler: habilitado con endpoint Alertmanager (`http://alertmanager:9093`)
- [x] Archivos de reglas: `/loki/rules-temp`

**Archivo: `loki/rules/fake/nginx-http.yml`** (5 alertas, grupo: `nginx-http`)

- [x] `NginxHighErrorRate` (warning) — 5xx error rate > 5% durante 5m (condición 2m)
- [x] `NginxCriticalErrorRate` (critical) — 5xx error rate > 20% durante 5m (condición 1m)
- [x] `NginxHighResponseTime` (warning) — P95 upstream_response_time > 2s durante 5m (condición 3m)
- [x] `NginxSuspiciousIPActivity` (warning) — IP sospechosa > 200 req/min (condición 2m)
- [x] `NginxErrorLogSpike` (warning) — error.log spike > 1 err/s (condición 2m)

**Nota:** Estas 5 alertas Loki son el **trigger por umbral** central del proyecto Argos. Cuando se
superan, activan el flujo inteligente (Sprint 4).

### Alloy: Log Collection

**Archivo: `alloy/config.alloy`**

- [x] Pipeline Nginx access logs (JSON parsing)
  - Fuente: `/var/log/nginx/access.log`
  - Labels: `node=node2-proxy`, `service=nginx`, `log_type=access`
  - 16 campos extraídos (timestamp, remote_ip, método, URI, status, latencia, etc.)
  - Destino: `http://loki:3100/loki/api/v1/push`

- [x] Pipeline Nginx error logs (regex parsing)
  - Fuente: `/var/log/nginx/error.log`
  - Labels: `node=node2-proxy`, `service=nginx`, `log_type=error`
  - Extrae: timestamp, level, PID, TID, mensaje
  - Destino: Loki

- [~] Pipeline Docker container logs (COMENTADO, opcional)
  - Requiere montura de `/var/run/docker.sock`
  - Activar si se necesita recolectar logs de contenedores adicionales

### Alertmanager: Routing y Receivers

**Archivo: `alertmanager/alertmanager.yml`**

- [x] Resolve timeout: 5m
- [x] SMTP TLS requerido: true
- [x] Routes por severity (critical → email-critical, warning → email-default)
- [x] Inhibición: alertas críticas suprimen warnings del mismo tipo
- [x] Receivers email (2): email-default, email-critical
- [~] SMTP credentials: placeholders (`REPLACE_WITH_*`) — **requiere configuración real**
- [ ] Webhook receiver `webhook-n8n`: **removido en migración, se reintroduce en Sprint 4**

### Grafana: Datasources y Dashboards

**Archivo: `grafana/provisioning/datasources/datasources.yml`**

- [x] Datasource Prometheus (UID: `prometheus`, URL: `http://prometheus:9090`, default: true)
- [x] Datasource Loki (UID: `loki`, URL: `http://loki:3100`, max lines: 1000)

**Archivo: `grafana/provisioning/dashboards/dashboards.yml`**

- [x] Folder: "Observability"
- [x] Update interval: 30s
- [x] UI edits disabled (read-only)

**Dashboards presentes:**

- [x] `nginx-overview.json` — Nginx performance (connections, request rate, status codes, latency)
- [x] `infrastructure.json` — Host CPU, memory, disk, container metrics
- [x] `nginx-logs.json` — Nginx access log streaming (Loki queries)

**Admin:** usuario `admin` / contraseña `admin`

### Validación / Smoke Tests

- [x] `http://localhost:9090` — Prometheus muestra targets y alertas
- [x] `http://localhost:3100/loki/api/v1/label/__name__/values` — Loki recolecta métricas
- [x] `http://localhost:3000` — Grafana dashboards cargan con datos
- [x] Alertmanager accesible en `http://localhost:9093`
- [x] Logs Nginx recolectados en Loki (verificar con query: `{service="nginx"}`)

---

## Sprint 3 — Base de conocimiento vectorial + RAG

**Objetivo:** Implementar base de conocimiento vectorial y pipeline de recuperación aumentada de
información (RAG) que permite recuperar contexto relevante de incidentes previos.

Entregable: base de conocimiento con contenido inicial, pipeline RAG validado, criterio de
similitud definido.

**Estado:** ✅ Completo | 100%

### Infraestructura / Docker

- [x] Agregar servicio `qdrant` a `docker/monitoring/docker-compose.yml`
  - Imagen: `qdrant/qdrant:v1.12.4`
  - Puerto: 6333 (REST) + 6334 (gRPC)
  - Volumen: `qdrant_data` (persistencia)
  - Red: `default` + `argos-net`

- [x] Variables de entorno en `rag/.env.example`:
  - `QDRANT_URL`, `VOYAGE_API_KEY`, `EMBEDDING_MODEL`, `VECTOR_SIZE`, `SIMILARITY_THRESHOLD`

### Embedding Model

- [x] Modelo seleccionado: **Voyage AI `voyage-3-lite`** (Opción 1 recomendada)
  - Dimensión: 512
  - Métrica: cosine similarity
  - Requiere: `VOYAGE_API_KEY` en `rag/.env`

### Colecciones Qdrant

- [x] Colección `incidentes` definida en `rag/argos_rag/qdrant_store.py` (`ensure_collection`)
  - Vector size: 512, distance: Cosine
  - Payload: tipo_alerta, resumen, pasos, fecha, estado, fuente

- [x] Script idempotente: `rag/scripts/init_collections.py` (polling readyz + create_collection)

### Pipeline RAG

- [x] `embed(texto, input_type)` → Voyage AI → `list[float]` (512 dims)
- [x] `upsert_incident(client, incident)` → embedding + upsert con ID determinístico UUID5
- [x] `query_incidents(client, query, top_k=3)` → top-k con score + clasificación
- [x] Umbral de similitud: `SIMILARITY_THRESHOLD=0.75` (configurable en `.env`)

### Carga Inicial de Conocimiento

- [x] 5 docs en `rag/knowledge/` (frontmatter YAML + markdown):
  - `nginx-5xx.md` → `NginxHighErrorRate`
  - `nginx-latency.md` → `NginxHighResponseTime`
  - `suspicious-ip.md` → `NginxSuspiciousIPActivity`
  - `error-log-spike.md` → `NginxErrorLogSpike`
  - `host-resources.md` → `HostHighCPU` (cubre también memoria y disco)

- [x] Script de seed: `rag/scripts/seed.py` (parsea frontmatter → embed → upsert)

### Validación

- [x] Qdrant accesible en `http://localhost:6333/readyz`
- [x] Colección `incidentes` creada y poblada con `scripts/init_collections.py` + `scripts/seed.py`
- [x] `embed()` retorna vector de 512 dimensiones
- [x] `query_incidents()` retorna resultados con score + clasificación documentado/nuevo
- [x] Umbral 0.75 validado con `scripts/query_demo.py`

---

## Sprint 4 — Orquestación n8n + Claude API

**Objetivo:** Implementar el motor de orquestación de Argos que, ante un trigger por umbral,
ejecute el flujo completo: recuperación de contexto mediante RAG, consulta a Claude API para
generar síntesis + pasos de resolución, y notificación al equipo por email y Microsoft Teams.

Entregable: flujo end-to-end documentado, probado, con notificaciones operativas.

**Estado:** ⏳ No iniciado | 0%

### Infraestructura / Docker

- [ ] Agregar servicio `n8n` a `docker/monitoring/docker-compose.yml`
  - Imagen: `n8nio/n8n:latest` o versión pinneada
  - Puerto: 5678 (UI + API)
  - Variables de entorno:
    - `N8N_ENCRYPTION_KEY` (32+ caracteres aleatorios, en `.env`)
    - `N8N_HOST=localhost` (desarrollo) o FQDN (producción)
    - `N8N_PORT=5678`
    - `NODE_ENV=production` (o development)
  - Volumen: `n8n_data` (workflows, credenciales)
  - Red: `argos-net`
  - Health check: `curl http://n8n:5678/healthz`

- [ ] Base de datos n8n (opciones):
  - **Opción A:** SQLite (included, simple para desarrollo)
  - **Opción B:** PostgreSQL dedicado (recomendado para producción)
    - Si usar PostgreSQL: agregar servicio `postgres` a docker-compose

- [ ] Variables de entorno en `.env.example`:
  - `N8N_ENCRYPTION_KEY=` (generar con `openssl rand -base64 24`)
  - `N8N_HOST=localhost`
  - `N8N_PORT=5678`
  - `ANTHROPIC_API_KEY=` (CLV de Anthropic)
  - `QDRANT_URL=http://qdrant:6333`
  - `WEBHOOK_URL_N8N=http://localhost:5678/webhook/argos-alerts` (para Alertmanager)

### LLM: Claude API Integration

- [ ] Configurar `ANTHROPIC_API_KEY` como obligatoria en `.env`
- [ ] Crear archivo de credenciales n8n:
  - Nombre: "Claude API"
  - Tipo: REST API
  - URL base: `https://api.anthropic.com/v1`
  - Header: `x-api-key: ${ANTHROPIC_API_KEY}`
  - Content-Type: application/json

- [ ] Seleccionar modelo Claude:
  - Recomendación: `claude-opus-4-8` (máxima capacidad) o `claude-sonnet-4-6` (balance velocidad/cost)
  - Actualizar si sale versión más nueva antes de Sprint 4

- [ ] Definir system prompt (rol de AIOps analyst):
  ```
  You are an AIOps analyst. When given alert context and historical incident data,
  synthesize the problem in clear technical terms and provide step-by-step resolution steps.
  Be concise, actionable, and reference the historical incidents if applicable.
  ```

### Alertmanager: Webhook Receiver

- [ ] Reintroducir receiver `webhook-n8n` en `alertmanager/alertmanager.yml`:
  - URL: `http://n8n:5678/webhook/argos-alerts` (ajustar según config n8n)
  - Method: POST
  - Send resolved: true
  - Headers: `Content-Type: application/json`

- [ ] Agregar ruta que envía al webhook:
  - Condición: cuando se supera el **umbral configurable** de alertas activas
  - Lógica: si `(count_of_loki_alerts > threshold)` → webhook

- [ ] Definir umbral de alertas que dispara el flujo:
  - Recomendación: 3+ alertas Loki activas simultáneamente
  - Hacer configurable vía `ALERT_THRESHOLD` en `.env`

### Workflow Principal n8n

**Workflow name:** `Alert Analysis & Resolution` (o similar)

- [ ] Node: HTTP Webhook — recibe POST de Alertmanager
  - Parsea payload: alertname, labels, annotations, status
  - Extrae datos relevantes (tipo de alerta, timestamp)

- [ ] Node: Condition — verifica si umbral superado
  - Si sí → continuar flujo
  - Si no → detener (no es incidente crítico)

- [ ] Node: RAG Query — consulta Qdrant
  - Input: resumen de alerta
  - Output: top-3 incidentes similares de knowledge base

- [ ] Node: Build Prompt — construye prompt para Claude
  - Datos de entrada:
    - Alerta actual (tipo, severity, labels)
    - Contexto RAG (incidentes similares previos)
  - Formato: "Alert: [X]. Historical context: [Y]. Propose resolution steps."

- [ ] Node: Claude API Call
  - Modelo: `claude-opus-4-8` (o sonnet)
  - System prompt: (AIOps analyst)
  - User message: (prompt construido)
  - Max tokens: 1024
  - Manejo de errores: retry x3 con backoff exponencial, rate limits

- [ ] Node: Parse Response
  - Extrae síntesis del problema
  - Extrae pasos de resolución (lista)
  - Valida formato (JSON o regex)

- [ ] Node: Notification — Email
  - Destinatario: `${ALERT_RECIPIENT_EMAIL}` (de `.env`)
  - Asunto: `[ALERTA] ${alert_name} — ${timestamp}`
  - Body: Síntesis + pasos de resolución (HTML formateado)
  - SMTP: (reuse config de Alertmanager)

- [ ] Node: Notification — Microsoft Teams (opcional en Sprint 4, recomendado)
  - Webhook Teams del channel `#alerts-argos`
  - Payload: card con título, descripción, pasos

- [ ] Node: Error Handling
  - Fallback si Claude API falla: enviar alerta genérica + logs
  - Log errors a archivo o servicio externo

### Validación End-to-End

- [ ] n8n accesible en `http://localhost:5678`
- [ ] Workflow creado y deployado
- [ ] Credencial Claude API configurada y validada
- [ ] Simular alerta superando umbral:
  - Generar alerta Loki test (`promtool` o query manual)
  - Verificar que Alertmanager envía webhook
  - Verificar que n8n recibe y procesa
  - Verificar que Claude API es consultada (ver logs n8n)
  - Verificar que email/Teams notificación es enviada
  - Verificar que respuesta es coherente y contiene pasos

---

## Sprint 5 — Auto-documentación + Dashboard propio de Argos

**Objetivo:** Implementar el mecanismo de auto-documentación: cuando el RAG no encuentre
coincidencias suficientes, Argos genera la documentación técnica mediante LLM y la persiste
simultáneamente en la base vectorial y en el dashboard propio. Desarrollar el dashboard central
de Argos con historial de incidentes, métricas y base de conocimiento.

Entregable: auto-documentación operativa, documentos visibles en dashboard, métricas de MTTR
calculadas.

**Estado:** ⏳ No iniciado | 0%

### Auto-Documentación en n8n

- [ ] Agregar condición al workflow principal:
  - Si `similarity_score < threshold` (ej: < 0.75) → incidente nuevo
  - Activar rama "auto-documentación"

- [ ] Nodo Claude: Generar documento técnico
  - Prompt especial: "Generate a concise technical documentation (title, description, root cause,
    resolution steps, prevention). Format as markdown."
  - Input: alerta + contexto (logs, métricas relevantes de Prometheus/Loki)
  - Output: documento técnico completo

- [ ] Parsear documento generado
  - Extrae: título, descripción, causa raíz, pasos, prevención

- [ ] Persistir en Qdrant (knowledge base)
  - Generar embedding del documento
  - Insertar en colección `incidentes` con payload enriquecido
  - Marcar como `fuente: "auto-generado"` y `estado: "documentado_recientemente"`

- [ ] Persistir en store del dashboard (API backend)
  - Hacer POST a `/api/incidents` (endpoint nuevo, ver abajo)
  - Incluye: tipo_alerta, resumen, documento_completo, fecha, estado, MTTR estimado

### Persistencia de Incidentes (Backend)

- [ ] Elegir store de persistencia:
  - **Opción A:** PostgreSQL + pgvector (recomendado si ya se usa Postgres en n8n)
    - Servicio `postgres` en docker-compose (agregado en Sprint 4 si aplica)
    - Tabla `incidents`: id, alert_type, summary, full_doc, created_at, status, mttr_minutes
    - Tabla `incident_vectors`: incident_id, embedding (pgvector)
  - **Opción B:** Qdrant + API adicional (si solo Qdrant)
    - Usar Qdrant como fuente única (ya guarda todo)
    - Implementar endpoint que querya Qdrant

- [ ] Esquema de incidente:
  ```
  {
    id: UUID,
    alert_type: string (NginxHighErrorRate, HostHighCPU, etc.),
    summary: string (resumen corto),
    full_document: string (markdown),
    detected_at: ISO8601 timestamp,
    resolved_at: ISO8601 timestamp (nullable, si aún no resuelto),
    status: enum (documented_previously | new | auto_documented),
    mttr_minutes: number (timestamp_resolved - timestamp_detected),
    source: enum (manual | auto_generated),
    qdrant_id: string (ID en Qdrant si aplica)
  }
  ```

- [ ] Script de init BD (si PostgreSQL):
  - Crear tablas
  - Crear índices (alert_type, created_at)
  - Crear volumen y health check

### API .NET: Endpoints nuevos

**Base path:** `/api/incidents` (o `/api/argos/incidents`)

- [ ] `POST /api/incidents`
  - Recibe: incident object (ver schema arriba)
  - Almacena en DB
  - Retorna: `201 Created` con incident creado

- [ ] `GET /api/incidents`
  - Query params: `?pageNumber=1&pageSize=20&status=auto_documented&alertType=NginxHighErrorRate`
  - Retorna: lista paginada de incidentes + total count
  - Ordenado por `created_at DESC`

- [ ] `GET /api/incidents/{id}`
  - Retorna: incidente completo con documento

- [ ] `PATCH /api/incidents/{id}`
  - Recibe: campos parciales (ej: `resolved_at`, `status`)
  - Actualiza en DB

- [ ] `GET /api/incidents/metrics`
  - Retorna: métricas agregadas
    ```
    {
      total_incidents: number,
      by_status: { documented_previously: N, new: N, auto_documented: N },
      by_alert_type: { NginxHighErrorRate: N, HostHighCPU: N, ... },
      mttr_average_minutes: number,
      incidents_per_day: [{ date, count }, ...],
      recent_trend: "improving" | "stable" | "degrading"
    }
    ```

- [ ] `GET /api/incidents/knowledge-base`
  - Retorna: lista de documentos auto-generados (knowledge base pública)
  - Query: `?search=error&page=1&pageSize=20`
  - Retorna: resúmenes (sin documento completo para brevedad)

### Dashboard de Argos (Frontend)

**Tecnología:** Blazor .NET o React/Next.js (elegir según preferencia)
**Puerto:** 3001 o integrado en la API (ej: carpeta `/wwwroot` en .NET)

- [ ] Página: Historial de incidentes
  - Tabla: alertname, fecha, resumen, pasos, estado (badge: documentado-previamente /
    nuevo / auto-documentado)
  - Filtros: por tipo de alerta, rango de fechas, estado
  - Búsqueda por palabra clave
  - Detalles: click en fila → modal con documento completo

- [ ] Página: Métricas agregadas
  - Card: total de incidentes
  - Card: MTTR promedio (minutos)
  - Card: incidentes por tipo (top 5, gráfico de barras)
  - Gráfico: tendencia temporal (incidentes por día, últimos 30 días)
  - Indicador: "Trending: ↑ empeorando" o "↓ mejorando"
  - Breakdown por estado (documentado-previamente / nuevo / auto-documentado)

- [ ] Página: Base de conocimiento
  - Listado de documentos auto-generados (solo lectura; escritura = auto-doc exclusively)
  - Búsqueda + filtros por alert type
  - Click en doc → detalle (markdown renderizado)

- [ ] Navbar/Menu:
  - Home (métricas)
  - Incidents (historial)
  - Knowledge Base (docs)
  - Settings (opcional: umbrales, alertas, integraciones)

### Validación

- [ ] Crear incidente de prueba con `POST /api/incidents` y verificar que aparece en historial
- [ ] Simular alerta "nueva" (score RAG < 0.75):
  - Verificar que se genera documento en n8n
  - Verificar que se guarda en DB
  - Verificar que aparece en dashboard con estado "auto_documented"
  - Verificar que está disponible en "Knowledge Base"
- [ ] Consultar `/api/incidents/metrics` y validar cálculos (MTTR, conteos, tendencia)
- [ ] Dashboard carga y renderiza datos correctamente
- [ ] Búsqueda en dashboard funciona

---

## Sprint 6 — Simulación k6 + Validación + Cloud Deployment

**Objetivo:** Ejecutar escenarios con k6 (tráfico normal, picos, patrones de ataque), evaluar
detección y calidad, migrar infraestructura a AWS o GCP. Completar memoria académica con análisis
de resultados.

Entregable: evidencia de pruebas, análisis de resultados, infraestructura en cloud, memoria académica
completa.

**Estado:** ⏳ No iniciado | 0%

### k6 Load Testing

- [ ] Crear estructura de directorios:
  - `k6/tests/` — scripts de prueba
  - `k6/results/` — outputs JSON (ignorados por `.gitignore`)
  - `k6/data/` — datasets (URLs, payloads)

- [ ] Script 1: Tráfico normal
  - **Archivo:** `k6/tests/normal-traffic.js`
  - Duración: 5 minutos
  - Virtual users: 10 usuarios
  - Ramp-up: 1 min, steady: 3 min, ramp-down: 1 min
  - Endpoints:
    - `GET http://localhost/api/usuarios?pageNumber=1&pageSize=10` (60% del tráfico)
    - `GET http://localhost/api/status` (40% del tráfico)
  - Assertions: response time p95 < 500ms, error rate < 1%
  - Objetivo: verificar que el flujo Argos NO se dispara (falsos positivos)

- [ ] Script 2: Picos de tráfico (spike)
  - **Archivo:** `k6/tests/spike-traffic.js`
  - Duración: 5 minutos
  - Fases:
    - 0–1m: 10 usuarios (baseline)
    - 1–2m: 100 usuarios (spike repentino)
    - 2–3m: 10 usuarios (normalización)
    - 3–5m: mantener 10 usuarios
  - Endpoints: igual que normal-traffic
  - Assertions: p95 response time < 2000ms (relajado), error rate < 5%
  - Objetivo: pico temporal dispara alertas de latencia / conexiones altas → Argos propone escalado

- [ ] Script 3: Patrón de ataque simulado
  - **Archivo:** `k6/tests/attack-pattern.js`
  - Duración: 5 minutos
  - Fases:
    - 0–1m: tráfico normal (baseline)
    - 1–4m: ataque simulado:
      - 50 usuarios generan requests con patrones sospechosos (IPs rotativas, User-Agent vacío,
        requests a endpoints no existentes → 404s)
      - Objetivo: disparar alertas Nginx (error rate > 20%, IP sospechosa)
    - 4–5m: normalización
  - Assertions: error rate 1–50%, verificar que alertas se disparan
  - Objetivo: valida detección de anomalías y propuesta de Argos (ej: "bloquear IPs", "investigar
    patrón de 404")

### Ejecución de Tests

- [ ] Ejecutar scripts con k6:
  ```
  k6 run --out json=k6/results/normal-traffic.json k6/tests/normal-traffic.js
  k6 run --out json=k6/results/spike-traffic.json k6/tests/spike-traffic.js
  k6 run --out json=k6/results/attack-pattern.json k6/tests/attack-pattern.js
  ```

- [ ] Recolectar resultados:
  - Response times (p50, p95, p99)
  - Error rates
  - Throughput (requests/sec)
  - Duración de cada scenario

- [ ] Observar en Grafana durante pruebas:
  - Dashboard Nginx: conexiones, request rate, status codes, latencia
  - Dashboard Infrastructure: CPU, memoria, disk (si empieza a escasear)

- [ ] Registrar eventos:
  - Cuándo se disparan alertas Prometheus/Loki
  - Cuándo Alertmanager entra en acción
  - Cuándo n8n webhook recibe (timestamp en logs)
  - Cuándo Claude API retorna síntesis
  - Cuándo auto-doc es creada (si aplica)

### Cloud Deployment (AWS o GCP)

**Elegir entre AWS o GCP; recomendación: GCP por comodidad con Terraform.**

#### Opción A: Google Cloud Platform (GCP)

- [ ] Crear `terraform/environments/gcp/` con estructura:
  - `main.tf` — Compute Engine instance, VPC, firewall rules
  - `variables.tf` — region, instance_type, project_id
  - `outputs.tf` — instance IP, URLs
  - `providers.tf` — google provider v5.0+

- [ ] Configurar recursos GCP:
  - Proyecto en Google Cloud Console
  - Enable APIs: Compute Engine
  - Service account con credenciales JSON

- [ ] Terraform GCP config:
  - `google_compute_instance` (e2-standard-2 o similar, Ubuntu 22.04 image)
  - `google_compute_firewall` (allow 80, 443, 3000, 9090, 3100)
  - `google_compute_network` (VPC)
  - `local-exec` provisioner: SSH → provisionar 3 stacks Docker (igual que LocalStack)

- [ ] Reapuntar Docker compose (opcional):
  - Si se despliega en GCP VM, los servicios siguen siendo locales
  - Solo cambiar credentials y networking (cloud SQL si se usa, rara en este sprint)

#### Opción B: AWS

- [ ] Crear `terraform/environments/aws/` con estructura análoga a GCP
  - `main.tf` — EC2 instance, VPC, security groups
  - `providers.tf` — aws provider (real, no LocalStack)

- [ ] Configurar recursos AWS:
  - AWS credentials (Access Key / Secret) en `~/.aws/credentials` o variables
  - EC2 instance (t3.medium, Ubuntu 22.04 AMI)
  - VPC + subnet
  - Security group: ports 80, 443, 3000, 9090, 3100, etc.

- [ ] Terraform AWS config: similar a GCP

#### Deployment Process

- [ ] `terraform init -chdir=terraform/environments/gcp` (o aws)
- [ ] `terraform plan` — revisar cambios
- [ ] `terraform apply` — desplegar (5–10 min)
- [ ] Esperar a que la instancia esté lista y provisioned
- [ ] Verificar acceso:
  - `curl http://<cloud-instance-ip>/api/status`
  - `curl http://<cloud-instance-ip>:3000` (Grafana)
  - `curl http://<cloud-instance-ip>:9090` (Prometheus)

- [ ] Correr k6 tests contra cloud instance:
  ```
  k6 run --out json=k6/results/cloud-normal.json \
    -e BASE_URL=http://<cloud-ip> \
    k6/tests/normal-traffic.js
  ```

- [ ] Documentar resultados (latencias, throughput desde Internet)

### Validación de Detección y Calidad

- [ ] **Detección:** verificar que las 18 alertas (13 Prometheus + 5 Loki) se disparan apropiadamente:
  - Spike test: HostHighCPU, NginxHighRequestRate, NginxHighActiveConnections
  - Attack test: NginxHighErrorRate, NginxSuspiciousIPActivity, NginxCriticalErrorRate

- [ ] **Calidad de síntesis LLM:** (sample de outputs de Claude)
  - ¿Es relevante la síntesis respecto a la alerta?
  - ¿Son accionables los pasos propuestos?
  - ¿Hay alucinaciones o recomendaciones genéricas sin valor?
  - Scoring cualitativo: 1–5 estrellas (documentar en memoria)

- [ ] **Persistencia de auto-doc:**
  - Conteo: ¿cuántas docs auto-generadas en los 3 scenarios?
  - Verificar que están en Qdrant (query) + dashboard (GET /api/incidents)
  - Verificar que vector similarity permite recuperarlas después

### Análisis de Resultados

- [ ] Tabla resumen de k6 results:
  | Scenario | Duration | Users | p95 Response (ms) | Error Rate | Alerts Fired | Auto-Docs |
  |----------|----------|-------|-------------------|------------|--------------|-----------|
  | Normal | 5m | 10 | 150 | 0% | 0 | 0 |
  | Spike | 5m | peak 100 | 1200 | 3% | 4 | 0 |
  | Attack | 5m | 50 | 800 | 25% | 6 | 2 |

- [ ] Estimación de **MTTR reducción**:
  - Baseline (manual): tiempo promedio para investigar + resolver sin Argos
    - Asumir: 30 minutos (lectura de logs, análisis, comunicación)
  - Con Argos (asistido):
    - Tiempo para recibir síntesis + pasos: ~2 minutos (webhook → Claude → email)
    - Tiempo para ejecutar pasos: ~10 minutos (asumiendo solo validación manual)
    - **Total:** ~12 minutos
  - **Reducción:** ~60% (de 30m a 12m)

- [ ] Identificar limitaciones y líneas de mejora (para memoria académica):
  - ¿Qué falló? (ej: rate limits de Claude API, latencia del webhook)
  - ¿Qué mejoraría el MTTR? (ej: remediación automática, integración con Jira)
  - ¿Escalabilidad?

### Memoria Académica

- [ ] Documento: `memoria-final.md` o `.pdf` (según reglamento de la Facultad)
  - **Secciones:**
    1. Introducción — recapitulación del problema y propuesta
    2. Arquitectura implementada — componentes principales, decisiones técnicas
    3. Resultados de pruebas — tablas, gráficos, análisis cuantitativo
    4. Análisis de MTTR — baseline vs con Argos
    5. Limitaciones — qué quedó fuera, por qué
    6. Líneas de mejora — roadmap futuro (remediación automática, multi-tenancy, etc.)
    7. Conclusiones — impacto académico y técnico
    8. Referencias — papers, documentación, herramientas usadas

- [ ] Adjuntar:
  - Screenshots de dashboards
  - Ejemplos de síntesis Claude
  - Logs de k6 tests
  - Código versioned en GitHub

---

## Backlog Transversal / Deuda Técnica

Estos ítems son ortogonales a los Sprints 1-6 y conviene resolver temprano o paralelamente.

### Seguridad

- [~] **SQL Server password:** actualmente hardcodeado en `docker-compose.yml` → `.env`
  - `docker/apps/docker-compose.yml`: reemplazar `YourStrong@Passw0rd` con `${SQL_PASSWORD}`
  - `.env.example`: agregar `SQL_PASSWORD=` con instrucción de cambiar

- [~] **Alertmanager SMTP:** credenciales placeholders (`REPLACE_WITH_*`)
  - Reemplazar con variables de entorno: `${SMTP_USER}`, `${SMTP_PASSWORD}`, etc.
  - `.env`: cargar valores reales (o usar secretos en cloud)

- [~] **ANTHROPIC_API_KEY:** guardar en `.env`, NO en código ni logs
  - CI/CD: usar GitHub Secrets (cuando se implemente)

- [ ] **Grafana admin:** cambiar credenciales `admin/admin` en producción
  - `.env`: `GRAFANA_PASSWORD=` (generar con `openssl rand -base64 12`)
  - Script de provisioning: actualizar datasources como usuario no-admin si aplica

- [ ] **Nginx SSL:** certificados self-signed en desarrollo, cambiar en producción
  - Generar certificados válidos (Let's Encrypt o CA corporativo)
  - `docker/proxy/nginx/certs/` — actualizar paths

### Base de Datos

- [x] **Inicialización de MembresiaDB:** resuelta vía contenedor `db-init` en docker/apps
  - Script: `docker/apps/db/init-db.sql` (idempotente: CREATE OR ALTER, IF NOT EXISTS)
  - SP con dos result sets (usuarios + TotalRegistros) según contrato de `UsuarioRepository.cs`
  - Se ejecuta automáticamente en cada `docker compose up` y termina sin reiniciarse

- [ ] **Seed data:** cargar datos de prueba (usuarios, incidentes históricos para RAG)

### API .NET

- [ ] **Swagger/OpenAPI en producción:**
  - Actualmente: `AddOpenApi()` registrado pero no mapeado en Program.cs
  - Habilitar: `app.MapOpenApi()` en producción (controlar con env vars)
  - Accesible en `http://localhost:5000/openapi/v1.json` + UI swagger en `/swagger`

- [ ] **Instrumentación OpenTelemetry:**
  - Agregar paquetes: `OpenTelemetry.Api`, `OpenTelemetry.Exporter.Prometheus`,
    `OpenTelemetry.Instrumentation.AspNetCore`
  - Configurar exporters: Prometheus (para métricas custom) y tracing (opcionales)
  - Metrics: request count, latency, error rate por endpoint
  - Traces: para debugging (opcional en Sprint 4-6)

- [ ] **Logging estructurado:**
  - Reemplazar `Console.WriteLine()` con `ILogger<T>`
  - Agregar Serilog para logging estructurado a JSON (Loki puede parsearlo)
  - Niveles: Info (operativo), Warning (anomalías), Error (fallos)

### CI/CD

- [ ] **GitHub Actions workflow:**
  - `.github/workflows/build-test.yml`:
    - Trigger: push a main/master, PR
    - Jobs:
      1. Build: `dotnet build`
      2. Test: `dotnet test` (si hay tests)
      3. Docker build: `docker build -t aacosta/argos-api:${GIT_SHA} .`
      4. Push image: a Docker Hub (con credenciales GitHub Secret)
  - `.github/workflows/deploy.yml`:
    - Trigger: release tag (ej: `v1.0.0`)
    - Deploy a cloud (GCP/AWS) con Terraform

- [ ] **Code quality checks (opcional):**
  - SonarQube o similar
  - Linter (StyleCop)

### Documentación

- [ ] **README.md:** mantener actualizado conforme avanzan los Sprints
  - Instrucciones de despliegue local
  - Cloud deployment
  - Troubleshooting

- [ ] **CLAUDE.md:** refleja el estado actual (roadmap + instrucciones técnicas)
  - Actualizar tras cada Sprint

- [ ] **Swagger/OpenAPI:** documentación automática de API (en Swagger UI)

---

## Dependencias entre Sprints

```
Sprint 1 (Infra base)
    ↓
Sprint 2 (Observabilidad + alertas) ← **Bloqueante: init de BD**
    ↓
Sprint 3 (Qdrant + RAG)
    ↓ (Sprint 3 es prereq de 4)
Sprint 4 (n8n + Claude API) ← **Consume salida de Sprint 3 (RAG)**
    ↓
Sprint 5 (Auto-doc + dashboard) ← **Consume salida de Sprint 4 (flujo completo)**
    ↓
Sprint 6 (k6 + cloud) ← **Valida todos los sprints anteriores**
```

**Nota:** Backlog transversal (seguridad, BD init, CI/CD) puede ejecutarse en paralelo o temprano.

---

## Próximas Acciones

1. ~~**Inmediato:** Resolver deuda de Sprint 2 — init-db.sql~~ ✅ Resuelto (db-init container)
2. ~~**Sprint 3:** Desplegar Qdrant, validar conexión, crear colecciones~~ ✅ Completo (`rag/`)
3. **Sprint 4:** Desplegar n8n, crear workflows, integrar Claude API, webhook Alertmanager
4. **Sprint 5:** Implementar auto-doc en n8n, endpoints API nuevos, dashboard
5. **Sprint 6:** Ejecutar k6 tests, desplegar en cloud, redactar memoria
