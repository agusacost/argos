# Argos: Instrucciones para Claude Code

**Proyecto:** Argos — Plataforma inteligente de AIOps  
**Alumno:** Juan Agustín Acosta (LU 57851)  
**Fecha:** 2026-08-03 (última actualización: 2026-11-15)

---

## Contexto del proyecto

Argos es una plataforma AIOps construida sobre un stack de observabilidad open-source (Prometheus, Loki, Grafana, Alertmanager). Detecta incidentes por umbrales, genera síntesis del problema + pasos de resolución mediante IA (LLM con cadena de respaldo Gemini → Groq → OpenRouter + RAG), y documenta automáticamente casos no registrados.

**Estado actual:** Sprints 1-5 completados; Sprint 6 casi cerrado (falta aplicar Terraform en AWS real + memoria)
- ✅ Terraform (módulo `argos-node` + entornos `localstack`/`aws`, ver "Cierre de noviembre") + LocalStack + 3 stacks Docker (monitoring, proxy, apps)
- ✅ Alertas Prometheus (13) + Loki (5)
- ✅ Grafana + 3 dashboards
- ✅ Alloy log collection
- ✅ BD MembresiaDB inicializada automáticamente (db-init en docker/apps)
- ✅ Qdrant + pipeline RAG (embeddings locales ONNX `paraphrase-multilingual-MiniLM-L12-v2` 384 dims, umbral default 0.63 — **en settings 0.68 desde 2026-10-27** por la evaluación del OE3, colección `incidentes`)
- ✅ **Argos API** (`argos-api/`, .NET 10): única API de la plataforma; contenedor `argos-rag-api` con alias `rag-api:8000`, consumida por el dashboard y n8n
- ✅ Sprint 4 (n8n + LLM Gemini/Claude) **validado Fase 1** (2026-08-11): `argos-n8n` (SQLite) up+healthy + `argos-rag-api` up, workflow `docker/monitoring/n8n/workflows/alert-analysis.json` **importado, con credencial SMTP adjunta y ACTIVO**. Dual-provider vía `LLM_PROVIDER` (`gemini`|`anthropic`) + nodos `Route LLM`/`Parse`; default `gemini-flash-latest`. E2E Fase 1 OK: payload simulado → `status=success` → email enviado. Pendiente opcional: Fase 2 (alerta real vía Alertmanager)
- ✅ Despliegue unificado vía `docker-compose.yml` raíz (desacoplado de Terraform)
- ✅ Sprint 5 (auto-documentación + dashboard) **validado E2E** (2026-09-23):
  - Argos API completa: auth JWT, CRUD usuarios, settings, `/incidents*`, `/observability/services`,
    `/notifications`, `/activity`, `/incidents/{id}/document` (LLM), `/incidents/{id}/observability`
    (queries reales a Loki/Prometheus), auditoría — **sin stubs**.
  - **Dashboard propio** Next.js (`dashboard/`, 7 vistas: Dashboard/Incidentes/Base de Conocimiento/
    Métricas/Pruebas de Carga/Usuarios/Configuración), servicio `argos-dashboard` puerto 3001, nginx proxya `/api`→`rag-api`.
  - **Auto-seed al arrancar** (`SeedService` + `DemoIncidentCatalog`): 5 runbooks de knowledge/ + 15 incidentes demo
    con estados/severidades/MTTR/docs variados. Idempotente. Flags: `ARGOS_AUTO_SEED`, `ARGOS_SEED_DEMO`.
  - **Fix KB**: la vista Base de Conocimiento filtra por `estados=[documentado_previamente,auto_documentado]`
    (no por `fuente`), mostrando tanto runbooks manuales como docs auto-generados por el LLM.
  - Credenciales default: `admin@argos.local` / `argos-admin-2026`.
- ✅ Sprint 6 — k6 **validado E2E** (2026-09-30): 3 escenarios (normal/spike/ataque); el de ataque
  dispara 4 alertas Loki en firing → Alertmanager → webhook n8n. Ver `k6/RESULTS.md`.
- ✅ Sprint 6 — **k6 on-demand desde el dashboard** (2026-10-05): vista "Pruebas de Carga" ejecuta los
  escenarios on-demand (solo Admin) y muestra el código + resultados. La API lanza el contenedor
  `grafana/k6` como hermano vía Docker socket + `Docker.DotNet`; volúmenes nombrados `argos_k6_scripts`/
  `argos_k6_results`; corridas persistidas en Qdrant (colección `argos_loadtests`). `LoadTestService`,
  endpoints `/loadtests/*`, vista `dashboard/src/components/LoadTestsView.tsx`.
- ✅ **Observabilidad en el dashboard** (2026-10-07): panel de **golden signals de Nginx** en la vista
  Métricas (`dashboard/src/components/NginxSignals.tsx`): requests por status code, latencia upstream
  P50/P95/P99 y error rate 5xx como series temporales desde Loki (selector 15m/1h/3h/6h). Endpoint
  `GET /observability/nginx-metrics?range=` (`NginxMetricsService`). Absorbe lo
  esencial del dashboard Grafana `nginx-overview` sin duplicar lo ya existente en Argos (logs por
  incidente, salud de servicios). **Fix "Salud de Servicios"**: `PlatformHealthService` hace probes
  HTTP directos sobre `argos-net` (nginx/api/qdrant/rag-api; healthy si HTTP <500) en vez de depender
  de `up{job}` de Prometheus — los jobs `api`/`rag-api`/`qdrant` **no se scrapean** (solo
  nginx-exporter/node-exporter/cadvisor/prometheus), por eso antes salían todos `unknown`. URLs por
  env (`NGINX_STATUS_URL`/`DOTNET_API_URL`/`QDRANT_READY_URL`/`RAG_SELF_URL`).
  - **Gotcha Loki range**: `query_range` alinea los samples a múltiplos absolutos del `step`; la grilla
    temporal del backend debe alinearse igual (`(start//step)*step`) o el match por timestamp da 0.
- ✅ **Pool de destinatarios de alertas** (2026-10-12, **validado E2E 2026-10-14** con `recipients_source=pool`; pool actual:
  `juanagustinacost@gmail.com`, `juanagustinacosta@hotmail.com`): el email de n8n ya
  no va solo a `ALERT_RECIPIENT_EMAIL` sino a un **pool configurable** desde Configuración (solo Admin).
  - Almacenamiento: campo `alert_recipients` del doc único de `argos_settings` (Qdrant-only),
    vía `GET/PATCH /settings`. `SettingsValidator.NormalizeRecipients()` (lowercase, dedup, regex, tope 50 →
    422 si es inválido). `[]` = pool vacío → fallback.
  - n8n lo lee **en cada alerta** con `GET /notifications/recipients` (sin JWT; si existe la env
    `ARGOS_INTERNAL_TOKEN`, exige el header `X-Argos-Token`). Nodos `Get Recipients` (onError=continue) →
    `Resolve Recipients` (`to` = pool o fallback `$env.ALERT_RECIPIENT_EMAIL`; expone `recipients_source`) →
    `Notify Email` (`toEmail={{ $json.to }}`).
  - UI: tarjeta "Notificaciones por email" en `SettingsView.tsx` (chips, N/50).
  - **Gotchas/fallas:** (1) hay que **re-importar el workflow en n8n y reactivarlo** (y revisar la
    credencial SMTP), si no sigue usando la env; (2) un token desalineado entre la API/n8n → 401 →
    **fallback silencioso** al env (mirar `recipients_source`); (3) Gmail: 100 destinatarios/mensaje,
    ~500/día; (4) un solo envío con varios To (todos ven las direcciones de todos); (5) sin token, el endpoint
    queda expuesto vía el proxy `:3001/api`; (6) `email-default` de Alertmanager no usa el pool
    (placeholders). Detalle en `docs/PRODUCT_BACKLOG.md` (Sprint 6).
