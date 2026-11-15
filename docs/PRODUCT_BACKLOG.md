# Argos: Product Backlog / Plan de Evolución del Trabajo

**Proyecto:** Argos — Plataforma inteligente de AIOps  
**Alumno:** Juan Agustín Acosta (LU 57851)  
**Profesor Orientador:** Rios, Leopoldo Jose  
**Fecha de creación:** 2026-08-04  
**Última actualización:** 2026-11-12  
**Versión:** 1.6  

---

## Propósito de este documento

Este es el **product backlog operativo** del proyecto Argos. Complementa la propuesta académica formal
(`docs/Anexoll_Acosta_Juan_Agustin_V2.docx.md`, Anexo II) que define el alcance y las fases; este
documento es el **registro vivo de qué está hecho, qué falta y cómo se desglosa en subtareas** por
sprint, con estado marcado.

Se mantiene actualizado conforme avanza la implementación y sirve como base para la toma de
decisiones, estimaciones y seguimiento de la deuda técnica.

> **Tablero visual (2026-11-09):** el seguimiento día a día de los PBI se movió a un **GitHub Project
> en formato Scrum** (no Kanban): https://github.com/users/agusacost/projects/2 — un Issue por PBI (50
> en total: uno por cada sección `###` de este documento + los hitos de Sprint 6 registrados en
> `CLAUDE.md` que todavía no estaban volcados aquí). Implementa los **3 principios del "Scrum
> Adaptado" del Anexo II** (`docs/Anexoll_Acosta_Juan_Agustin_V2.docx.md`, sección II):
> 1. **Entregas incrementales (slicing horizontal):** cada PBI pertenece a un Sprint, agrupado por el
>    componente técnico que habilita.
> 2. **Timeboxing:** el campo `Sprint` es un campo **Iteration** real (no una etiqueta/categoría),
>    con fecha de inicio y duración propia por Sprint — Sprint 1–2 estimados (previos a la migración a
>    este repo, bundleados en el commit del 2026-08-03), Sprint 3–6 con las fechas reales del changelog
>    de `CLAUDE.md`. GitHub calcula solo "Sprint 6" como la iteración **activa** (hoy cae dentro de su
>    rango) y el resto como completadas — refleja, de paso, que el timeboxing planeado (2–3 semanas,
>    Tabla 1 del Anexo II) no se cumplió en varios Sprints (Sprint 5 duró 42 días, Sprint 6 lleva 60+ y
>    sigue abierto por el despliegue en AWS real y el cierre de la memoria), dato honesto para la sección de limitaciones.
> 3. **Definition of Done verificable:** campo `Status` (Todo/In Progress/Done) por PBI, con el
>    entregable concreto en el cuerpo de cada issue.
>
> Los 6 ítems de **deuda técnica transversal** (seguridad, BD, API .NET, CI/CD, docs) no tienen
> Sprint asignado a propósito: en Scrum son Product Backlog Items sin "pull" a ningún Sprint todavía,
> no un sprint propio (el Anexo II no define un "Sprint Transversal").
>
> Campo adicional **Category** (Infra/RAG/LLM/Dashboard/etc.) para filtrar por área técnica. Labels
> `sprint-1`…`sprint-6`/`transversal` en el repo. Este `.md` sigue siendo la referencia narrativa y de
> detalle técnico (gotchas, validaciones, decisiones); el board es la vista operativa del Sprint
> actual y del historial de Sprints cerrados.

---

## Estado actual (Snapshot a 2026-11-12)

| Sprint | Objetivo | Entregable esperado | Estado | % |
|--------|----------|----------------------|--------|-----|
| **1** | Infraestructura base (Terraform/LocalStack + Docker stacks) | Flujo end-to-end Nginx → Alloy → Loki → Grafana | ✅ Hecho (Terraform rehecho en noviembre: módulo + `localstack`/`aws`) | 100% |
| **2** | Stack observabilidad + reglas de detección | Tableros + alertas (18 reglas) + Alloy | ✅ Hecho | 100% |
| **3** | Base de conocimiento vectorial + RAG | Qdrant + pipeline RAG funcional | ✅ Hecho | 100% |
| **4** | Orquestación n8n + LLM (Gemini/Claude) | Workflows → análisis IA + notificaciones | ✅ Validado (Fase 1 + cadena real vía k6) | 100% |
| **5** | Auto-documentación + dashboard Argos | Dashboard propio + docs auto-generadas | ✅ Validado E2E | 100% |
| **6** | Simulación k6 + validación + cloud | k6 scenarios + evaluación OE3 + IaC portable a AWS | 🔄 Casi cerrado: falta aplicar en AWS real | ~90% |

**Próximo hito:** cerrar Sprint 6. Hecho: k6 (3 escenarios + `flood`, también on-demand desde el panel), evaluación
del OE3 (cerrada el 2026-10-28, `docs/EVALUACION_OE3.md`), CI con GitHub Actions y la infraestructura como código
portable (módulo `terraform/modules/argos-node` con `environments/localstack` y `environments/aws`, verificada contra
el simulador). **Restante:** (1) **aplicar el entorno `aws` en una cuenta real** (no se hizo: requiere credenciales y
tiene costo; el arranque por cloud-init en una VM real no está probado), (2) estudio de MTTR con personas (la línea base
manual no se midió: limitación declarada), (3) repetir la suite E2E del panel completa con memoria disponible,
(4) subir los cambios para que el CI corra en GitHub y fusionar `feat/dashboard-nextjs` en `master` (la rama principal
todavía tiene el panel en Vite). Ver Sprint 6 abajo.

**Pool de destinatarios de alertas (2026-10-12):** el email de alertas de n8n dejó de ir a una única
dirección fija por env (`ALERT_RECIPIENT_EMAIL`): ahora se envía a un **pool de emails configurable
desde el dashboard** (Configuración, solo Admin), persistido en Qdrant. **Validado E2E el 2026-10-14** con
`recipients_source=pool`. Ver Sprint 6 → "Pool de destinatarios de alertas".

**Configuración funcional (2026-10-12):** todas las opciones del panel Configuración pasaron a tener efecto real
(LLM, umbral, auto-doc, polling) y los switches de Integraciones se reemplazaron por estado en vivo.
Ver Sprint 6 → "Configuración funcional del dashboard".

**Puente Sprint 3→4:** El RAG se expone como API HTTP (Argos API, `argos-api/` en .NET 10, servicio
`argos-api` con alias `rag-api` en el compose de monitoring) para que n8n pueda consultarlo vía
`POST http://rag-api:8000/query`.

**LLM (estado actual):** n8n ya no llama a los proveedores: llama a `POST rag-api/internal/llm/generate`, que
recorre la cadena principal (settings) → `LLM_FALLBACK_CHAIN` (Gemini, Groq, OpenRouter; Anthropic se puede elegir
como principal si hay una key válida). El principal vigente es `gemini-flash-lite-latest`. El diseño original
(`LLM_PROVIDER` + nodos `Route LLM`/`Parse` en n8n, descripto en el Sprint 4) se reemplazó el 2026-10-26; ver
Sprint 6 → "LLM con fallback".

---

## Leyenda de estados

- `[x]` — Completado / funcional
- `[ ]` — Pendiente / no iniciado
- `[~]` — Parcial / requiere configuración o tiene bloqueante menor

---

## Sprint 1 — Infraestructura base (IaC + Docker)

**Objetivo:** Provisionar la infraestructura mediante Terraform sobre LocalStack (edición comunitaria; los servicios que
usa el módulo no requieren Pro), orquestando
los stacks Docker interconectados. Entregable: flujo end-to-end Nginx → Alloy → Loki → Grafana
operativo.

**Estado:** ✅ Completo (con residuales de seguridad)

### Terraform / LocalStack

- [x] Configurar provider AWS v5.0+ hacia `http://localhost:4566` (LocalStack endpoint)
- [x] Definir `aws_instance.observability` (EC2 simulada, t3.medium, tag `argos-node`) — *versión original de
  octubre; reemplazada en noviembre por el módulo `argos-node` (ver Sprint 6 → "Cierre de noviembre")*
- [x] ~~Script PowerShell `get_or_create_ami.ps1`~~ — **eliminado** (solo funcionaba en Windows); la AMI es la variable
  `ami_id`
- [x] ~~`null_resource.provision` con los 3 stacks en secuencia~~ — **eliminado** junto con los scripts de la topología
  de tres nodos (`terraform/scripts/node*.sh`); el despliegue es el `docker-compose.yml` raíz
- [x] Crear red `argos-net` (bridge) con `docker network create`
- [x] Outputs Terraform: instance_id, private_ip, URLs (Grafana, Prometheus, Loki)
- [~] **Credenciales:** AWS test/test solo en `environments/localstack`; `environments/aws` usa la cadena estándar de
  credenciales de AWS

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

- [x] `terraform apply` ejecuta sin errores (en noviembre, contra LocalStack 3.8: 12 recursos creados y 12 destruidos; ver Sprint 6)
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

- [x] Pipeline Docker container logs (**activado el 2026-11-11**, antes comentado; **opt-in desde el 2026-11-15**)
  - Socket de Docker montado en solo lectura; solo contenedores con el label de Docker `argos.logs=true` (hoy `membresia-api`
    y `membresia-sqlserver`, declarado en `docker/apps/docker-compose.yml`). Los de la plataforma Argos quedan afuera
    (el 11/11 se recolectaban todos los `argos*`, sin `alloy` ni `loki`)
  - Etiquetas `job="docker"`, `container="<nombre>"` (no se mezclan con `service="nginx"`: las 5 reglas de Loki y los
    tableros no cambian). En Loki pasaron de 13 flujos a 2
  - Ver Sprint 6 → "Cierre de noviembre" (limitaciones y gotcha del primer arranque)

### Alertmanager: Routing y Receivers

