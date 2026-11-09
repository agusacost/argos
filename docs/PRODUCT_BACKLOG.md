# Argos: Product Backlog / Plan de Evolución del Trabajo

**Proyecto:** Argos — Plataforma inteligente de AIOps  
**Alumno:** Juan Agustín Acosta (LU 57851)  
**Profesor Orientador:** Rios, Leopoldo Jose  
**Fecha de creación:** 2026-08-04  
**Última actualización:** 2026-10-07  
**Versión:** 1.5  

---

## Propósito de este documento

Este es el **product backlog operativo** del proyecto Argos. Complementa la propuesta académica formal
(`docs/Anexoll_Acosta_Juan_Agustin_v2.md`, Anexo II) que define el alcance y las fases; este
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
>    sigue abierto por el cloud deploy y la memoria), dato honesto para la sección de limitaciones.
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

## Estado actual (Snapshot a 2026-10-12)

| Sprint | Objetivo | Entregable esperado | Estado | % |
|--------|----------|----------------------|--------|-----|
| **1** | Infraestructura base (Terraform/LocalStack + Docker stacks) | Flujo end-to-end Nginx → Alloy → Loki → Grafana | ✅ Hecho | 100% |
| **2** | Stack observabilidad + reglas de detección | Tableros + alertas (18 reglas) + Alloy | ✅ Hecho | 100% |
| **3** | Base de conocimiento vectorial + RAG | Qdrant + pipeline RAG funcional | ✅ Hecho | 100% |
| **4** | Orquestación n8n + LLM (Gemini/Claude) | Workflows → análisis IA + notificaciones | ✅ Validado (Fase 1 + cadena real vía k6) | 100% |
| **5** | Auto-documentación + dashboard Argos | Dashboard propio + docs auto-generadas | ✅ Validado E2E | 100% |
| **6** | Simulación k6 + validación + cloud | k6 scenarios + cloud deploy (GCP/AWS) | 🔄 En progreso | ~50% |

**Próximo hito:** cerrar Sprint 6. La **parte k6 está validada E2E** (2026-09-30): 3 escenarios
(normal/spike/ataque) ejecutados; el escenario de ataque disparó **4 alertas Loki en firing**
(`NginxCriticalErrorRate`, `NginxErrorLogSpike`, `NginxHighErrorRate`, `NginxSuspiciousIPActivity`)
→ Alertmanager → webhook n8n (HTTP 200), cerrando también la **Fase 2 del Sprint 4** (cadena real).
Resultados en `k6/RESULTS.md`. **Restante:** (1) deploy en cloud (GCP/AWS) y (2) memoria académica
final. Ver Sprint 6 abajo.

**Pool de destinatarios de alertas (2026-10-12):** el email de alertas de n8n dejó de ir a una única
dirección fija por env (`ALERT_RECIPIENT_EMAIL`): ahora se envía a un **pool de emails configurable
desde el dashboard** (Configuración, solo Admin), persistido en Qdrant. Implementado y compilado;
validado E2E vía fallback (falta probar con direcciones en el pool). Ver Sprint 6 → "Pool de destinatarios de alertas".

**Configuración funcional (2026-10-12):** todas las opciones del panel Configuración pasaron a tener efecto real
(LLM, umbral, auto-doc, polling) y los switches de Integraciones se reemplazaron por estado en vivo.
Ver Sprint 6 → "Configuración funcional del dashboard".

**Puente Sprint 3→4:** El RAG se expone como API HTTP (Argos API, `argos-api/` en .NET 10, servicio
`argos-api` con alias `rag-api` en el compose de monitoring) para que n8n pueda consultarlo vía
`POST http://rag-api:8000/query`.

**Dual LLM provider (nuevo):** El workflow soporta **Gemini y Anthropic** de forma intercambiable. La
variable `LLM_PROVIDER` (`gemini` | `anthropic`) selecciona el proveedor en runtime mediante el nodo
**`Route LLM`** (arma URL/headers/body por proveedor) y el nodo **`Parse`** detecta el formato de
respuesta (`candidates[]` de Gemini / `content[]` de Anthropic). Default: `gemini` /
`gemini-flash-latest` (alias auto-actualizable: los modelos pineados tipo `gemini-2.x-flash` ya están
deprecados/retirados para cuentas nuevas). La API key viaja por header/query desde `$env`, sin
credencial n8n dedicada para el LLM.

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
- [x] Umbral de similitud: `SIMILARITY_THRESHOLD=0.63` (configurable en `.env`)