- ✅ **Configuración funcional** (2026-10-12, validado E2E): el panel Configuración ya no es decorativo.
  - LLM proveedor/modelo → `LlmService.Resolve(settings)` (API) + n8n vía `GET /internal/config` (nodo `Get Config`,
    fallback env). Al guardar, `ILlmProvider.CheckAsync` valida contra el proveedor (key inválida / modelo Gemini inexistente → 422).
  - Umbral → `/query` lee settings. Auto-doc → nodo `Auto Doc?`; con OFF, `Build Pending Incident` guarda `estado=nuevo`.
  - Polling → `App.tsx` usa `polling_interval_seconds` (contexto `setPollingSeconds`).
  - Integraciones → **estado en vivo** `GET /integrations/status` (la clave `integrations` se descarta).
  - **Gotcha:** la `ANTHROPIC_API_KEY` actual del `.env` es **inválida** (401) → no se puede elegir Claude hasta
    reemplazarla. Si `Get Config` falla, n8n usa env y asume auto-doc ON. Detalle en el backlog (Sprint 6).
- ✅ **Argos API en .NET 10** (2026-10-19, `argos-api/`): solución `Argos.slnx` en capas Entities / Repository /
  Business / Controllers (+ Common), módulos Autofac por capa, todo por interfaz, sin clases anidadas.
  Embeddings con ONNX Runtime + `Microsoft.ML.Tokenizers`, Qdrant por REST, JWT HS256 + bcrypt. Validado: n8n
  (alerta simulada y real), k6 on-demand, "Generar documentación" con LLM, arranque limpio. Detalle:
  `argos-api/README.md`; checklist de UI pendiente en `docs/VALIDACION_API.md`.
  - ~~Gotcha: tras recrear `argos-rag-api`, hacer `docker restart argos-dashboard`~~ — **ya no aplica** (2026-11-11): los dos Nginx
    resuelven el nombre en cada request (`resolver 127.0.0.11` + variable en `proxy_pass`); antes lo resolvían una sola vez al arrancar.
- ✅ **LLM con fallback + salud de la IA + notificaciones** (2026-10-26): n8n ya **no llama a los LLM**; llama a
  `POST rag-api/internal/llm/generate`, que recorre la cadena principal (settings) → `LLM_FALLBACK_CHAIN`
  (default `gemini:gemini-flash-lite-latest,groq,openrouter`; eslabones sin key se saltean). Proveedores nuevos
  **Groq** y **OpenRouter** (API OpenAI-compatible, `GROQ_API_KEY`/`OPENROUTER_API_KEY`). Salud **pasiva**
  (`LlmHealthTracker`, en memoria): cada llamada real registra up/down; un fallo deja al eslabón en cooldown
  (`LLM_COOLDOWN_SECONDS`=300) al final de la cadena. Sin pings automáticos; "Probar ahora" manual
  (`POST /llm/health/check`, Admin). `GET /llm/health` alimenta el badge del header (activa/en respaldo/no disponible)
  y la tarjeta "Estado de los modelos IA" en Configuración.
  - **Notificaciones** (campana, persistidas en `argos_notifications`, sin push) con `kind`: `incident` (cada
    incidente que registra el pipeline vía `/upsert`, además de nuevos/críticos), `llm_health` (solo en cambios
    de estado de un proveedor) y `pipeline_error` (falló toda la cadena). Click → abre el incidente;
    `POST /notifications/read-all`.
  - Si falla toda la cadena, el workflow **igual** manda el email (con aviso) y guarda el incidente como `nuevo`
    (antes se cortaba la ejecución y no quedaba rastro). Re-import por CLI (sin login a la UI):
    `MSYS_NO_PATHCONV=1 docker exec argos-n8n n8n import:workflow --input=/workflows/alert-analysis.json` +
    `n8n update:workflow --id=argos-alert-analysis --active=true` + `docker restart argos-n8n`.
  - **Gotcha:** el chequeo de settings (`CheckAsync`, GET de metadata) **no detecta** un modelo saturado (503
    "high demand" de `gemini-flash-latest`); solo una generación real lo ve.
- ✅ **Ciclo de vida de alertas + KPIs honestos** (2026-10-26):
  - n8n distingue `firing`/`resolved` (nodo `Resolved?`): una **resolución no se analiza ni manda email**; va a
    `POST /internal/incidents/resolve` → `resolved_at = endsAt` y **MTTR real** (`endsAt − startsAt`) + aviso en la campana.
  - Alertas **conocidas** (match en la KB) ahora quedan como incidente `pendiente` con la solución copiada del mejor
    match (`POST /internal/incidents/occurrence`, sin llamar al LLM para documentar). Antes solo mandaban email.
  - Cada ocurrencia = `tipo + startsAt` (`detected_at`): `Incident.NaturalKey` incluye `detected_at`, así una
    repetición de Alertmanager (repeat_interval 1h) no duplica y la resolución encuentra la ocurrencia exacta.
  - **Fix RAG:** el incidente se guarda con el MISMO texto con el que se busca (`summary. description`, `query_text`
    en `Prepare`). Antes se guardaba solo el summary → la misma alerta daba score 0.398 contra su propio doc y se
    re-documentaba en cada repetición.
  - Seed demo **apagado** (`ARGOS_SEED_DEMO=false`; los 15 demo se borraron de Qdrant): con fechas relativas al
    arranque "rejuvenecían" en cada reinicio. El seed solo inserta lo que falta (`SaveIfAbsentAsync`).
  - Dashboard: `nuevo` = "Sin documentar" (antes "Analizando IA" / "análisis en curso"); KPI "Pendientes de
    documentar" clickeable → Incidentes filtrado; KPI MTTR usa `mttr_trend` (7d vs 7d) y `mttr_per_day` (antes mostraba
    la tendencia de VOLUMEN); volumen por día solo con `detected_at`; banner con estado real de la IA (sin "Uptime 99.99%").
    Fixes F-07 (umbral KB fijo) y F-09 (título Pruebas de Carga).