**Archivo: `alertmanager/alertmanager.yml`**

- [x] Resolve timeout: 5m
- [x] SMTP TLS requerido: true
- [x] Routes por severity (critical → email-critical, warning → email-default)
- [x] Inhibición: alertas críticas suprimen warnings del mismo tipo
- [x] Receivers email (2): email-default, email-critical
- [~] SMTP credentials: placeholders (`REPLACE_WITH_*`) — **requiere configuración real**
- [x] Webhook receiver `webhook-n8n`: removido en la migración y **reintroducido en el Sprint 4**

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
- [x] Logs Nginx recolectados en Loki (verificar con query: `{service="nginx"}`); logs de contenedores con `{job="docker"}`

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

- [x] Variables de entorno (`docker/monitoring/.env.example` + `argos-api/Argos.Common/Configuration`):
  - `QDRANT_URL`, `COLLECTION_NAME`, `VECTOR_SIZE`, `DISTANCE`, `SIMILARITY_THRESHOLD`, `EMBEDDING_MODEL_DIR`

### Embedding Model

- [x] Modelo seleccionado: **`sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2` local (ONNX Runtime)**
  - Dimensión: 384
  - Métrica: cosine similarity
  - 100% local (sin API key); el modelo ONNX cuantizado se descarga en el build de la imagen (con checksum)

### Colecciones Qdrant

- [x] Colección `incidentes` definida en `IncidentRepository` (`EnsureCollectionAsync`)
  - Vector size: 384, distance: Cosine
  - Payload: tipo_alerta, resumen, pasos, fecha, estado, fuente

- [x] Creación idempotente de colecciones al arrancar la API (`StartupInitializer`)

### Pipeline RAG

- [x] `ITextEmbedder.Embed(texto)` → ONNX (MiniLM local) → `float[]` (384 dims)
- [x] `IIncidentRepository.SaveAsync(incident)` → embedding + upsert con ID determinístico UUID5
- [x] `IRagService.QueryAsync` (`POST /query`, `top_k=3` por defecto) → top-k con score + clasificación
- [x] Umbral de similitud: `SIMILARITY_THRESHOLD=0.63` (configurable en `.env`). **En settings: 0.68 desde
  2026-10-27**, por la evaluación del OE3 (`docs/EVALUACION_OE3.md` §3)
- [x] **Señal por nombre de alerta** (2026-11-11): `POST /query` acepta `alertname` y devuelve `match_reason`; un
  documento con solución del mismo tipo cuya similitud supera `RAG_NAME_MATCH_MIN_SCORE` (default **0.30**) cuenta como
  conocida aunque no llegue al umbral. Re-evaluación 7/7 (§6 de la evaluación)

### Carga Inicial de Conocimiento

- [x] 5 docs en `docker/monitoring/knowledge/` (hasta el 15/11 en `argos-api/Argos.Api/knowledge/`) (frontmatter YAML + markdown):
  - `nginx-5xx.md` → `NginxHighErrorRate`
  - `nginx-latency.md` → `NginxHighResponseTime`
  - `suspicious-ip.md` → `NginxSuspiciousIPActivity`
  - `error-log-spike.md` → `NginxErrorLogSpike`
  - `host-resources.md` → `HostHighCPU` (cubre también memoria y disco)

- [x] Seed al arrancar: `SeedService.SeedKnowledgeAsync` (parsea frontmatter → embed → upsert)

### Validación

- [x] Qdrant accesible en `http://localhost:6333/readyz`
- [x] Colección `incidentes` creada y poblada al arrancar la API
- [x] El embedder retorna vectores de 384 dimensiones
- [x] `POST /query` retorna resultados con score + clasificación documentado/nuevo
- [x] Umbral 0.63 validado con consultas a `POST /query`

---

## Sprint 4 — Orquestación n8n + LLM (Gemini/Claude)

**Objetivo:** Implementar el motor de orquestación de Argos que, ante un trigger por umbral,
ejecute el flujo completo: recuperación de contexto mediante RAG, consulta a un LLM (Gemini o Claude)
para generar síntesis + pasos de resolución, y notificación al equipo por email.

Entregable: flujo end-to-end documentado, probado, con notificaciones operativas.

**Estado:** ✅ Completo | 100% (Fase 1 validada 2026-08-11; Fase 2 / cadena real cerrada vía k6 2026-09-30)

> **Nota (2026-11):** esta sección describe el diseño original. Lo que cambió después: los nodos `Route LLM`/`Parse`/
> `Route Doc LLM` ya no existen (n8n llama a `/internal/llm/generate`); el destinatario sale del pool configurable y no solo
> de `ALERT_RECIPIENT_EMAIL`; se agregaron `Get Config`, `Auto Doc?`, `Resolved?` y `Get Evidence`.

**Implementado y validado:** servicios `n8n` (SQLite, `argos-n8n`, **up + healthy**) y `rag-api`
(`argos-rag-api`, up) en el compose de monitoring; receiver `webhook-n8n` + ruta en Alertmanager;
workflow `docker/monitoring/n8n/workflows/alert-analysis.json` (webhook → umbral → RAG → **Route LLM**
→ **LLM Call** → Parse → email) **importado, con credencial SMTP adjunta y ACTIVO**; soporte
**dual-provider** (Gemini/Claude vía `LLM_PROVIDER`); `.env` completo (`N8N_ENCRYPTION_KEY`,
`LLM_PROVIDER=gemini`, `GEMINI_API_KEY`, `GEMINI_MODEL=gemini-flash-latest`, SMTP Gmail,
`ALERT_RECIPIENT_EMAIL`). **Validación Fase 1 OK** (2026-08-11): payload simulado → `status=success`,
email enviado. **Pendiente (opcional):** Fase 2 con alerta real vía Alertmanager.

### Infraestructura / Docker

- [x] Agregar servicio `n8n` a `docker/monitoring/docker-compose.yml`
  - Imagen: `n8nio/n8n:1.70.3` (pinneada), SQLite embebido
  - Puerto: 5678 (UI + API)
  - Variables de entorno:
    - `N8N_ENCRYPTION_KEY` (32+ caracteres aleatorios, en `.env`)
    - `N8N_HOST=localhost` (desarrollo) o FQDN (producción)
    - `N8N_PORT=5678`
    - `NODE_ENV=production` (o development)
  - Volumen: `n8n_data` (workflows, credenciales)
  - Red: `argos-net`
  - Health check: `wget -qO- http://localhost:5678/healthz` (contenedor `argos-n8n` reporta `healthy`)

- [x] Base de datos n8n — **Opción A elegida: SQLite** (`DB_TYPE=sqlite`, embebido, simple para
  desarrollo). PostgreSQL dedicado queda como opción de producción a futuro.

- [x] Variables de entorno en `.env.example` (`docker/monitoring/.env.example`):
  - `N8N_ENCRYPTION_KEY=` (generar con PowerShell `RandomNumberGenerator.GetBytes(32)` → hex)
  - `N8N_HOST=localhost`, `N8N_PORT=5678`
  - `LLM_PROVIDER=gemini` (`gemini` | `anthropic`)
  - `GEMINI_API_KEY=`, `GEMINI_MODEL=gemini-flash-latest`
  - `ANTHROPIC_API_KEY=`, `CLAUDE_MODEL=claude-sonnet-4-6`
  - `RAG_API_URL=http://rag-api:8000`
  - Webhook Alertmanager → n8n: `http://n8n:5678/webhook/argos-alerts`

### LLM: Provider Integration (Gemini | Claude)

- [x] Selector de proveedor `LLM_PROVIDER` (`gemini` | `anthropic`) en `.env`; default `gemini`.
- [x] Nodo `Route LLM` — arma URL/headers/body según el proveedor en runtime (no requiere credencial
  n8n dedicada: la API key viaja por header `x-api-key` (Anthropic) o query `?key=` (Gemini) desde
  `$env`).
- [x] Nodo `LLM Call` (HTTP Request) con retry x3 (`maxTries=3`, backoff `waitBetweenTries=2000`).
- [x] Nodo `Parse` — extrae el texto detectando el formato de respuesta por proveedor
  (`candidates[]` Gemini / `content[]` Anthropic).
- [x] Modelo por defecto: `gemini-flash-latest` (alias, o `gemini-pro-latest`); alternativa Claude
  `claude-sonnet-4-6` / `claude-opus-4-8`.
- [x] System prompt (rol de AIOps analyst) definido en el nodo `Build Prompt` (en español, síntesis +
  pasos accionables numerados, referencia al contexto RAG).
- [x] `GEMINI_API_KEY` verificada (HTTP 200 a `:generateContent` con `gemini-flash-latest`). Nota: la
  key `AQ.…` autentica bien; los pins `gemini-2.x-flash` devuelven 404 (retirados) → usar el alias
  `-latest`.

### Alertmanager: Webhook Receiver

- [x] Reintroducir receiver `webhook-n8n` en `alertmanager/alertmanager.yml`:
  - URL: `http://n8n:5678/webhook/argos-alerts`
  - Method: POST
  - Send resolved: true
  - Headers: `Content-Type: application/json`

- [x] Agregar ruta que envía al webhook:
  - Rutea las 5 alertas Loki de nginx + `severity=critical` a `webhook-n8n`
  - El conteo de umbral lo evalúa el nodo IF de n8n (`ALERT_THRESHOLD`), no Alertmanager

- [x] Definir umbral de alertas que dispara el flujo:
  - Configurable vía `ALERT_THRESHOLD` en `.env` (default 3); lo evalúa el nodo `Prepare` de n8n
    (dispara si `alerts.length >= threshold` **o** hay alguna `severity=critical`).

### Workflow Principal n8n

**Workflow name:** `Argos — Alert Analysis & Resolution`
**Archivo versionado:** `docker/monitoring/n8n/workflows/alert-analysis.json` (importar en n8n UI o
`docker exec argos-n8n n8n import:workflow --input=/workflows/alert-analysis.json`). Nodos abajo ya
implementados en el JSON; falta activarlo y validarlo.