### Carga Inicial de Conocimiento

- [x] 5 docs en `argos-api/Argos.Api/knowledge/` (frontmatter YAML + markdown):
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

- [~] Node: Error Handling — parcial: retry x3 en `LLM Call`. Falta rama de fallback (alerta genérica
  si el LLM falla tras los reintentos).

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
> - **Dashboard = SPA propia React + Vite + TypeScript** (`dashboard/`), servida por nginx (proxy
>   `/api` → `rag-api`, sin CORS). Servicio `argos-dashboard` (puerto 3001) en el stack monitoring.
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

- [x] `GET /incidents/knowledge-base` — solo `fuente=auto-generado`; `?search=&page=&size=`
  (resúmenes, sin el markdown completo)

### Dashboard de Argos (Frontend)

**Tecnología:** React + Vite + TypeScript (`dashboard/`), servido por nginx (proxy `/api` → `rag-api`).
**Puerto:** 3001 (servicio `argos-dashboard` en stack monitoring).

- [x] Página: Historial de incidentes (`/incidents`)
  - Tabla: tipo_alerta, fecha, resumen, estado (badge), fuente
  - Filtros: por tipo de alerta y estado; búsqueda por palabra clave
  - Detalles: click en fila → modal con documento markdown renderizado (`react-markdown`)

- [x] Página: Métricas agregadas (`/metrics`)
  - Cards: total, MTTR promedio, tendencia, auto-documentados
  - Barras: incidentes por tipo (top 8, `recharts`); línea: incidentes por día
  - Indicador de tendencia (↑ empeorando / ↓ mejorando / → estable); breakdown por estado

- [x] Página: Base de conocimiento (`/knowledge-base`)
  - Listado de documentos `fuente=auto-generado` (solo lectura)
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
detección y calidad, migrar infraestructura a AWS o GCP. Completar memoria académica con análisis
de resultados.

Entregable: evidencia de pruebas, análisis de resultados, infraestructura en cloud, memoria académica
completa.

**Estado:** 🔄 En progreso | ~50% (k6 validado E2E 2026-09-30; **pendiente: cloud deploy + memoria académica**)

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

### Pool de destinatarios de alertas — 🔄 IMPLEMENTADO (2026-10-12), validación E2E pendiente

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
  porque el pool está vacío). Falta el paso 5 con direcciones reales cargadas en el pool.
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

### Cloud Deployment (AWS o GCP) — ⏳ PENDIENTE (bloquea el cierre del Sprint 6)

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

- [x] **Detección:** validada en el escenario de ataque — 4 alertas Loki en firing
  (`NginxCriticalErrorRate`, `NginxErrorLogSpike`, `NginxHighErrorRate`, `NginxSuspiciousIPActivity`).
  Nota: el spike se comportó como evento de *error rate* (503 por rate-limiting), no de latencia, y la
  fase de error (~1 min) no sostuvo el umbral `for: 2m`, por lo que no llegó a firing.

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

- [ ] **Métricas de app en Prometheus (gap de scraping):** Prometheus solo scrapea
  `nginx-exporter`/`node-exporter`/`cadvisor`/`prometheus`. `api`, `rag-api` y `qdrant` **no exponen
  `/metrics` ni tienen scrape job**, por eso la salud de esos servicios se resuelve hoy con probes HTTP
  directos (workaround, 2026-10-07). Para observabilidad real de la app convendría exponer `/metrics`
  (ver OpenTelemetry abajo) + agregar scrape jobs; recién ahí tendría sentido volver a `up{job}`.

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
8. **Sprint 6b (cloud):** crear `terraform/environments/gcp/` (o `aws/`) con provider real, desplegar
   los 3 stacks en una VM, verificar acceso público y correr k6 contra la instancia cloud
9. **Sprint 6c (memoria):** redactar la memoria académica final (arquitectura, resultados k6, análisis
   MTTR, limitaciones, conclusiones) con screenshots, ejemplos de síntesis LLM y logs de k6