- ✅ **Evaluación del OE3** (2026-10-27/28, cerrada con limitación declarada): `eval/` (catálogo de fallas
  F1–F7, `faults.ps1` con modo ciego, `timer.ps1` contra Alertmanager, `mark.ps1`, `summarize-mttr.mjs`,
  `run-llm-eval.mjs` que ejecuta los nodos reales del workflow). `POST /internal/llm/generate` acepta `provider`/`model`
  (un solo proveedor, sin fallback ni aviso; solo evaluación). Informe: `docs/EVALUACION_OE3.md`.
  - Medido (2 corridas): `gemini-flash-latest` 0/17 (503/timeout) → **principal = `gemini-flash-lite-latest`** y
    **umbral 0.68** en settings (RAG 6/7 sin falsos "conocida"; n8n 65 s → 6 s). Aprendizaje OK (F4 y F7: 2ª ocurrencia
    reconocida con ≥0.968). `openrouter/free` a veces devolvía "User Safety: safe" (el router elegía un clasificador)
    → **`OPENROUTER_MODEL=nvidia/nemotron-3-ultra-550b-a55b:free`** (8/8 OK).
  - Prompts con el documento aprendido + contexto del stack (accionabilidad 2,5 → 4,5 en alertas aprendidas). **Desde el
    2026-11-15 ese contexto ya no está en el código**: es el campo `environment_context` de Configuración (ver "Contexto del entorno").
    10 corridas automáticas (`eval/run-auto.mjs`): propuesta en ~1,1 min, arreglo exacto 8/10. **Línea base manual de
    MTTR no medida: limitación declarada** (decisión del autor). Presentación/SPEECH: OE3 95 %, total ~93 %.
- ✅ **Dashboard migrado a Next.js** (2026-11-10): la propuesta (Anexo II) pedía Next.js y estaba en Vite.
  Next.js 16 App Router con `output: 'export'` (el mismo nginx sirve `out/`; proxy `/api` y bloqueo de
  endpoints internos sin cambios), React 19, **una ruta por vista** (`/incidentes`, `/metricas`, `/conocimiento`,
  `/pruebas-carga`, `/usuarios`, `/configuracion`; mapa en `dashboard/src/routes.ts`). Estado global en
  `src/app-shell/AppProvider.tsx` (la vista activa sale de la URL). E2E: `e2e/routing.spec.ts` + suite adaptada
  → 80 pasan, 4 fallan (I-04, I-07, K-06, C-01: fallan igual con Vite, son pendientes previos).
  - **Gotchas:** `package-lock.json` está en `.gitignore` → el Dockerfile usa `npm install` (no `npm ci`);
    `next dev` usa rewrites (`DEV_API_TARGET`), el build no (export estático); `next dev` genera un
    `AGENTS.md` en `dashboard/` (ignorado en git).
- ✅ **Cierre de noviembre** (2026-11-11/12; detalle y cifras en `docs/EVALUACION_OE3.md` §6 y `docs/PRODUCT_BACKLOG.md`):
  - **IaC (OE1):** módulo `terraform/modules/argos-node` (VPC, SG, EC2 Ubuntu 24.04 gp3 cifrado + IMDSv2, Elastic IP, 4 secretos con
    `random_password`, cloud-init que levanta el compose raíz y deja `argos-deploy`) usado por `environments/localstack` y
    `environments/aws` (`main/variables/outputs.tf` idénticos; cambia `providers.tf` + tfvars). `fmt`/`validate` OK y `apply`+`destroy`
    en LocalStack 3.8 (12 recursos). **No se aplicó en AWS real** (ni se probó cloud-init en una VM real). Instancia `t3.xlarge`.
  - **CI:** `.github/workflows/ci.yml`, 6 jobs (terraform, observability-config, compose, argos-api con 106 tests xUnit de
    `argos-api/Argos.Tests`, membresia-api, dashboard). Solo CI, sin CD. **Aún no corrió en GitHub** (cambios sin subir).
  - **Logs de aplicación:** Alloy descubre por el socket de Docker (`:ro`) **solo los contenedores con el label `argos.logs=true`**
    (opt-in: `membresia-api` y `membresia-sqlserver` en `docker/apps/docker-compose.yml`; desde el 2026-11-15) → Loki con
    `job="docker"`, `container="<nombre>"`; no toca las etiquetas `service="nginx"`. Antes recolectaba todos los `argos*` (13 streams).
  - **Principio de diseño (2026-11-15, decisión del autor):** Argos infiere todo **a partir de Nginx** (o de la propia alerta), sin
    conocer los contenedores del servicio. La fuente de evidencia la declara cada regla con el label `evidence`: `nginx` (las 5 de Loki +
    las 3 de tráfico de Prometheus) o `target` (`NginxDown`, `NodeExporterDown`, `CAdvisorDown`, usa el `instance`); sin label, nada.
    Con `nginx`, el upstream sale de `upstream_addr`/error.log y `DockerContainerDirectory` lo traduce a contenedor (IP, nombre, servicio
    de Compose o alias). **No volver a escribir nombres de contenedores en `AlertEvidencePlanner`.**
  - **Contexto del entorno (2026-11-15, decisión del autor):** la descripción del entorno que acompaña a los prompts (Docker Compose,
    contenedores, puertos) salió del código (`Build Prompt`, `Build Doc Prompt`, `IncidentDocumentPrompt`) y pasó a configuración:
    campo `environment_context` de `argos_settings`, editable en Configuración (Admin, tope 3000 caracteres → 422), expuesto a n8n por
    `GET /internal/config` (nodo `Get Config`). Si nunca se editó (`null`), `SettingsRepository` usa `ARGOS_ENVIRONMENT_CONTEXT` del
    despliegue (valor de ejemplo en `docker/monitoring/.env.example`); `""` = vacío a propósito. El código solo conserva la instrucción
    genérica (`IncidentDocumentPrompt.EnvironmentGuidance`, mismo texto en n8n). Gotcha: el `.env` local no trae la variable; en el
    stack local el contexto se cargó por `PATCH /settings` (persiste en Qdrant). `eval/run-llm-eval.mjs` lee `/internal/config` igual que n8n.
  - **Runbooks fuera de la imagen (2026-11-15):** los 5 runbooks pasaron de `argos-api/Argos.Api/knowledge/` a
    `docker/monitoring/knowledge/` (montados en `/app/knowledge:ro`): nombraban contenedores y tablas del servicio de ejemplo, así que
    son contenido del despliegue, no de Argos. La imagen de la API no trae runbooks. Se quitó la contraseña `sa` del runbook
    `nginx-5xx.md` y de los 17 incidentes de Qdrant que la habían copiado en sus pasos (iba a parar a los prompts del LLM externo).
    Gotcha: el seed no pisa documentos existentes; un runbook ya sembrado se corrige con `PATCH /incidents/{id}`.
  - **Evidencia de logs + IP seudonimizada:** `POST /internal/evidence` (token interno) y nodo `Get Evidence` (entre `RAG Query` y
    `Build Prompt`, falla = continúa): ≤18 líneas de ≤200 caracteres de Loki. `LlmService` reemplaza IPv4/IPv6 por `[IP-n]` antes de
    enviar y las restaura en la respuesta. Env: `ALERT_EVIDENCE_ENABLED`, `LLM_PSEUDONYMIZE_IPS` (ambas `true`). Seudonimización, no
    anonimización; ahora al LLM externo **sí** sale una muestra de logs. **Diseño vigente (15/11):** ya no hay contenedores fijos; el
    upstream sale de Nginx y `DockerContainerDirectory` lo traduce a contenedor e informa su estado ("Exited (0) ..."); la base de datos
    no se consulta (Nginx solo conoce a la API: su causa se ve únicamente si la API la registra). Si Docker o Loki no responden, la
    evidencia queda solo en Nginx o vacía. n8n manda los labels de la alerta (nodos `Prepare`/`Get Evidence`). Verificado: F1, F2 y F7
    con arreglo exacto, N = 1 (`docs/EVALUACION_OE3.md` §6.2 y §6.5).
  - **Señal por nombre en el RAG:** `POST /query` acepta `alertname`, devuelve `match_reason` (`similarity|alert_name|none`); un doc con
    solución del mismo tipo con similitud ≥ `RAG_NAME_MATCH_MIN_SCORE` (**0.30**) cuenta como conocido. Re-evaluación 7/7.
  - **Defecto de Nginx corregido:** guardaba la IP del upstream (resuelta al arrancar); tras `docker stop/start membresia-api` quedaba en 502.
    Por eso los MTTR de F1/F2 de octubre miden cuándo se apaga la alerta, no cuándo vuelve el servicio.
  - **Panel:** implementados F-02/F-03/F-04/F-05/F-06/F-10 (`POST /incidents/knowledge-base`, `DELETE /incidents/{id}`, ambos Admin).
    **Suite E2E completa re-corrida el 15/11: 103 pruebas, 101 ok, 0 fallas, 2 omitidas** (S-06 e I-07 por falta de datos). Encontró un
    defecto real en M-04: la latencia P50/P95/P99 de Nginx no agrupaba (una serie por combinación de etiquetas; 400 de Loki a 6 h) →
    `by (service)` en `NginxMetricsService`. Gotcha E2E: Next.js agrega un `role=alert` propio (anunciador de rutas); filtrar por texto.
    F-12 sigue abierto. API de ejemplo (`docker/apps/api/MembresiaAPI`) ahora registra sus errores (antes los callaba y la evidencia quedaba muda): sin esos logs, la causa en la base de datos no llega a la evidencia.
  - **Gotchas:**
    1. **Piso de la señal por nombre = 0.30**, no 0.50: `NginxHighResponseTime` (con runbook manual) puntuó 0.355 contra su runbook (el resumen
       escrito a mano se parece poco al texto real) y con 0.50 se autodocumentó de más. Apoyado en dos casos (0.607 y 0.355), no calibrado.
    2. Las pruebas manuales que provocan 5xx (parar la API, etc.) **contaminan `eval/run-auto.mjs`**: el runner descarta las alertas que
       empezaron antes de la inyección. Esperar a que no haya alertas activas antes de correrlo.
    3. Correr la **suite E2E completa con el stack levantado puede agotar la memoria** del equipo (caídas de navegador, "worker process exited");
       no dar por fallidas esas pruebas: re-correr con memoria libre.
    4. En el primer arranque, **Alloy envía logs anteriores a la retención de Loki (7 d) y Loki los rechaza** (unos 10 errores en un segundo,
       sin repetición): es inofensivo.
    5. **`terraform plan` nunca queda limpio contra LocalStack:** no conserva las opciones de metadatos (IMDSv2) y Terraform las vuelve a proponer.
    6. **AMI válida para LocalStack:** `ami-785db401` (Ubuntu 16.04 del catálogo precargado de LocalStack 3.x); otros IDs dan `InvalidAMIID.NotFound`.
       En LocalStack el `user_data` no se ejecuta (`-var deploy_local_stack=true` levanta el compose contra el Docker local).
    7. Si no hay respuesta de proveedores LLM, el incidente queda `pendiente` y el correo avisa (evidencia RNF-01); no es un fallo del workflow.
    8. **Botón "Generar documentación" del panel:** el incidente no guarda los labels de la alerta, así que no conoce `evidence`; usa un criterio
       de respaldo (evidencia de Nginx si el nombre empieza con "Nginx", ninguna para el resto). Solo n8n manda los labels reales.
    9. **Cómo se identifica un contenedor detenido:** el Nginx con resolver por request escribe `<nombre> could not be resolved` en el error.log
       cuando el contenedor desaparece del DNS de Docker; ese nombre es lo que `DockerContainerDirectory` traduce y consulta. Si se vuelve a un
       `upstream` con nombre fijo, se pierde esa señal (y reaparece el defecto de la IP vieja).
   10. **Regla de alerta nueva = decidir su label `evidence`** (`nginx`, `target` o ninguno). Sin label no tiene evidencia (la API no lo infiere).
  - **Pendientes del autor:** aplicar en AWS, estudio de MTTR con personas, subir cambios (CI) y fusionar
    `feat/dashboard-nextjs` en `master` (master aún tiene el panel en Vite). El doc de prueba `DotnetMigrationE2E` ya se borró (15/11).