- [x] Node: `Webhook Alertmanager` — recibe POST de Alertmanager (`path=argos-alerts`,
  `responseMode=onReceived`, 200)

- [x] Node: `Prepare` (Code) — verifica umbral y normaliza el payload
  - Dispara si `alerts.length >= ALERT_THRESHOLD` o hay `severity=critical`; si no, retorna `[]` y
    corta el flujo. Extrae summary, description, alertname, severity, alertsCount, status.

- [x] Node: `RAG Query` (HTTP) — `POST {RAG_API_URL}/query` con `{text, top_k:3}`
  - Output: top-3 incidentes similares + `is_documented` (score vs. umbral 0.63)

- [x] Node: `Build Prompt` (Code) — combina alerta + contexto RAG
  - Genera `system` (rol AIOps analyst, español) y `user` (alerta + histórico documentado)

- [x] Node: `Route LLM` (Code) — selecciona proveedor y arma la request (ver sección LLM arriba)

- [x] Node: `LLM Call` (HTTP) — `gemini-flash-latest` (o Claude); max_tokens 1024; retry x3 con backoff

- [x] Node: `Parse` (Code) — extrae el texto (multi-formato) y arma el HTML del email

- [x] Node: `Notify Email` (emailSend) — credencial SMTP (`SMTP account`, Gmail App Password)
  adjunta y probada (email enviado en Fase 1)
  - Destinatario: `${ALERT_RECIPIENT_EMAIL}`; asunto `[ALERTA] {alertname} — {severity}`
  - Body HTML: síntesis + pasos

- [ ] Node: Notification — Microsoft Teams — **diferido / opcional** (no implementado en el workflow)

- [x] Node: Error Handling — retry x3 en `LLM Call` y, desde el 2026-10-26, cadena de fallback entre proveedores
  (Sprint 6 → "LLM con fallback"). Si falla toda la cadena, el workflow igual manda el email con aviso y guarda el
  incidente como `nuevo`. Evidencia del 2026-11-11 (RNF-01): los dos proveedores con key fallaron por error de red,
  el incidente quedó `pendiente` y el correo salió con "Análisis IA no disponible".

### Validación End-to-End

- [x] n8n accesible en `http://localhost:5678`
- [x] Workflow importado en n8n (`n8n import:workflow --input=/workflows/alert-analysis.json`)
- [x] Acceso al LLM configurado (API key vía `$env`, sin credencial n8n dedicada)
- [x] Credencial SMTP creada en n8n (`SMTP account`, Gmail 587/STARTTLS) y **adjunta** al nodo
  `Notify Email` (referenciada en el JSON versionado para que el import la reconecte)
- [x] Workflow **activo** (webhook productivo `/webhook/argos-alerts` registrado)
- [x] **Fase 1** (aislar n8n): POST de payload Alertmanager simulado → ejecución `status=success`
  recorriendo Prepare → RAG Query → Route LLM → LLM Call (Gemini) → Parse → Notify Email; email
  enviado sin error (2026-08-11)
- [x] **Fase 2** (cadena completa): validada el 2026-09-30 con el escenario de ataque k6 → 4 alertas
  Loki en firing → Alertmanager rutea a `webhook-n8n` → n8n responde HTTP 200 y procesa el flujo
  (RAG → LLM → notificación). Ver `k6/RESULTS.md`.

---

## Sprint 5 — Auto-documentación + Dashboard propio de Argos

**Objetivo:** Implementar el mecanismo de auto-documentación: cuando el RAG no encuentre
coincidencias suficientes, Argos genera la documentación técnica mediante LLM y la persiste
simultáneamente en la base vectorial y en el dashboard propio. Desarrollar el dashboard central
de Argos con historial de incidentes, métricas y base de conocimiento.

Entregable: auto-documentación operativa, documentos visibles en dashboard, métricas de MTTR
calculadas.

**Estado:** ✅ Validado E2E | 100% (2026-09-23)

> **Decisiones de implementación (2026-08-11):**
> - **Persistencia = Qdrant como fuente única, vía `rag-api`** (no SQL Server / no Postgres+pgvector).
>   Los endpoints de incidentes/métricas se implementaron en la **Argos API** (`argos-api/`, servicio
>   `rag-api`), no en MembresiaAPI. La sección "API .NET: Endpoints nuevos" de abajo se **re-mapea a `rag-api`**
>   (`GET /incidents`, `GET /incidents/metrics`, `GET /incidents/knowledge-base`,
>   `GET /incidents/{id}`, `PATCH /incidents/{id}`). El `POST /upsert` ya existía (Sprint 3/4).
> - **Dashboard = panel propio en React + TypeScript** (`dashboard/`), servido por nginx (proxy
>   `/api` → `rag-api`, sin CORS). Servicio `argos-dashboard` (puerto 3001) en el stack monitoring.
>   Se hizo primero con Vite y el 2026-11-10 se migró a **Next.js** (App Router, export estático), como
>   indicaba la propuesta (Anexo II).
> - Modelo de incidente enriquecido en el payload Qdrant (`documento` markdown, `severity`,
>   `detected_at`, `resolved_at`, `mttr_minutes`, estados `nuevo`/`auto_documentado`).

### Auto-Documentación en n8n

- [x] Agregar condición al workflow principal:
  - Nodo `Is New?` (IF) sobre `is_documented === false` (score RAG < `SIMILARITY_THRESHOLD` 0.63)
  - Activa la rama "auto-documentación"

- [x] Nodo LLM: Generar documento técnico
  - `Build Doc Prompt` (prompt markdown: título, descripción, causa raíz, pasos, prevención) →
    `Route Doc LLM` + `Doc LLM Call` (dual provider Gemini/Claude, reusa el ruteo del flujo)
  - Input: alerta + `prep` (resumen, descripción, severidad, cantidad de alertas)
  - Output: documento técnico markdown

- [x] Parsear documento generado (`Parse Doc`)
  - Arma el payload de incidente con el markdown generado

- [x] Persistir en Qdrant (knowledge base) vía `POST rag-api/upsert` (`Persist Incident`)
  - `upsert_incident` genera el embedding e inserta en la colección `incidentes`
  - Marca `fuente: "auto-generado"`, `estado: "auto_documentado"`, `detected_at`

- [x] Persistir en el store del dashboard = **mismo Qdrant** (fuente única). El `/upsert` cumple
  ambos roles (KB semántica + historial del dashboard); no hay backend adicional.

### Persistencia de Incidentes (Backend)

- [x] Elegir store de persistencia: **Opción B — Qdrant como fuente única** (vía `rag-api`).
  - Descartadas Postgres+pgvector y SQL Server: Qdrant ya guarda todo; menor superficie y sin
    depender de un mecanismo de deploy de schema (el `db-init` que citan los docs no existe en el
    compose real).
  - Lectura/agregación implementada en `IncidentRepository` + `IncidentService` +
    `IncidentMetricsCalculator` usando `scroll`/`retrieve`/`set_payload` de Qdrant.

- [x] Esquema de incidente (payload Qdrant enriquecido):
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

- [x] Init del store = `ensure_collection` (Qdrant) ya existente; no hace falta script de BD relacional.

### API de incidentes (Argos API, servicio `rag-api`)

**Base path:** `/incidents` en `rag-api` (`IncidentsController`). Antes previsto en MembresiaAPI; se movió
a la Argos API por la decisión de persistencia Qdrant-only.

- [x] `POST /upsert` (rol de `POST /api/incidents`) — crea/actualiza incidente; `201 Created`.
  Enriquecido con `documento`, `severity`, `detected_at`, `resolved_at`, `mttr_minutes`.

- [x] `GET /incidents`
  - Query: `?page=1&size=20&estado=auto_documentado&tipo=NginxHighErrorRate&search=...`
  - Retorna: `{data, total, page, size}`; ordenado por `detected_at`/`fecha` DESC

- [x] `GET /incidents/{id}` — incidente completo con `documento` markdown

- [x] `PATCH /incidents/{id}` — campos parciales (`estado`, `resolved_at`, ...); recalcula `mttr_minutes`

- [x] `GET /incidents/metrics` — `total_incidents`, `by_status`, `by_alert_type`,
  `mttr_average_minutes`, `incidents_per_day`, `recent_trend`

- [x] `GET /incidents/knowledge-base` — estados `documentado_previamente` y `auto_documentado` (ver fix del 2026-09-23); `?search=&page=&size=`
  (resúmenes, sin el markdown completo)

### Dashboard de Argos (Frontend)

**Tecnología:** Next.js 16 (App Router, export estático) + React 19 + TypeScript (`dashboard/`), servido por nginx (proxy `/api` → `rag-api`).
**Puerto:** 3001 (servicio `argos-dashboard` en stack monitoring).

- [x] Página: Historial de incidentes (`/incidentes`)
  - Tabla: tipo_alerta, fecha, resumen, estado (badge), fuente
  - Filtros: por tipo de alerta y estado; búsqueda por palabra clave
  - Detalles: click en fila → modal con documento markdown renderizado (`react-markdown`)

- [x] Página: Métricas agregadas (`/metricas`)
  - Cards: total, MTTR promedio, tendencia, auto-documentados
  - Barras: incidentes por tipo (top 8, `recharts`); línea: incidentes por día
  - Indicador de tendencia (↑ empeorando / ↓ mejorando / → estable); breakdown por estado

- [x] Página: Base de conocimiento (`/conocimiento`)
  - Listado de runbooks manuales y documentos auto-generados; alta manual de runbooks (Admin) implementada en noviembre
    (F-06, ver `docs/FIXES_DASHBOARD.md`)
  - Búsqueda; click en doc → detalle markdown