- ⏳ Sprint 6 pendiente: aplicar `terraform/environments/aws` en una cuenta real + memoria académica final (Informe Final)

---

## Estructura del proyecto

```
D:\DEV\argos/
├── docker-compose.yml                        — Compose raíz (include de 3 stacks)
├── .github/workflows/ci.yml                  — CI (6 jobs: terraform, observability-config, compose, argos-api, membresia-api, dashboard)
├── docs/
│   ├── Anexoll_Acosta_Juan_Agustin_V2.docx.md — Propuesta académica
│   ├── PRODUCT_BACKLOG.md, EVALUACION_OE3.md, FIXES_DASHBOARD.md, VALIDACION_API.md, Informe_Final_*.docx/.pdf
├── eval/                                     — Evaluación del OE3 (scenarios.json, faults.ps1, run-llm-eval.mjs, run-auto.mjs, results/)
├── terraform/
│   ├── modules/argos-bootstrap/              — Arranque común a cualquier nube: secretos generados + cloud-init (base.sh, deploy.sh.tftpl)
│   ├── modules/argos-node/                   — AWS: VPC, SG, EC2 (IMDSv2, gp3 cifrado), EIP; usa argos-bootstrap
│   ├── modules/argos-node-gcp/               — GCP: red, firewall, Compute Engine (Shielded VM), IP fija; usa argos-bootstrap
│   └── environments/
│       ├── localstack/                       — argos-node contra LocalStack (+ localstack.tf: deploy_local_stack opcional)
│       ├── aws/                              — argos-node contra AWS real (sin aplicar)
│       └── gcp/                              — argos-node-gcp contra Google Cloud (nube elegida el 16/11; sin aplicar aún)
├── docker/
│   ├── monitoring/                           — Stack observabilidad + Qdrant
│   │   ├── docker-compose.yml
│   │   ├── prometheus/ (config + rules)
│   │   ├── loki/ (config + rules)
│   │   ├── grafana/ (dashboards provisioned)
│   │   ├── alertmanager/ (routing config)
│   │   ├── knowledge/ (runbooks del entorno; se montan en la API, no van en su imagen)
│   │   └── .env.example
│   ├── proxy/                                — Nginx + Alloy
│   │   ├── docker-compose.yml
│   │   ├── nginx/ (config + Dockerfile)
│   │   └── alloy/ (log collector config)
│   └── apps/                                 — API .NET + SQL Server + db-init
│       ├── docker-compose.yml
│       ├── db/init-db.sql                    — Esquema + SP + seed (idempotente)
│       └── api/MembresiaAPI/ (.NET 10 Minimal)
├── argos-api/                                — Argos API (.NET 10), solución Argos.slnx
│   ├── Argos.Api/ (Controllers MVC, Authentication JWT, Binding, Errors, Filters, Hosting)
│   ├── Argos.Business/ (Services: Auth/Users/Settings/Llm/Rag/Incidents/Audit/Notifications/
│   │                    Observability/LoadTests; Security; Seeding)
│   ├── Argos.Repository/ (repositorios Qdrant, Embeddings ONNX, Loki/Prometheus, LLM providers, k6/Docker)
│   ├── Argos.Entities/ (entidades de dominio + Dtos/)   Argos.Common/ (config por entorno, excepciones)
│   ├── Argos.Tests/ (xUnit, 106 pruebas; no entra en la imagen)
│   └── Dockerfile + README.md
├── dashboard/                                — Sprint 5: panel propio (Next.js App Router + TS, export estático)
│   ├── src/ (app/: una ruta por vista · app-shell/: estado + layout · components/ · routes.ts · api.ts)
│   ├── Dockerfile (next build → out/ → nginx) + nginx.conf (proxy /api → rag-api)
│   └── package.json
├── k6/                                       — Tests de carga (Sprint 6): scripts + runner + RESULTS.md
├── .gitignore
├── README.md                                 — Guía de despliegue
└── CLAUDE.md                                 — Este archivo
```

---

## Despliegue local (referencia rápida)

```powershell
cd D:\DEV\argos

# 1. Crear red compartida (una sola vez)
docker network create argos-net

# 2. Levantar todos los stacks
docker compose up -d --build

# 3. Verificar
docker compose ps
curl http://localhost/api/status
curl http://localhost:3000   # Grafana
curl http://localhost:9090   # Prometheus
curl http://localhost:6333/readyz  # Qdrant
```

### Limpiar

```powershell
docker compose down           # bajar servicios
docker compose down -v        # bajar + borrar volúmenes
```

---

## Credenciales y acceso (desarrollo)

| Servicio | Usuario | Contraseña | URL |
|----------|---------|-----------|-----|
| Grafana | admin | admin | http://localhost:3000 |
| SQL Server | sa | YourStrong@Passw0rd | localhost:1433 |
| Nginx reverse proxy | — | — | http://localhost / https://localhost:8443 |

**⚠️ Producción:** cambiar todas las credenciales en `.env` / variables

---

## Instrucciones técnicas por rol

### 1. Trabajar con Terraform

**Archivo clave:** `terraform/modules/argos-node/` (la infraestructura); los entornos `terraform/environments/localstack` y
`terraform/environments/aws` solo lo invocan (`main.tf`/`variables.tf`/`outputs.tf` idénticos; difieren `providers.tf` y el `terraform.tfvars`,
que se copia de `terraform.tfvars.example` y no se versiona).

Cambios típicos:
- Dimensionamiento: `instance_type` (default `t3.xlarge`: 4 vCPU/16 GiB; con 8 GiB no alcanza), `root_volume_size_gb`
- Acceso: `admin_cidr_blocks` (obligatoria; panel 3001, Grafana 3000 y SSH) y `ssh_public_key`; solo 80 y 8443 quedan abiertos a Internet
- Qué se despliega: `repo_url`/`repo_ref` y el mapa `argos_env` (se escribe como `.env` en la VM)
- **Nube elegida: GCP** (decisión del autor, 2026-11-16): `environments/gcp` + `modules/argos-node-gcp` (`project_id`, `region`, `zone`,
  `machine_type` default `e2-standard-4`; credenciales con `gcloud auth application-default login`). El arranque es el módulo común
  `argos-bootstrap`. AWS queda como alternativa sin aplicar. LocalStack: `ami_id = "ami-785db401"`; su instancia es simulada (el `user_data` no corre);
  `-var deploy_local_stack=true` levanta el compose contra el Docker local

**Comandos:**
```powershell
cd terraform/environments/localstack   # o environments/aws (credenciales por AWS_PROFILE / AWS_ACCESS_KEY_ID)
terraform init
terraform plan      # contra LocalStack nunca queda limpio (opciones de metadatos de IMDSv2): es del simulador
terraform apply     # aws: tiene costo; en la VM el arranque lo hace cloud-init y deja el comando argos-deploy
terraform destroy
terraform fmt -check -recursive ; terraform validate   # lo mismo que corre el CI
```

### 2. Trabajar con Docker Compose

**Stack monitoring** (`docker/monitoring/`):
- `docker compose -p argos-monitoring up -d` — levantar
- `docker compose -p argos-monitoring logs -f prometheus` — ver logs servicio específico
- `docker compose -p argos-monitoring down` — bajar
- `docker compose -p argos-monitoring down -v` — bajar + borrar volúmenes

Análogo para `proxy` y `apps`.

**Cambios típicos:**
- Añadir servicios en `docker-compose.yml` (ej: Qdrant para RAG en Sprint 3)
- Modificar variables de entorno en `.env`
- Ajustar resource limits (CPU, memoria)

### 3. Alertas y reglas

**Prometheus alertas:** `docker/monitoring/prometheus/rules/`
- Añadir alerta: nuevo `- alert:` en `nginx.yml` o `infrastructure.yml`
- Cambiar umbral: editar `expr` o condición `for`
- Probar: ejecutar `curl http://localhost:9090/api/v1/rules`

**Loki alertas:** `docker/monitoring/loki/rules/fake/nginx-http.yml`
- Sintaxis LogQL (filtros + aggregations)
- Test: `curl -G 'http://localhost:3100/loki/api/v1/query' --data-urlencode 'query={service="nginx"}'`

**Alertmanager routing:** `docker/monitoring/alertmanager/alertmanager.yml`
- Routes por severity / alertname
- Receivers: email (`email-default`/`email-critical`, con placeholders SMTP: **no** usan el pool) y `webhook-n8n` (el que cuenta; reintroducido en el Sprint 4)

### 4. Configurar Grafana

**Dashboards** (`docker/monitoring/grafana/provisioning/dashboards/`):
- Archivos JSON presentes: nginx-overview, infrastructure, nginx-logs
- Cargar nuevo dashboard: export JSON desde Grafana UI → guardar en carpeta → reiniciar container
- UID debe ser único (ver datasources.yml para UIDs)

**Datasources** (`docker/monitoring/grafana/provisioning/datasources/datasources.yml`):
- Prometheus UID: `prometheus-uid` (cambiar si falta o hay conflict)
- Loki UID: `loki-uid`

### 5. Logs y troubleshooting

**Ver logs del provisioning:**
```powershell
terraform apply -out=plan.tfplan  # Guardar plan
terraform apply plan.tfplan       # Verbose output
# En la VM de AWS: sudo cat /var/log/cloud-init-output.log ; argos-deploy para volver a desplegar
```

**Logs de aplicación en Loki** (desde noviembre): `{job="docker", container="membresia-api"}` y `membresia-sqlserver` (solo contenedores con label `argos.logs=true`; los de la plataforma se miran con `docker logs`)

**Docker logs:**
```powershell
docker logs -f argos-monitoring-prometheus-1  # Último servicio (sufijo varía)
docker logs --since 5m container-name         # Últimos 5 min
```