- [x] Navbar/Menu: Métricas · Incidentes · Base de conocimiento (Settings: diferido/opcional)

### Validación

Validada E2E el 2026-09-23 (ver changelog de `CLAUDE.md`). Se sumó **auto-seed** al arrancar
(`SeedService` + `DemoIncidentCatalog`): 5 runbooks de `knowledge/` + 15 incidentes demo (idempotente, flags
`ARGOS_AUTO_SEED`/`ARGOS_SEED_DEMO`) para poblar dashboard y KB desde el primer `up`.

- [x] Crear incidente de prueba con `POST /upsert` y verificar que aparece en el historial
- [x] Simular alerta "nueva" (score RAG < 0.63):
  - [x] Se genera el documento en n8n (rama `Is New?`)
  - [x] Se guarda en Qdrant (`estado=auto_documentado`, `fuente=auto-generado`)
  - [x] Aparece en el dashboard con estado "auto_documentado"
  - [x] Está disponible en "Base de conocimiento"
- [x] Consultar `/incidents/metrics` y validar cálculos (MTTR, conteos, tendencia)
- [x] Dashboard carga y renderiza datos correctamente
- [x] Búsqueda en dashboard funciona

> **Fix clave (2026-09-23):** `GET /incidents/knowledge-base` filtra por
> `estados=[documentado_previamente, auto_documentado]` (antes filtraba por `fuente=auto-generado` y
> la KB quedaba siempre vacía).

---

## Sprint 6 — Simulación k6 + Validación + Cloud Deployment

**Objetivo:** Ejecutar escenarios con k6 (tráfico normal, picos, patrones de ataque), evaluar
detección y calidad, migrar la infraestructura a AWS (módulo Terraform portable; ver más abajo). Completar memoria académica con análisis
de resultados.

Entregable: evidencia de pruebas, análisis de resultados, infraestructura como código para la nube, memoria académica
completa.

**Estado:** 🔄 Casi cerrado | ~90% (k6 y evaluación del OE3 cerrados; IaC portable verificada contra LocalStack; **pendiente: aplicar en AWS real**, estudio de MTTR con personas y cierre de la memoria)

> **Validación k6 (2026-09-30):** 3 escenarios ejecutados en contenedor `grafana/k6` dentro de
> `argos-net` (target interno `http://nginx-proxy`, no localhost). Resultados completos en
> `k6/RESULTS.md`. Resumen: **Normal** p95=3.23ms, 0% err (sin falsos positivos); **Spike** 61% err
> por rate-limiting Nginx (503) con latencia plana; **Ataque** 28.454 reqs, ~74% 5xx → **4 alertas
> Loki en firing** → Alertmanager → webhook n8n (HTTP 200). La auto-doc NO disparó (los tipos de
> alerta ya tienen runbook en la KB → RAG los marca documentados), comportamiento esperado.
> Gotcha: las alertas Loki no aparecen en Prometheus `/api/v1/alerts`; verlas en el ruler Loki
> (`:3100/prometheus/api/v1/alerts`) y Alertmanager (`:9093/api/v2/alerts`).

### k6 Load Testing

- [x] Crear estructura de directorios:
  - `k6/tests/` — scripts de prueba
  - `k6/results/` — outputs JSON (ignorados por `.gitignore`)
  - `k6/data/` — datasets (URLs, payloads)
  - `k6/docker-compose.yml` (runner `grafana/k6` en `argos-net`), `k6/README.md`, `k6/RESULTS.md`

- [x] Script 1: Tráfico normal
  - **Archivo:** `k6/tests/normal-traffic.js`
  - Duración: 5 minutos
  - Virtual users: 10 usuarios
  - Ramp-up: 1 min, steady: 3 min, ramp-down: 1 min
  - Endpoints:
    - `GET http://localhost/api/usuarios?pageNumber=1&pageSize=10` (60% del tráfico)
    - `GET http://localhost/api/status` (40% del tráfico)
  - Assertions: response time p95 < 500ms, error rate < 1%
  - Objetivo: verificar que el flujo Argos NO se dispara (falsos positivos)

- [x] Script 2: Picos de tráfico (spike)
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

- [x] Script 3: Patrón de ataque simulado
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

- [x] Ejecutar scripts con k6 (runner containerizado, target interno `http://nginx-proxy`):
  ```powershell
  docker compose -f k6/docker-compose.yml run --rm k6 run --out json=/results/normal-traffic.json /tests/normal-traffic.js
  docker compose -f k6/docker-compose.yml run --rm k6 run --out json=/results/spike-traffic.json  /tests/spike-traffic.js
  docker compose -f k6/docker-compose.yml run --rm k6 run --out json=/results/attack-pattern.json /tests/attack-pattern.js
  ```

- [x] Recolectar resultados (throughput, p95, error rate por escenario → `k6/RESULTS.md`)
- [x] Registrar eventos de la cadena de detección (alertas Loki en firing → Alertmanager → webhook n8n HTTP 200)
- [~] Observar en Grafana durante pruebas (dashboards disponibles; captura para la memoria pendiente)

### Ejecución k6 on-demand desde la plataforma — ✅ HECHO (2026-10-05)

Acopla la ejecución de los escenarios k6 al dashboard, para dispararlos **en el momento exacto** que
se quiera y ver el código + los resultados sin consola.

- [x] `rag-api` lanza el contenedor `grafana/k6` como **contenedor hermano** vía Docker socket
  (`/var/run/docker.sock`) + `Docker.DotNet`. Trigger **solo rol Admin**; una sola corrida
  activa a la vez (409 si hay otra).
- [x] Script y resultados compartidos con el sibling vía **volúmenes nombrados** (`argos_k6_scripts`,
  `argos_k6_results`, con `name:` fijo en compose) — evita gotchas de rutas host en Windows.
- [x] Corridas persistidas en Qdrant (colección `argos_loadtests`): estado, timestamps, summary, logs.
- [x] Endpoints en `LoadTestsController`: `GET /loadtests/scenarios` (incluye código fuente),
  `GET/POST /loadtests/runs`, `GET /loadtests/runs/{id}`, `POST /loadtests/runs/{id}/stop`.
  Cierre de la corrida en segundo plano + parseo de `k6 --summary-export`. `LoadTestService`,
  `DockerK6ContainerRunner` y `K6FileWorkspace`.
- [x] Vista **"Pruebas de Carga"** (`dashboard/src/components/LoadTestsView.tsx`): cards de escenarios,
  botón Ejecutar (Admin), **visor de código con resaltado** (`react-syntax-highlighter`), resultados
  por polling (reqs, throughput, p95, error rate, checks) e historial de corridas.
- [x] Validación E2E (2026-10-05): corrida `normal` → `passed`, summary `reqs=1629, throughput=5.42,
  p95=3.45ms, error=0%, checks=100%`; contenedor efímero limpiado; 409/401 verificados.

> **Gotchas resueltos:** (1) `grafana/k6` corre como uid 12345 → lanzar el sibling con `user="root"`
> para que escriba el summary en el volumen (de root), si no `permission denied`. (2) En el JSON de
> `--summary-export`, las métricas **Rate** (`http_req_failed`, `checks`) guardan la fracción en la
> clave `value` (no `rate`); Trend usa `p(95)`/`avg`, Counter `count`/`rate` (rate = req/s).

### Observabilidad en el dashboard — ✅ HECHO (2026-10-07)

Acopla a Argos los **golden signals de Nginx** que hoy solo vivían en Grafana, previa evaluación para
**no duplicar** lo ya existente en Argos (logs por incidente en el drawer, salud de servicios, MTTR).
Decisión: implementar solo el hueco real (series temporales de Nginx); los logs se dejan a nivel
incidente (ya cubiertos) y los recursos de host/contenedores quedan como fase 2 opcional.

- [x] Endpoint `GET /observability/nginx-metrics?range=15m|1h|3h|6h` en `ObservabilityController` →
  `NginxMetricsService`. Reusa el cliente de Loki (`ILokiClient`). Devuelve,
  alineadas a una grilla temporal común: requests por status code (2xx/3xx/4xx/5xx), latencia upstream
  P50/P95/P99, request rate total y error rate 5xx (%).
- [x] Vista **Métricas**: nuevo panel `dashboard/src/components/NginxSignals.tsx` (gráfico de líneas
  multiserie SVG + selector de rango + mini-stats request rate / error 5xx; polling 20s). Cliente
  `getNginxMetrics` + tipo `NginxMetrics` en `dashboard/src/api.ts`.
- [x] **Fix "Salud de Servicios"** (bug): la tarjeta mostraba **todos los servicios como `unknown`**.
  Causa: `services_health()` consultaba `up{job="nginx"|"api"|"rag-api"|"qdrant"}`, pero Prometheus
  solo scrapea `nginx-exporter`/`node-exporter`/`cadvisor`/`prometheus` → esos jobs no existen.
  Solución: `services_health()` ahora hace **probes HTTP directos** sobre `argos-net` (nginx:8081,
  api:8080, qdrant:6333/readyz, rag-api self); `healthy` si responde `<500`, `degraded` si no. URLs
  configurables por env (`NGINX_STATUS_URL`, `DOTNET_API_URL`, `QDRANT_READY_URL`, `RAG_SELF_URL`). El
  sparkline `up` se conserva solo para Nginx (único con job real). La API .NET responde 404 en `/`
  (sin ruta raíz) → se interpreta como "proceso vivo" (healthy).
- [x] Validación E2E (2026-10-07): con tráfico generado, el endpoint devuelve request rate ~1 req/s,
  2xx/4xx poblados, latencia p95 ~2ms y error rate calculado; los 4 servicios reportan `healthy` (probe
  interno verificado: nginx 200, api 404, qdrant 200, rag-api 200). Funciona vía proxy del dashboard
  (`:3001/api/...`).