**Verificar conectividad inter-contenedores:**
```powershell
docker exec nginx-proxy ping prometheus    # Ping entre redes
docker inspect network argos-net           # Ver contenedores en red
```

---

## Cambios futuros esperados (Roadmap)

### Sprint 3: Base de conocimiento + RAG

- Añadir servicio `qdrant` en `docker/monitoring/docker-compose.yml`
- Crear pipeline RAG: logs Loki → embedding (nomic-embed-text o API Claude) → Qdrant
- Inicializar colecciones Qdrant con incidentes históricos (opcional)

### Sprint 4: Orquestación n8n + LLM (Gemini/Claude) — ✅ implementado, cierre pendiente

- ✅ `n8n` (SQLite) + `rag-api` en `docker/monitoring/docker-compose.yml`
- ✅ Workflow `alert-analysis.json` importado (webhook → umbral → RAG → Route LLM → LLM Call → email)
- ✅ **Dual-provider**: `LLM_PROVIDER` (`gemini`|`anthropic`); default `gemini-flash-latest` (alias
  auto-actualizable — los pins tipo `gemini-2.x-flash` ya están deprecados). La key va
  por `$env` (header `x-api-key` Anthropic / query `?key=` Gemini), sin credencial n8n para el LLM
- ✅ Ruta `webhook-n8n` en `alertmanager.yml`
- ✅ Credencial SMTP (Gmail App Password) adjunta al nodo email; workflow **activo**
- 🔄 Destinatarios: `toEmail` ya no sale de `$env.ALERT_RECIPIENT_EMAIL` (queda como fallback) sino del pool
  configurado en el dashboard (`GET rag-api/notifications/recipients`). Ver "Pool de destinatarios" arriba.
- ✅ **Validación e2e Fase 1** (payload simulado → email enviado, `status=success`)
- ⏳ Pendiente opcional: Fase 2 (alerta real vía Alertmanager, cubierta por k6 en Sprint 6)

### Sprint 5: Auto-documentación + Dashboard — ✅ validado E2E (2026-09-23)

**Decisiones:** persistencia **Qdrant-only** vía `rag-api` (no SQL/Postgres); dashboard **SPA React+Vite** (migrado a Next.js el 2026-11-10).

- ✅ Argos API (`rag-api`) completa: auth JWT, CRUD usuarios, settings, CORS, `/incidents*` (list/get/patch/metrics/
  KB/document/observability/audit), `/observability/services`, `/notifications`, `/activity` — sin stubs.
  Observabilidad real: queries a Loki y Prometheus con graceful degradation (retorna `[]` si no responden).
- ✅ **Auto-seed** al arrancar (`SeedService`): carga 5 runbooks de `knowledge/` + 15 incidentes
  demo (estados: nuevo/pendiente/resuelto/auto_documentado; severidades mixtas; MTTR; docs markdown
  embebidos). Flags: `ARGOS_AUTO_SEED=true` (default), `ARGOS_SEED_DEMO=true` (default). Idempotente.
- ✅ **Fix KB**: `GET /incidents/knowledge-base` filtra por `estados=[documentado_previamente, auto_documentado]`
  (antes filtraba por `fuente=auto-generado` y quedaba vacío).
- ✅ Dashboard `dashboard/` (React+Vite+TS): 6 vistas (Dashboard, Incidentes, Base de Conocimiento,
  Métricas, Usuarios, Configuración). Servicio `argos-dashboard` (3001) en stack monitoring; nginx proxya
  `/api`→`rag-api` (sin CORS). Polling cada 20s. Auth JWT con persistencia de sesión.
- ✅ n8n: rama `Is New?` → genera doc con LLM → `POST rag-api/upsert`. Workflow activo.

### Sprint 6: Validación + Cloud

- ✅ Stack k6 con 3 escenarios (normal, spike, ataque) — validado E2E (`k6/`, `k6/RESULTS.md`)
- ✅ **k6 on-demand desde el dashboard** (vista "Pruebas de Carga"): `rag-api` lanza `grafana/k6`
  como contenedor hermano vía Docker socket + `Docker.DotNet`; volúmenes nombrados `argos_k6_scripts`/
  `argos_k6_results`; corridas en Qdrant (`argos_loadtests`). Trigger solo Admin, una corrida a la vez.
  - **Gotcha 1:** k6 corre como uid 12345 → lanzar el sibling con `user="root"` para que escriba el
    `--summary-export` en el volumen (de root), si no → `permission denied`.
  - **Gotcha 2:** en el JSON de `--summary-export`, las métricas Rate (`http_req_failed`, `checks`)
    guardan la fracción en la clave `value` (no `rate`); Trend usa `p(95)`/`avg`, Counter `count`/`rate`.
- ✅ Evaluación del OE3 cerrada (2026-10-28), línea base manual de MTTR no medida (limitación declarada)
- ✅ IaC portable (módulo `argos-node` + `localstack`/`aws`), CI y logs de aplicación (cierre de noviembre)
- ⏳ **Aplicar `terraform/environments/aws` en una cuenta real** y verificar cloud-init en la VM (no hecho)
- ⏳ Memoria académica final (Informe Final) y, si se quiere, estudio de MTTR con personas

---

## Herramientas recomendadas

- **VS Code** con extensiones: Terraform, Docker, C# (Omnisharp), PowerShell
- **Postman / Insomnia** para testear endpoints API
- **DBeaver** para administrar SQL Server
- **SQLTools** VS Code extension para queries rápidas
- **Grafana UI** para dashboards y exploración de datos

---

## Referencias rápidas

### Comandos Docker útiles

```powershell
# Listar contenedores de proyecto
docker ps --filter "name=argos-"

# Ejecutar comando en contenedor
docker exec -it membresia-api powershell

# Copiar archivo de/hacia contenedor
docker cp local-file.txt container-id:/path/

# Reiniciar servicio específico
docker compose -p argos-monitoring restart prometheus

# Ver uso de recursos
docker stats
```

### Comandos Terraform útiles

```powershell
terraform fmt                      # Formatear HCL
terraform validate                 # Validar sintaxis
terraform show                      # Ver estado actual
terraform state list               # Listar recursos tracked
terraform state rm resource-name   # Remover recurso de state
```

### URLs de documentación externa

- **Prometheus:** https://prometheus.io/docs/
- **Loki:** https://grafana.com/docs/loki/latest/
- **Grafana:** https://grafana.com/docs/grafana/latest/
- **Alertmanager:** https://prometheus.io/docs/alerting/latest/
- **Terraform AWS provider:** https://registry.terraform.io/providers/hashicorp/aws/latest
- **LocalStack docs:** https://docs.localstack.cloud/
- **n8n workflows:** https://docs.n8n.io/ (Sprint 4+)
- **Claude API:** https://docs.anthropic.com/ (Sprint 4+)

---

## Contacto / Soporte

- **Alumno:** Juan Agustín Acosta (juanagustinacost@gmail.com)
- **Email trabajo:** aacosta@desarrollosnea.com
- **Profesor:** Rios, Leopoldo Jose (ljr@comunidad.unne.edu.ar)

---

## Control de cambios

| Fecha | Autor | Cambio |
|-------|-------|--------|
| 2026-08-03 | Claude Code | Creación inicial: migración infra-observability → argos (Sprints 1-2) |
| 2026-08-10 | Claude Code | Corrección doc RAG (Voyage→FastEmbed local, 384/0.63). API HTTP del RAG (`rag/api.py`, servicio `rag-api`). Sprint 4: servicio `n8n` (SQLite), receiver `webhook-n8n`, workflow `alert-analysis.json` |
| 2026-08-11 | Claude Code | Sprint 4 (~85%): `argos-n8n` up+healthy y `argos-rag-api` up; workflow importado en n8n. Soporte **dual LLM** (Gemini\|Claude) vía `LLM_PROVIDER` + nodos `Route LLM`/`Parse`. `.env` con `N8N_ENCRYPTION_KEY` + `GEMINI_API_KEY`. Pendiente: credencial SMTP + activar workflow + validación e2e |
| 2026-08-11 | Claude Code | Sprint 4 **validado Fase 1** (~95%): modelo → `gemini-flash-latest` (los pins 2.x están retirados); fix healthcheck n8n (`127.0.0.1`, no `localhost`/IPv6); dedup workflows + `id` estable `argos-alert-analysis`; credencial SMTP Gmail adjunta al nodo email; workflow activo; e2e simulado `status=success` + email enviado |
| 2026-08-11 | Claude Code | Sprint 5 (~70%, base): persistencia **Qdrant-only** vía `rag-api` (`list/get/update_incident`, `compute_metrics`; modelo `Incident` enriquecido). Endpoints `/incidents*` + `/metrics` + `/knowledge-base` + CORS en `rag/api.py`. Rama **auto-doc** en workflow n8n (`Is New?` → genera doc → `POST /upsert`). **Dashboard** propio React+Vite (`dashboard/`, servicio `argos-dashboard`:3001). Pendiente: validación e2e |
| 2026-09-23 | Claude Code | Sprint 5 **validado E2E** (análisis profundo + cierre funcional): (1) Fix KB: filtro por estados `[documentado_previamente, auto_documentado]` (antes `fuente=auto-generado` → KB siempre vacía). (2) `argos_rag/seeding.py`: `seed_knowledge` + `seed_demo_incidents` (15 incidentes demo con estados/severidades/MTTR/docs) + `run_startup_seed`. (3) Auto-seed en `lifespan` de `api.py` vía `ARGOS_AUTO_SEED/SEED_DEMO`. (4) Warning LLM key faltante en startup. (5) `qdrant_store._build_filter` + `list_incidents` con parámetro `estados` (MatchAny). (6) Env vars `ARGOS_AUTO_SEED/SEED_DEMO` en docker-compose y `.env.example`. CLAUDE.md actualizado. |
| 2026-10-05 | Claude Code | Sprint 6 **k6 on-demand desde el dashboard** (validado E2E): nueva vista "Pruebas de Carga" ejecuta los 3 escenarios on-demand (solo Admin) + visualiza su código. `rag-api` lanza `grafana/k6` como contenedor hermano vía Docker socket + SDK `docker==7.1.0`; volúmenes nombrados `argos_k6_scripts`/`argos_k6_results`; corridas en Qdrant (`argos_loadtests`). Nuevo módulo `argos_rag/loadtest.py` + endpoints `/loadtests/*` + `LoadTestsView.tsx`. Gotchas resueltos: k6 como `user=root` (si no, `permission denied` al escribir el summary) y `--summary-export` Rate usa clave `value` (no `rate`). |
| 2026-10-07 | Claude Code | **Observabilidad en el dashboard** (validado E2E): panel de golden signals de Nginx en la vista Métricas (`NginxSignals.tsx`): requests por status code + latencia P50/P95/P99 + error rate 5xx como series temporales desde Loki (selector de rango). Nuevo `nginx_metrics()` en `observability.py` + endpoint `GET /observability/nginx-metrics`. **Fix Salud de Servicios**: `services_health()` pasó de `up{job}` de Prometheus (jobs `api`/`rag-api`/`qdrant` inexistentes → todo `unknown`) a **probes HTTP directos** sobre `argos-net` (URLs por env). Gotcha Loki: `query_range` alinea samples a múltiplos del `step` → alinear la grilla (`(start//step)*step`) o devuelve 0. Stack corre bajo project `argos-platform`. |
| 2026-10-12 | Claude Code | **Pool de destinatarios de alertas** (implementado, E2E pendiente): `alert_recipients` en `argos_settings` (`normalize_recipients`/`get_alert_recipients` en `meta_store.py`); `PATCH /settings` valida (422); nuevo `GET /notifications/recipients` (token opcional `ARGOS_INTERNAL_TOKEN` / header `X-Argos-Token`); workflow n8n con nodos `Get Recipients` + `Resolve Recipients` (fallback `ALERT_RECIPIENT_EMAIL`, `recipients_source`); tarjeta "Notificaciones por email" en `SettingsView.tsx`; compose/.env.example actualizados. Fallas posibles documentadas en el backlog. |
| 2026-10-14 | Claude Code | **Prueba integral de la plataforma** (stack completo `argos-platform`): pool de emails configurado (gmail+hotmail) y validado E2E (n8n #30 simulada, #31 alerta real por k6 attack, #36/#41 auto-doc → `recipients_source=pool`). **Fix k6 attack on-demand**: faltaba `/data/payloads.json` → bind `k6/data:/app/k6_data`, copia al volumen de scripts y montaje extra en `/data` (`loadtest.py`). **Fix truncado Gemini**: `gemini-flash-latest` es thinking y los thoughts consumían `maxOutputTokens` (1024/1536 → todas las respuestas `MAX_TOKENS`, email y doc cortados) → 4096 (análisis) / 6144 (doc) en el workflow y `llm.py`; workflow re-importado y activo. Nota: el `Prepare` de n8n descarta alertas `warning` sueltas (umbral `ALERT_THRESHOLD=3` o `critical`), por diseño. |
| 2026-10-12 | Claude Code | **Configuración funcional** (validado E2E): LLM/umbral/auto-doc/polling del panel Configuración ahora tienen efecto (`resolve_llm`, `check_llm`, `validate_settings_patch`, `GET /internal/config`, `/query` con umbral de settings); Integraciones → estado en vivo (`GET /integrations/status`); workflow n8n con `Get Config` + `Auto Doc?` + `Build Pending Incident`. Detectada `ANTHROPIC_API_KEY` inválida. |
| 2026-10-19 | Claude Code | **Argos API en .NET 10** (`argos-api/Argos.slnx`): capas `Argos.Entities` (entidades + DTOs), `Argos.Repository` (repositorios Qdrant con `QdrantRepository<T>`, embeddings ONNX, Loki/Prometheus, `ILlmProvider` Gemini/Anthropic, k6 vía Docker.DotNet), `Argos.Business` (servicios con interfaz), `Argos.Api` (controllers MVC + auth JWT + binder de DTOs) y `Argos.Common`; DI con módulos Autofac. Pasa a ser la **única API**: se eliminó la implementación Python (`rag/`, servicio `rag-api-python`, volumen `fastembed_cache`). Validado: endpoints vía `:3001/api`, n8n, LLM doc, k6 start/409/stop. |
| 2026-10-26 | Claude Code | **LLM con fallback + salud + notificaciones**: un `CAdvisorDown` real murió en n8n por 503/timeout de `gemini-flash-latest` sin dejar incidente ni aviso. Ahora n8n llama a `POST /internal/llm/generate` (cadena principal → `LLM_FALLBACK_CHAIN`), proveedores `GroqProvider`/`OpenRouterProvider` (`OpenAiCompatibleProvider`), `LlmHealthTracker` pasivo con cooldown, `GET /llm/health` + `POST /llm/health/check`, notificaciones con `kind` (`incident`/`llm_health`/`pipeline_error`) + `read-all`; header con badge de IA real y campana con navegación al incidente; tarjeta "Estado de los modelos IA" en Configuración. Workflow: sin `Route LLM`/`Route Doc LLM`, nodos LLM con `onError=continue`, el incidente se persiste aunque falle la IA. n8n ya no recibe keys de LLM. Validado: flash-latest timeout → flash-lite responde (`fallback_used=true`). |
| 2026-11-10 | Claude Code | **Dashboard migrado de Vite a Next.js** (lo pedía la propuesta, Anexo II): Next.js 16 App Router + React 19, export estático servido por el mismo nginx, una ruta por vista (`src/app/`, `src/routes.ts`), estado en `src/app-shell/AppProvider.tsx`, Sidebar con `<Link>`, 404 propio, `/usuarios` solo Admin. E2E `routing.spec.ts` (deep links, F5, historial, login con deep link, `?estado=`, 404) + suite adaptada (k6: 4 escenarios). Resultado 80 ✓ / 4 ✗ (las 4 fallan igual con Vite). Docs, presentación e Informe Final actualizados. |
| 2026-11-12 | Claude Code | **Cierre de noviembre** (11 y 12/11): Terraform rehecho como módulo `argos-node` + entornos `localstack`/`aws` (verificado contra LocalStack 3.8, **sin aplicar en AWS**), CI `.github/workflows/ci.yml` (6 jobs, 82 tests xUnit en `Argos.Tests`, aún sin correr en GitHub), Alloy con logs de contenedores (`job="docker"`), evidencia de logs en el prompt (`/internal/evidence`, nodo `Get Evidence`) con seudonimización de IP, señal por nombre de alerta en el RAG (piso 0.30; re-evaluación 7/7), fix de Nginx (resuelve por request: ya no hace falta reiniciar `argos-dashboard`), panel F-02/F-03/F-04/F-05/F-06/F-10 implementados (**suite E2E completa sin re-correr por falta de memoria**; F-12 abierto), API de ejemplo registra sus errores. Docs al día: EVALUACION_OE3 §6, FIXES_DASHBOARD, PRODUCT_BACKLOG, README de `eval/` y `argos-api/`. |
| 2026-11-15 | Claude Code | **Evidencia inferida desde Nginx** (decisión del autor: la API de membresías es un servicio independiente y Argos no debe conocer sus contenedores): label `evidence` (`nginx`/`target`) en las reglas de Prometheus y Loki; el upstream sale de `upstream_addr` y del error.log y `DockerContainerDirectory` lo traduce a contenedor (estado vía Docker) antes de pedir sus logs a Loki; se eliminaron los nombres fijos de `AlertEvidencePlanner` y la consulta a la base. Alloy pasa a **opt-in** con el label `argos.logs=true` (13 → 2 flujos en Loki). n8n envía los labels; el botón del panel usa criterio de respaldo. `eval/scenarios.json` con `labels`. 94 pruebas unitarias en verde. Corridas F1/F2/F7 del 15/11 con arreglo exacto en las tres (N = 1). Docs: EVALUACION_OE3 §6.2 y §6.5, PRODUCT_BACKLOG, `eval/README.md`. |
| 2026-11-15 | Claude Code | **Contexto del entorno a configuración** (decisión del autor: los nombres de contenedores escritos en los prompts eran otra forma de que Argos conociera el servicio): campo `environment_context` en `argos_settings`, editable en Configuración (solo Admin, tope 3000, 422 si se excede), que n8n recibe en `Get Config`; valor inicial `ARGOS_ENVIRONMENT_CONTEXT` (el `.env.example` trae la descripción anterior); vacío = prompt genérico; el código conserva solo una instrucción genérica (n8n, `LlmService`, `IncidentDocumentPrompt`). 102 pruebas unitarias en verde. Corridas F1/F2/F7: F1 y F7 con arreglo exacto, F2 parcial (N = 1; la diferencia con la corrida previa no se atribuye al cambio). El campo no se vio en un navegador. Docs: EVALUACION_OE3 §6.7, PRODUCT_BACKLOG, FUNCIONALIDADES_DASHBOARD. |
| 2026-11-15 | Claude Code | **Runbooks fuera de la imagen de Argos**: `argos-api/Argos.Api/knowledge/` → `docker/monitoring/knowledge/` (volumen `:ro` sobre `/app/knowledge`); el `.csproj` ya no los copia; `DemoIncidentCatalog` sin nombres del servicio de ejemplo. Contraseña `sa` eliminada del runbook `nginx-5xx.md` y de 17 incidentes de Qdrant (vía `PATCH`). Borrado el doc `DotnetMigrationE2E`. 106 pruebas en verde; RAG 7/7 conocidas. Suite E2E completa: 101/103 ok, 2 omitidas; corregido M-04 (latencia de Nginx agrupada `by (service)`) y +3 pruebas de C-08. |
| 2026-11-15 | Claude Code | **Cierre de documentación**: Informe Final (87 páginas) con los runbooks fuera de la imagen, la contraseña en la base de conocimiento, la suite E2E real (101/103), el defecto de latencia M-04 y los desvíos respecto de la propuesta (LocalStack comunitario, nodo único, CI sin CD); presentación, SPEECH y `ARQUITECTURA.html` al día. Tercera tanda F1/F2/F7 ×3 **interrumpida por falta de memoria** en la segunda corrida (F1 exacto, F2 no): acumulado del 15/11 F1 3/3, F7 2/2, F2 1/3 (`EVALUACION_OE3.md` §6.8). Rama `feat/dashboard-nextjs` commiteada pero **sin subir** (push pendiente del autor). |
| 2026-11-16 | Claude Code | **Terraform para GCP** (decisión del autor: desplegar en Google Cloud, unas horas y destruir): el arranque (secretos + cloud-init) se extrajo a `modules/argos-bootstrap` (con bloques `moved` en `argos-node`), módulo nuevo `modules/argos-node-gcp` (red, firewall 80/8443 públicos y 3001/3000/22 por CIDR, Compute Engine Ubuntu 24.04 con Shielded VM y sin cuenta de servicio, IP fija) y entorno `environments/gcp`; CI valida los tres entornos. `fmt` y `validate` OK y cloud-init renderizado en local; **sin `plan`/`apply` en GCP** (falta cuenta) y **sin re-verificar contra LocalStack** tras la extracción (Docker apagado, poca memoria). El CI corrió en GitHub por primera vez el 16/11: 6 de 6 jobs en verde. Dimensionamiento: 7 vCPU de la propuesta no hacen falta; el límite es la memoria (medición pendiente). |