> **Gotcha Loki (`query_range`):** los samples se alinean a múltiplos **absolutos** del `step` (epoch),
> no al `start` de la consulta. La grilla temporal del backend debe alinearse igual
> (`start_ts = (int(start)//step)*step`); si arranca en un segundo arbitrario, el match por timestamp
> falla y **toda la serie sale en 0** aunque Loki tenga datos. Aplica a futuras queries de rango.

### Pool de destinatarios de alertas — ✅ HECHO (2026-10-12), validado E2E (2026-10-14)

**Problema:** el email con el análisis IA (nodo `Notify Email` del workflow n8n) se enviaba a **una sola
dirección**, `toEmail = $env.ALERT_RECIPIENT_EMAIL`; para cambiarla había que editar `.env` y recrear n8n.
**Objetivo:** definir un **pool de emails**, configurable desde la app y persistido, al que se le envía
cada alerta.

**Decisiones:**
- **Lista simple de emails**: todos reciben todo (sin filtro por severidad ni toggle por destinatario).
- Se guarda como el campo `alert_recipients: list[str]` del **documento único de configuración**
  (colección `argos_settings`), siguiendo el patrón Qdrant-only. No se crea una colección nueva y se
  reutilizan `GET/PATCH /settings`.
- n8n **consulta el pool en cada alerta** (no se cachea) → un cambio en el dashboard aplica a la próxima
  alerta, sin reiniciar nada. **Fallback** a `ALERT_RECIPIENT_EMAIL` si el pool está vacío o rag-api no
  responde, para no perder alertas.
- **Alcance:** solo el email de n8n (el canal real). El receiver `email-default` de Alertmanager no se toca.

**Implementación:**
- [x] `AppSettings.AlertRecipients` (default `[]`); `SettingsValidator.NormalizeRecipients()` (strip +
  lowercase + dedup conservando el orden, regex `^[^@\s]+@[^@\s]+\.[^@\s]+$`, tope 50, 422 con las
  direcciones inválidas) y `SettingsService.GetAlertRecipientsAsync()`.
- [x] Argos API:
  - `SettingsPatchRequest.alert_recipients`: `PATCH /settings` (solo Admin) normaliza la lista; si es
    inválida → **422** con el detalle. `[]` se guarda (vacía el pool → fallback).
  - Nuevo **`GET /notifications/recipients`** → `{recipients, count}`, sin JWT (lo consume n8n, igual que
    `/query` y `/upsert`). Si está definida la env **`ARGOS_INTERNAL_TOKEN`**, exige el header
    `X-Argos-Token` (si no coincide → 401).
- [x] Workflow `docker/monitoring/n8n/workflows/alert-analysis.json`: dos nodos nuevos antes de `Notify Email`
  (las dos ramas, `Is New?`→false y `Persist Incident`, pasan a llegar a `Get Recipients`):
  - **`Get Recipients`** (HTTP GET a rag-api con `X-Argos-Token`, `retryOnFail` 2×1s,
    `onError: continueRegularOutput` → un fallo no corta el flujo).
  - **`Resolve Recipients`** (Code): `to = pool.join(', ')` o, si está vacío, `$env.ALERT_RECIPIENT_EMAIL`;
    expone `recipients_source` (`pool`|`env`) para diagnóstico. Si no hay ningún destinatario, lanza un
    error visible en Executions.
  - `Notify Email.toEmail = {{ $json.to }}` (subject/html siguen leyendo de `$('Parse')`).
- [x] `docker/monitoring/docker-compose.yml`: `ARGOS_INTERNAL_TOKEN` en `argos-api` y `n8n`;
  `ALERT_RECIPIENT_EMAIL` queda en n8n como fallback. Documentado en `docker/monitoring/.env.example`.
- [x] Dashboard: `Settings.alert_recipients` en `dashboard/src/api.ts`; tarjeta **"Notificaciones por email"**
  en `SettingsView.tsx` (input + Enter/Agregar con validación, chips con ✕, contador N/50, aviso de
  fallback si está vacía, deshabilitada para no-Admin). Se persiste con el botón "Guardar configuración".
- [x] Verificado offline: `normalize_recipients` (dedup/lowercase, 422 si inválido, tope 51 → error),
  `npx tsc --noEmit` y `npm run build` OK, JSON del workflow válido y con las conexiones correctas.
- [x] Paso 5 validado (2026-10-14): pool = `juanagustinacost@gmail.com`, `juanagustinacosta@hotmail.com`; ejecuciones
  #30 (simulada), #31 (alerta real disparada por k6 `attack`), #36/#41 (rama auto-doc) → `recipients_source=pool`, SMTP
  aceptó ambos destinatarios. En el camino: fix de `attack` on-demand (`/data/payloads.json`) y de respuestas Gemini
  truncadas por thinking tokens (`maxOutputTokens` 4096/6144).
- [x] Validación E2E (2026-10-12): pasos 1–4 OK. Las ejecuciones n8n #26 y #28 terminaron `success`, con el workflow
  re-importado y activo, `Get Recipients` 200 y el email aceptado por SMTP vía **fallback** (`recipients_source=env`,
  porque el pool estaba vacío). El paso 5 (pool con direcciones reales) se cerró el 2026-10-14 (ver arriba).
  1. `docker compose up -d --build rag-api dashboard` (project `argos-platform`).
  2. `PATCH /settings` con duplicados/mayúsculas → se guarda normalizado; inválido → 422; Operador → 403.
  3. `GET /notifications/recipients` (y 401 si el token no coincide); el pool sobrevive a un
     `restart rag-api`.
  4. Re-importar el workflow: `docker exec argos-n8n n8n import:workflow --input=/workflows/alert-analysis.json`
     → verificar la credencial SMTP → **reactivar**.
  5. Payload simulado (Fase 1) → en Executions, `recipients_source=pool` y el email llega a todos.
  6. Pool vacío o `docker stop argos-rag-api` → llega solo a `ALERT_RECIPIENT_EMAIL` (`recipients_source=env`).

> **Posibles fallas / gotchas:**
> 1. **El workflow en n8n no se actualiza solo**: el JSON del repo es solo la fuente. Hay que re-importarlo,
>    verificar que la credencial SMTP (`m0MAUGNKirRpuLWP`) siga adjunta y **reactivarlo** (el import lo deja
>    `active:false`). Si no, n8n sigue enviando a `ALERT_RECIPIENT_EMAIL` como antes.
> 2. **rag-api caído o Qdrant no listo** → fallback a `ALERT_RECIPIENT_EMAIL`. Si esa env también está
>    vacía, la ejecución falla con "Sin destinatarios configurados" (se ve en n8n → Executions).
> 3. **Token desalineado** (`ARGOS_INTERNAL_TOKEN` distinto entre rag-api y n8n) → 401 → **fallback
>    silencioso** al env. Síntoma: el email llega solo a la dirección por defecto. Se diagnostica con
>    `recipients_source` en la ejecución.
> 4. **Límites de Gmail SMTP**: hasta 100 destinatarios por mensaje y ~500 por día. Ante una tormenta de
>    alertas con un pool grande se agota la cuota diaria (421/550). El tope de 50 solo cubre el primer límite.
> 5. **Un solo envío con varios To**: los destinatarios ven las direcciones de los demás, y si el servidor
>    rechaza el mensaje, no le llega a nadie. Una casilla inexistente rebota aparte y no bloquea al resto.
>    Mejora futura: BCC o un envío por destinatario.
> 6. **Validación solo sintáctica**: la regex no verifica que el buzón exista; un typo genera rebotes que
>    Argos no registra.
> 7. **Exposición**: sin `ARGOS_INTERNAL_TOKEN`, `/api/notifications/recipients` queda accesible vía el proxy
>    del dashboard (`:3001`). En cloud, el token es **obligatorio**.
> 8. **Concurrencia**: `update_settings` reescribe el documento entero → si dos Admins guardan a la vez, gana
>    el último (igual que el resto de la configuración). Además, el dashboard envía todos los settings al
>    guardar, así que una pestaña vieja puede pisar el pool.
> 9. **Fuera de alcance**: las alertas que Alertmanager enruta a `email-default`/`email-critical` (warnings
>    que no son de Nginx) **no** usan el pool y hoy no envían nada (placeholders `REPLACE_WITH_*`).

### Configuración funcional del dashboard — ✅ HECHO (2026-10-12)

**Problema:** el panel Configuración guardaba todo en `argos_settings`, pero nadie leía esos valores, salvo el
pool de emails. Proveedor/modelo LLM, umbral y auto-doc se tomaban de env; el polling estaba
hardcodeado (`POLL_MS`) y los switches de Integraciones no tenían efecto.

| Opción | Antes | Ahora (quién lo lee) |
|---|---|---|
| Proveedor / Modelo LLM | env `LLM_PROVIDER`/`*_MODEL` | `LlmService.Resolve(settings)` en la Argos API («Generar documentación») y n8n vía `GET /internal/config` (nodos `Route LLM`/`Route Doc LLM`). Al guardar se valida contra el proveedor: key inválida o modelo Gemini inexistente → 422 |
| Umbral de similitud | env `SIMILARITY_THRESHOLD` | `/query` lee settings → `is_documented` y `clasificacion` (n8n lo recibe sin cambios) |
| Auto-documentación | ignorado | nodo If `Auto Doc?` en n8n. Con OFF, `Build Pending Incident` persiste el incidente `estado=nuevo` sin doc y el email avisa «pendiente de documentar» |
| Notificaciones por email | ✅ ya funcionaba | `GET /notifications/recipients` |
| Integraciones | 4 switches decorativos | **estado en vivo**: `GET /integrations/status` (probes Prometheus `/-/ready`, Loki `/ready`, Alertmanager `/-/ready`, n8n `/healthz`), auto-refresh 30 s |
| Refresco automático | `POLL_MS=20000` fijo | `App.tsx` lee `polling_interval_seconds` y se aplica al guardar sin recargar |
| Seguridad | informativo | sin cambios (JWT 12 h, `JwtTokenService`) |

- [x] Backend: `SettingsValidator` (proveedor ∈ {gemini, anthropic}, umbral 0.30–0.95, polling 10–120 → 422),
  `llm_keys` de solo lectura en `GET /settings`, `ILlmProvider.CheckAsync` (listado de modelos del proveedor, sin costo de
  tokens), `GET /internal/config` (mismo token opcional `ARGOS_INTERNAL_TOKEN` que el pool), `GET /integrations/status`.
  La clave obsoleta `integrations` se descarta en `get_settings`.
- [x] `/query`: un match con `estado=nuevo` y sin documento no cuenta como conocimiento, para que un incidente pendiente
  no haga pasar por documentadas las alertas siguientes.
- [x] Workflow n8n: nodos `Get Config` (onError=continue → fallback env), `Auto Doc?`, `Build Pending Incident`;
  re-importado y activo.
- [x] Validación E2E: 422 en los cuatro rangos/valores inválidos; umbral 0.90 → `is_documented=false` y 0.63 → `true`;
  modelo que no corresponde al proveedor → se resuelve al default; auto-doc OFF → incidente `nuevo` + email
  «pendiente» (exec #26, success); auto-doc ON → `auto_documentado` (exec #28, success); provider=anthropic →
  n8n llamó a `api.anthropic.com` (exec #27), lo que **reveló que la `ANTHROPIC_API_KEY` del `.env` es inválida (401)**.
  Ahora el guardado lo bloquea.

> **Nota (2026-10-26):** la tabla refleja el estado del 2026-10-12. Desde entonces los proveedores son cuatro
> (Gemini, Anthropic, Groq, OpenRouter) y n8n no tiene nodos `Route LLM`; lee el proveedor/modelo vía `Get Config` y genera
> con `/internal/llm/generate`. El punto 2 de abajo ("esa alerta no envía email") ya no aplica: el fallback lo evita.

> **Posibles fallas / gotchas:**
> 1. **rag-api caído** → `Get Config` falla y n8n usa env para el LLM y asume auto-doc ON (sin error visible;
>    se ve en Executions).
> 2. **Key presente pero inválida**: `llm_keys` solo indica si está cargada. La validez se chequea al *cambiar*
>    el proveedor o el modelo; si la key se revoca después, el `LLM Call` falla y **esa alerta no envía email**
>    (comportamiento previo: el email depende de la respuesta del LLM).
> 3. **Alias de Anthropic**: si el modelo no aparece en `/v1/models` se guarda igual con un *warning* (puede ser un alias).
> 4. **Cambiar el umbral** reclasifica solo las alertas siguientes; los incidentes guardados no cambian.
> 5. **Cada cambio al workflow** exige re-importarlo y reactivarlo en n8n (`MSYS_NO_PATHCONV=1` en Git Bash).
> 6. **Polling**: se aplica en la sesión del Admin que guarda; las demás sesiones lo toman al volver a loguearse o recargar.

### Migración del dashboard a Next.js — ✅ HECHO (2026-11-10)

La propuesta (Anexo II, §V y §VI) indica un panel en **Next.js**; se había construido con Vite.

- [x] Next.js 16 (App Router) con `output: 'export'`: el mismo nginx sirve `out/`; se conservan el proxy
  `/api` y el bloqueo de endpoints internos (F-13). Dev: `next dev` con rewrite `/api` → `DEV_API_TARGET`.
- [x] Una ruta por vista (`/incidentes`, `/metricas`, …) con layout compartido; el estado global pasó de
  `App.tsx` a `src/app-shell/AppProvider.tsx` y la vista activa se deriva de la URL.
- [x] Sidebar con `<Link>` + `aria-current`; `/usuarios` redirige a un Operador; 404 propio.
- [x] E2E: nuevo `e2e/routing.spec.ts` (deep links, F5, atrás/adelante, login con deep link, filtro
  `?estado=`, 404) y suite adaptada. Resultado: **80 pasan, 4 fallan, 2 omitidos**; las 4 fallas
  (I-04, I-07, K-06, C-01) fallan igual con la versión Vite, no vienen de la migración.
- [x] React 18 → 19 (requisito de Next 16).

### LLM con fallback, salud de la IA y notificaciones — ✅ HECHO (2026-10-26)

Un `CAdvisorDown` real murió en n8n por 503/timeout de `gemini-flash-latest` sin dejar incidente ni aviso.

- [x] n8n llama a `POST rag-api/internal/llm/generate`: cadena principal (settings) → `LLM_FALLBACK_CHAIN`
  (default `gemini:gemini-flash-lite-latest,groq,openrouter`; los eslabones sin key se saltean). Proveedores nuevos
  `GroqProvider` y `OpenRouterProvider` (`OpenAiCompatibleProvider`).
- [x] Salud **pasiva** (`LlmHealthTracker`, en memoria): cada llamada real registra up/down; un fallo deja al eslabón en
  cooldown (`LLM_COOLDOWN_SECONDS`=300). Sin pings automáticos; "Probar ahora" manual (`POST /llm/health/check`, Admin).
  `GET /llm/health` alimenta el badge del header y la tarjeta "Estado de los modelos IA".
- [x] Notificaciones (campana, persistidas en `argos_notifications`) con `kind`: `incident`, `llm_health`, `pipeline_error`;
  click abre el incidente; `POST /notifications/read-all`.
- [x] Si falla toda la cadena, el workflow igual envía el email (con aviso) y guarda el incidente como `nuevo`.
- Gotcha: el chequeo de settings (`CheckAsync`) no detecta un modelo saturado (503 "high demand"); solo una generación real.

### Ciclo de vida de alertas y KPIs honestos — ✅ HECHO (2026-10-26)

- [x] n8n distingue `firing`/`resolved` (nodo `Resolved?`): una resolución no se analiza ni manda email; va a
  `POST /internal/incidents/resolve` → `resolved_at = endsAt` y MTTR real (`endsAt − startsAt`).
- [x] Alertas conocidas quedan como incidente `pendiente` con la solución copiada del mejor match
  (`POST /internal/incidents/occurrence`, sin llamar al LLM).
- [x] Cada ocurrencia = `tipo + startsAt` (`Incident.NaturalKey` incluye `detected_at`): una repetición de Alertmanager no duplica.
- [x] Fix RAG: el incidente se guarda con el mismo texto con el que se busca (`summary. description`).
- [x] Seed demo apagado (`ARGOS_SEED_DEMO=false`; los 15 demo se borraron). Dashboard: `nuevo` = "Sin documentar", KPI MTTR con
  `mttr_trend`/`mttr_per_day`, banner con estado real de la IA.

### Argos API en .NET 10 — ✅ HECHO (2026-10-19)

- [x] `argos-api/Argos.slnx` en capas Entities / Repository / Business / Controllers (+ Common), módulos Autofac, todo por
  interfaz. Es la **única API**: se eliminó la implementación Python. Detalle en `argos-api/README.md`; checklist de UI en
  `docs/VALIDACION_API.md`.

### Evaluación del OE3 — ✅ CERRADA (2026-10-28), con limitación declarada

Detalle y cifras en `docs/EVALUACION_OE3.md`; herramientas en `eval/`.

- [x] **Calidad del LLM** (rúbrica, 7 escenarios, 2 modelos; puntuación asistida por Claude, revisada por el autor):
  principal `gemini-flash-lite-latest` (`gemini-flash-latest` 0/17 por 503/timeout), `OPENROUTER_MODEL=nvidia/nemotron-3-ultra-550b-a55b:free`
  (8/8 OK). Prompts con el documento aprendido + contexto del stack: accionabilidad de 2,5 a 4,5 en alertas aprendidas.
- [x] **Clasificación RAG:** umbral 0.68 (6/7 sin falsos "conocida").
- [x] **Persistencia y aprendizaje:** F4 y F7, la 2ª ocurrencia se reconoce con ≥0.968 sin llamar al LLM.
- [x] **Detección y propuesta:** 10 corridas automáticas (`eval/run-auto.mjs`): propuesta en ~1,1 min, arreglo exacto 8/10.
- [ ] **Línea base manual de MTTR: no medida** (decisión del autor). No hay porcentaje de reducción; el estudio con
  operadores humanos queda como trabajo futuro (herramientas ya en `eval/`).
- Nota (2026-11-12): los MTTR de F1/F2 miden cuándo se apaga la alerta, no cuándo vuelve el servicio (defecto de Nginx, abajo).

### Cierre de noviembre (2026-11-11 y 12) — ✅ HECHO, con pendientes declarados

Origen: al contrastar la propuesta con el repositorio aparecieron compromisos flojos o incumplidos (Terraform casi
decorativo, sin CI, Alloy solo con logs de Nginx, el LLM sin ver logs, F6 clasificada como nueva con runbook, cinco
hallazgos abiertos del panel).

**Infraestructura como código (OE1)**
- [x] Módulo `terraform/modules/argos-node` consumido por `environments/localstack` y `environments/aws`; `main.tf`,
  `variables.tf` y `outputs.tf` idénticos (comprobado con `diff`), cambia `providers.tf` y `terraform.tfvars`.
- [x] El módulo crea VPC con subred pública, internet gateway y rutas; security group; EC2 Ubuntu 24.04 con gp3 cifrado de
  50 GB e IMDSv2 obligatorio; Elastic IP; y cuatro secretos con `random_password` (clave de n8n, JWT, token interno,
  contraseña del admin) si no se pasan. Instancia por defecto `t3.xlarge` (4 vCPU, 16 GiB): el límite real es la
  memoria (8 GiB no alcanzan).
- [x] Security group: solo 80 y 8443 abiertos a Internet; panel (3001), Grafana (3000) y SSH (solo con clave) limitados a
  `admin_cidr_blocks` (obligatoria y validada). Prometheus, Loki, Qdrant, n8n, Argos API y SQL Server no se exponen (túnel SSH).
- [x] Arranque por cloud-init: instala Docker, escribe el `.env` (modo 600), clona el repo, crea `argos-net`, levanta
  `docker compose -p argos-platform up -d --build` e importa y activa el workflow. Deja el comando `argos-deploy`.
- [x] `terraform fmt -check` y `terraform validate` en verde en los dos entornos; `apply` en LocalStack 3.8 (comunitaria):
  12 recursos creados y 12 destruidos (sobre una copia con state propio).
- [ ] **Aplicar en una cuenta real de AWS: NO hecho.** El arranque por cloud-init en una VM real no está probado.
- Limitaciones: en LocalStack el `user_data` no se ejecuta (por eso `deploy_local_stack`, apagado por defecto, despliega contra
  el Docker local); un segundo `terraform plan` nunca queda limpio contra LocalStack (no conserva las opciones de metadatos
  de IMDSv2); los secretos quedan en el `user_data` y en el state (corresponde un backend S3 cifrado para uso compartido).

**Integración continua**
- [x] `.github/workflows/ci.yml` (push y PR), seis jobs: `terraform` (fmt + validate), `observability-config` (`promtool`,
  `amtool`, sintaxis de Alloy, JSON del workflow de n8n), `compose` (`docker compose config`), `argos-api` (build + 102 pruebas
  xUnit de `argos-api/Argos.Tests`), `membresia-api` (build) y `dashboard` (`next build`). `actionlint` sin observaciones.
- Es integración continua, no despliegue continuo (no publica imágenes ni despliega). **El workflow todavía no corrió en
  GitHub** (los cambios no están subidos).

**Logs de aplicación (Alloy)**
- [x] Alloy recolecta por el socket de Docker solo los contenedores con el label `argos.logs=true` (opt-in desde el 15/11; el
  11/11 eran todos los `argos*`; ver Sprint 2). Limitaciones: el socket, aun en solo
  lectura, es un privilegio alto (en producción conviene un proxy del socket); Prometheus sigue sin métricas propias de las
  aplicaciones. En el primer arranque Loki rechazó logs anteriores a la retención de 7 días (10 errores en un segundo, sin
  repetición, sin efecto).

**Evidencia de logs, seudonimización de IP y señal por nombre (OE2)**
- [x] `POST /internal/evidence` + nodo `Get Evidence`: muestra acotada de Loki (18 líneas de hasta 200 caracteres) en el prompt.
  `ALERT_EVIDENCE_ENABLED=true`.
- [x] **Evidencia inferida desde Nginx (2026-11-15, decisión del autor):** la primera versión tenía escritos en Argos los
  contenedores de la aplicación de ejemplo (`AlertEvidencePlanner`) y Alloy recolectaba todos los `argos*`. Ahora la regla de
  alerta declara la fuente con el label `evidence` (`nginx` en las 5 de Loki y las 3 de tráfico de Prometheus; `target` en
  `NginxDown`, `NodeExporterDown`, `CAdvisorDown`, que usa el `instance`; sin label, sin evidencia). Con `nginx`, el upstream sale
  de `upstream_addr` y del error.log (`<nombre> could not be resolved` si el contenedor se detuvo) y `DockerContainerDirectory` lo
  traduce a contenedor (IP, nombre, servicio de Compose o alias) e informa su estado antes de pedir sus logs a Loki. La base de
  datos ya no se consulta. Alloy pasó a opt-in (`argos.logs=true`). n8n manda los labels; el botón "Generar documentación" del
  panel usa un criterio de respaldo (el incidente no guarda los labels). 94 pruebas unitarias en verde.
- Verificado el 15/11: F1, F2 y F7 con arreglo exacto (una corrida cada una, N = 1; `docs/EVALUACION_OE3.md` §6.5). Al agregar una
  regla nueva hay que decidir su label `evidence`.
- [x] **Runbooks fuera de la imagen de Argos (2026-11-15):** tres de los cinco runbooks nombraban contenedores, tablas y un
  procedimiento del servicio de ejemplo y viajaban dentro de la imagen de la API: era otra forma de que Argos conociera el
  servicio. Pasaron a `docker/monitoring/knowledge/` (montados en `/app/knowledge`, solo lectura); la imagen no trae ninguno y
  `DemoIncidentCatalog` quedó sin nombres del servicio. El runbook `nginx-5xx.md` incluía la contraseña `sa`, que se había
  copiado a los pasos de 16 ocurrencias de `NginxCriticalErrorRate` y entraba en los prompts enviados al LLM externo: se quitó
  del archivo y de los 17 documentos de Qdrant. Borrado el documento de prueba `DotnetMigrationE2E`. 102 pruebas unitarias en
  verde; RAG: 7 de 7 escenarios reconocidos.
- [x] **Contexto del entorno a configuración (2026-11-15, decisión del autor):** la descripción del entorno con los nombres de los
  contenedores (`membresia-api`, `membresia-sqlserver`...) estaba escrita en `Build Prompt`, `Build Doc Prompt` e
  `IncidentDocumentPrompt`. Ahora es el campo `environment_context` de `argos_settings`, editable en Configuración (solo
  Administrador, tope 3000 caracteres, 422 si se excede); n8n lo recibe en `Get Config` (`GET /internal/config`). Si nunca se
  editó se usa `ARGOS_ENVIRONMENT_CONTEXT` (el `.env.example` trae la descripción anterior como valor de ejemplo); vacío = prompt
  genérico. En el código quedó una instrucción genérica, igual en n8n y en la API. `run-llm-eval.mjs` toma el contexto de
  `/internal/config`. 102 pruebas unitarias en verde.
- Verificado el 15/11: compila, panel compila, desplegado; `GET /internal/config` devolvía el contexto vacío, se cargó por la API de
  configuración y n8n lo recibe (591 caracteres); el tope de 3000 devuelve 422. F1 y F7 con arreglo exacto y F2 parcial (diagnóstico
  correcto, `docker restart membresia-api`), N = 1 (`docs/EVALUACION_OE3.md` §6.7); la diferencia con la corrida anterior (F2
  exacto) no se atribuye al cambio. **No verificado:** el campo no se vio en un navegador (solo compila y pasa el chequeo de
  tipos); no se repitió la rúbrica de calidad con evaluador. Si el campo queda vacío o desactualizado pueden volver las propuestas
  genéricas (systemd, kubectl).
- [x] Seudonimización de IP en `LlmService` (`[IP-n]`, restaurada en la respuesta): `LLM_PSEUDONYMIZE_IPS=true`. Es
  seudonimización, no anonimización; ahora al LLM externo sale una muestra de logs, cosa que antes el Informe negaba.
- [x] Señal por nombre en el RAG (`alertname`, `match_reason`, piso `RAG_NAME_MATCH_MIN_SCORE=0.30`). Re-evaluación 7/7.
- [x] Defecto de Nginx corregido: guardaba la IP del upstream resuelta al arrancar; ahora resuelve por request con el DNS de
  Docker en los dos Nginx. Eso hizo innecesario reiniciar `argos-dashboard` tras recrear la API.
- Qué se probó y qué no: F1 y una corrida correcta de F2 (N = 1) con evidencia (12/11, versión anterior de la evidencia), y F1, F2
  y F7 con el diseño nuevo (15/11, N = 1 cada una); no se repitió la rúbrica con evaluador.

**Panel (hallazgos de `docs/FIXES_DASHBOARD.md`)**
- [x] Implementados F-02, F-03, F-04, F-05, F-06 y F-10; endpoints nuevos `POST /incidents/knowledge-base` y `DELETE /incidents/{id}`
  (Admin). **Suite E2E completa re-corrida el 15/11: 103 pruebas, 101 aprobadas, 0 fallidas, 2 omitidas** (S-06 e I-07 por
  falta de datos; I-07 sin confirmar). Encontró y se corrigió un defecto real en M-04 (latencia de Nginx sin agrupar: 400 de
  Loki en el rango de 6 h) y se sumaron tres pruebas de C-08. Detalle en `docs/FIXES_DASHBOARD.md`.

### Cloud Deployment (AWS) — 🔄 PREPARADO Y VERIFICADO CONTRA EL SIMULADOR; SIN APLICAR EN AWS REAL

Se eligió **AWS** (el módulo reutiliza el provider ya usado con LocalStack); no hay scaffold de GCP. El detalle de lo
implementado está en "Cierre de noviembre" arriba. Estructura real:

- `terraform/modules/argos-node/` — VPC, subred, IGW, rutas, security group, EC2 Ubuntu 24.04 (gp3 cifrado, IMDSv2),
  Elastic IP, secretos; `templates/deploy.sh.tftpl` (comando `argos-deploy`) y `scripts/base.sh` (cloud-init).
- `terraform/environments/localstack/` y `terraform/environments/aws/` — mismos `main.tf`/`variables.tf`/`outputs.tf`; difieren
  `providers.tf` y `terraform.tfvars` (hay un `terraform.tfvars.example` en cada uno).

- [x] Módulo compartido y dos entornos; `fmt` y `validate` en verde (también en CI)
- [x] `terraform apply` + `destroy` contra LocalStack 3.8 (12 recursos). AMI válida para LocalStack: `ami_id = "ami-785db401"`
  (Ubuntu 16.04 del catálogo precargado; LocalStack rechaza con `InvalidAMIID.NotFound` los IDs que no estén ahí)
- [ ] Credenciales de AWS reales (`AWS_PROFILE` o `AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY`) y `terraform.tfvars` del entorno `aws`
  (`admin_cidr_blocks`, `ssh_public_key`, `repo_url`/`repo_ref`, `argos_env`)
- [ ] `terraform init` / `plan` / `apply` en `terraform/environments/aws`
- [ ] Verificar el arranque por cloud-init en la VM real (`argos-deploy`, contenedores `argos-platform`, workflow de n8n activo)
- [ ] Verificar acceso: `http://<ip>/api/status` (80/8443 abiertos); panel y Grafana desde `admin_cidr_blocks`; el resto por túnel SSH
- [ ] Correr k6 contra la instancia en la nube y documentar latencias/throughput desde Internet
- [ ] Backend S3 cifrado para el state si se usa en equipo (los secretos del `.env` están en el state y el `user_data`)

### Validación de Detección y Calidad

- [x] **Detección:** validada en el escenario de ataque — 4 alertas Loki en firing
  (`NginxCriticalErrorRate`, `NginxErrorLogSpike`, `NginxHighErrorRate`, `NginxSuspiciousIPActivity`).
  Nota: el spike se comportó como evento de *error rate* (503 por rate-limiting), no de latencia, y la
  fase de error (~1 min) no sostuvo el umbral `for: 2m`, por lo que no llegó a firing.

- [x] **Calidad de síntesis LLM:** evaluada con rúbrica (diagnóstico, accionabilidad, completitud, claridad, alucinaciones) en
  7 escenarios y 2 modelos; puntuación asistida por Claude y revisada por el autor, no por evaluadores humanos
  independientes (`docs/EVALUACION_OE3.md` §2).
- [x] **Persistencia de auto-doc:** F4 y F7 se auto-documentan en la 1ª ocurrencia, quedan en Qdrant/dashboard y se
  recuperan en la 2ª (score ≥ 0.968) sin llamar al LLM (§4).

### Análisis de Resultados

- [x] Resultados de k6 por escenario (normal, spike, ataque, flood): `k6/RESULTS.md`. Ejemplos medidos: normal p95 = 3,23 ms con
  0 % de error y sin falsos positivos; ataque, 28.454 requests con ~74 % de 5xx y 4 alertas Loki en firing; flood, 8099 requests con
  37 % cortados por 429 y 0 errores 5xx.
- [x] **MTTR:** medida la condición Argos (`docs/EVALUACION_OE3.md` §5): propuesta en ~1,1 min desde el `startsAt`, tiempo hasta la
  acción correcta ~1,2 min. **Línea base manual no medida**: el "~60 % de reducción (de 30 a 12 min)" que figuraba acá era un
  supuesto sin datos y no se sostiene; no hay porcentaje de reducción medido.
- [x] Limitaciones y líneas de mejora identificadas: `docs/EVALUACION_OE3.md` §7 (N chico, operador simulado, evaluador LLM,
  entorno local, k6 desde una sola IP) y las de noviembre (§6).
- [ ] Estudio de MTTR con operadores humanos con y sin Argos (depende del autor).

### Memoria Académica

El entregable es el **Informe Final** (`docs/Informe_Final_Acosta_Juan_Agustin.docx` y `.pdf`), que se mantiene aparte de este
backlog; la plantilla es `docs/Plantilla Informe Final.docx`. Este backlog no registra el avance de su redacción.

- [~] Informe Final (estructura según la plantilla de la Facultad): ver el documento
- [ ] Adjuntar capturas de dashboards y ejemplos de síntesis del LLM (según el estado del Informe)

---

## Backlog Transversal / Deuda Técnica

Estos ítems son ortogonales a los Sprints 1-6. Estado al 2026-11-12.

### Seguridad

- [~] **SQL Server password:** `YourStrong@Passw0rd` fijo en el compose de desarrollo (en la nube, los secretos de n8n/JWT/token/admin sí se
  generan con Terraform, pero SQL Server no). Pendiente pasarla a `${SQL_PASSWORD}`.
- [~] **Alertmanager SMTP:** `email-default`/`email-critical` siguen con placeholders (`REPLACE_WITH_*`); el email real sale por n8n.
- [~] **ANTHROPIC_API_KEY:** la del `.env` de desarrollo es inválida (401); no se puede elegir Claude hasta reemplazarla. Guardar en `.env`/secretos, no en el código.
- [ ] **Grafana admin:** sigue `admin/admin` en desarrollo (`GRAFANA_PASSWORD`); cambiar en producción.
- [ ] **Nginx SSL:** certificados self-signed; cambiar en producción.
- [x] **Endpoints internos y de incidentes sin auth** (F-01, F-08, F-13): corregidos el 2026-10-21 (`docs/FIXES_DASHBOARD.md`).
- [x] **Secretos en la nube:** los cuatro secretos se generan con `random_password` (noviembre). Siguen en el `user_data` y el state de Terraform.
- [ ] **Socket de Docker:** montado en `argos-api` (k6 on-demand) y en Alloy (solo lectura): privilegio alto; en producción acotar con un proxy del socket.
- [x] **Seudonimización de IP** hacia el proveedor LLM (noviembre); no es anonimización.

### Base de Datos

- [x] **Inicialización de MembresiaDB:** contenedor `db-init` (`docker/apps/db/init-db.sql`, idempotente).
- [x] **Seed data:** runbooks de `knowledge/` al arrancar (`SeedService`, idempotente; completa el documento de los ya existentes). El seed demo está apagado a propósito.

### API .NET

- [x] **Pruebas unitarias:** `argos-api/Argos.Tests` (xUnit, 102 pruebas en verde; corren en CI).
- [ ] **Swagger/OpenAPI en producción:** no confirmado en el repo; sin cambios.
- [ ] **Métricas de app en Prometheus (gap de scraping):** Prometheus solo scrapea `nginx-exporter`/`node-exporter`/`cadvisor`/`prometheus`;
  `argos-api`, `membresia-api` y Qdrant no exponen `/metrics`. La salud de servicios se resuelve con probes HTTP directos.
- [ ] **Instrumentación OpenTelemetry:** no implementada.
- [~] **Logging de aplicación:** Alloy envía a Loki los logs de los contenedores con el label `argos.logs=true` (opt-in desde el 15/11) y la API de ejemplo
  ahora registra los errores que antes callaba. Logging estructurado (Serilog/JSON) no hecho.
- [ ] **Borrar el documento de prueba `DotnetMigrationE2E`** (autodocumentado) de la base de conocimiento: ahora se puede con `DELETE /incidents/{id}` (Admin).

### Panel

- [ ] **F-12** (búsqueda sin debounce, categorías de la KB solo de la página actual, etc.): abierto. Ver `docs/FIXES_DASHBOARD.md`.
- [x] **Suite E2E completa repetida** (15/11): 101 de 103 aprobadas, 2 omitidas. Falta confirmar I-07 con un incidente en estado `nuevo`.
- [ ] **F-11** (elementos decorativos): ver `docs/FIXES_DASHBOARD.md`.

### CI/CD

- [x] **GitHub Actions (CI):** `.github/workflows/ci.yml`, seis jobs (ver "Cierre de noviembre"). Todavía no corrió en GitHub: falta subir los cambios.
- [ ] **CD:** no hay despliegue continuo ni publicación de imágenes (los `build-test.yml`/`deploy.yml` y el push a Docker Hub planteados originalmente no se hicieron).
- [ ] Fusionar `feat/dashboard-nextjs` en `master` (la rama principal tiene el panel en Vite).
- [ ] **Code quality checks (opcional):** SonarQube/StyleCop, sin hacer.

### Documentación

- [~] **README.md** y **CLAUDE.md:** actualizados tras el cierre de noviembre; mantener al día tras cada cambio.
- [ ] **Swagger/OpenAPI:** sin cambios.

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
2. ~~**Sprint 3:** Desplegar Qdrant, validar conexión, crear colecciones~~ ✅ Completo (`argos-api/`)
3. ~~**Sprint 4a:** Desplegar n8n, importar workflow, integrar LLM (dual Gemini/Claude), webhook
   Alertmanager~~ ✅ Hecho
4. ~~**Sprint 4b (cerrar):** SMTP real + credencial + activar workflow + validar e2e Fase 1~~ ✅ Hecho
5. ~~**Sprint 5a:** auto-doc en n8n (rama `Is New?`), endpoints `rag-api` `/incidents*`, dashboard
   React~~ ✅ Implementación base hecha (Qdrant-only + SPA React)
6. ~~**Sprint 5b (validar):** rebuild `rag-api` + reimportar workflow + levantar `argos-dashboard` →
   e2e: alerta nueva → doc generado → visible en dashboard + KB; validar `/incidents/metrics`~~
   ✅ Validado E2E (2026-09-23) + auto-seed al arrancar
7. ~~**Sprint 6a (k6):** 3 escenarios k6 + validar cadena de detección~~ ✅ Validado E2E (2026-09-30)
7b. ~~**Sprint 6 (k6 on-demand):** ejecutar escenarios desde el dashboard + visualizar código~~
   ✅ Hecho y validado E2E (2026-10-05) — vista "Pruebas de Carga", `rag-api` + Docker socket
7c. ~~**Sprint 6 (evaluación OE3):** calidad del LLM, clasificación RAG, aprendizaje y MTTR con la condición Argos~~
   ✅ Cerrada (2026-10-28), línea base manual no medida
7d. ~~**Cierre de noviembre:** módulo Terraform portable, CI, logs de aplicación, evidencia de logs + seudonimización, señal por
   nombre, arreglos del panel~~ ✅ Hecho (2026-11-11/12), verificación E2E del panel pendiente
8. **Sprint 6b (cloud):** `terraform apply` del entorno `terraform/environments/aws` en una cuenta real, verificar el arranque
   por cloud-init y correr k6 contra la instancia
9. **Sprint 6c (memoria):** cerrar el Informe Final (se mantiene aparte, `docs/Informe_Final_*`)
10. **Pendientes del autor:** estudio de MTTR con personas, subir los cambios (CI) y fusionar
    `feat/dashboard-nextjs` en `master`
