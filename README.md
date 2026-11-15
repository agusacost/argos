# Argos: Plataforma inteligente de AIOps

**Argos** es una plataforma de AIOps para la detección, resolución asistida y documentación automática de incidentes en entornos web HTTP. Se construye sobre un stack de observabilidad open-source (Prometheus, Loki, Grafana, Alertmanager) integrado con inteligencia artificial para sintetizar problemas, proponer pasos de resolución y documentar incidentes automáticamente.

**Proyecto Final de Carrera** — Facultad de Ciencias Exactas y Naturales y Agrimensura (UNNE), 2026  
**Alumno:** Juan Agustín Acosta (LU 57851)  
**Profesor orientador:** Rios, Leopoldo Jose  

---

## Fases de implementación (Roadmap)

| Sprint | Objetivo | Duración | Estado |
|--------|----------|----------|--------|
| **Sprint 1-2 (Migrado)** | Infraestructura base + stack observabilidad | 4 sem | ✅ Completado (migrado de infra-observability) |
| **Sprint 3** | Base de conocimiento vectorial + RAG | 2-3 sem | ✅ Completado |
| **Sprint 4** | Orquestación en n8n + API Claude | 3 sem | 🔄 En progreso (config + workflow; falta validación E2E) |
| **Sprint 5** | Auto-documentación de incidentes | 2 sem | ⏳ Pendiente |
| **Sprint 6** | Validación con k6 + despliegue cloud | 2 sem | ⏳ Pendiente |

---

## Arquitectura actual (Sprints 1-2 migrados)

```
┌─────────────┐
│   Usuario   │ (navegador / sistema)
└──────┬──────┘
       │ HTTP
       ▼
┌──────────────────────────────────┐
│  Nginx Reverse Proxy :80/:8443   │
│  (rate limiting, SSL termination)│
└────────────────────┬─────────────┘
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
   ┌─────────────┐           ┌──────────────┐
   │ API .NET    │           │  SQL Server  │
   │ Port 8080   │◄─────────►│  Port 1433   │
   └─────────────┘           └──────────────┘
        │
        │ Métricas + Logs
        ▼
   ┌─────────────────────────────────────────┐
   │  Observabilidad Stack                   │
   ├─────────────────────────────────────────┤
   │ • Prometheus (tsdb métricas)  :9090     │
   │ • Loki (storage logs)         :3100     │
   │ • Grafana (dashboards)        :3000     │
   │ • Alertmanager (alertas)      :9093     │
   │ • Alloy (log collector)                 │
   └─────────────────────────────────────────┘
```

### Stack de observabilidad (Docker: monitoring/)

- **Prometheus v2.54.1**: Almacén de métricas en series temporales (TSDB)
  - Scrape targets: node-exporter, nginx-exporter, cadvisor
  - Reglas de alerta: 13 alertas (4 nginx + 9 infraestructura)
  - Retention: 15 días (configurar en prometheus.yml)

- **Loki v3.2.0**: Almacén de logs indexado por labels
  - Fuente: logs JSON de Nginx vía Alloy
  - Reglas: 5 alertas LogQL (error rate, latencia, IP sospechosa, error spike)
  - Retention: 168h (7 días)

- **Grafana v11.3.0**: Visualización unificada
  - 3 dashboards provisionados: nginx-overview, infrastructure, nginx-logs
  - Credenciales dev: `admin` / `admin`
  - Datasources: Prometheus (UID: prometheus-uid) + Loki (UID: loki-uid)

- **Alertmanager v0.27.0**: Enrutamiento y agrupación de alertas
  - Receivers: email-default, email-critical (placeholders REPLACE_WITH_*)
  - Routes por severity: critical (1h repeat), warning (4h repeat)
  - **Nota:** el webhook a n8n para análisis IA se reintroducirá en Sprint 4

### Stack proxy (Docker: proxy/)

- **Nginx**: Reverse proxy + SSL termination
  - Upstream: membresia-api:8080
  - Rate limiting: zona "api" a 30 req/s + burst 100
  - Logs: JSON estructurado (15 campos) → `/var/log/nginx/access.log`
  - SSL: auto-firmado en puerto 8443

- **Alloy v1.4.3**: Recolector de logs (sucesor Promtail)
  - 2 pipelines file-based:
    - Access logs: JSON → parsing → labels (service, status, method, host, remote_addr) → Loki
    - Error logs: regex → labels (service, level) → Loki
  - Logs de aplicación: stdout/stderr de los contenedores de la plataforma vía socket de Docker
    (ver [Logs de aplicación con Alloy](#logs-de-aplicación-con-alloy))
  - Loki endpoint: http://loki:3100/loki/api/v1/push

- **Exporters**:
  - `node-exporter`: Métricas del host (CPU, RAM, disk, network)
  - `nginx-exporter`: Stub status de Nginx (conexiones activas, requests)
  - `cadvisor`: Métricas de contenedores (**DOWN en Podman/Windows**, limitación conocida)

### Stack apps (Docker: apps/)

- **API REST ASP.NET Core 10** (Minimal API)
  - Endpoints:
    - `GET /api/status` — health check
    - `GET /api/usuarios?pageNumber=1&pageSize=10` — lista paginada con SQL
  - Arquitectura: Controllers → Business → Repository → ADO.NET
  - Base de datos: MembresiaDB en SQL Server 2022

- **SQL Server 2022**
  - Credenciales dev: `sa` / `YourStrong@Passw0rd`
  - Base de datos: `MembresiaDB` (se inicializa automáticamente vía contenedor `db-init`)

- **Qdrant v1.12.4** (Sprint 3)
  - Puerto: 6333 (REST), 6334 (gRPC)
  - Colección: `incidentes` (la crea y siembra la Argos API al arrancar)

- **Argos API (.NET 10)** — `argos-api/`, contenedor `argos-rag-api`, puerto 8000 (alias `rag-api` en la red)
  - Backend del dashboard (vía nginx `/api`) y de n8n: RAG (`/query`, `/upsert`), auth JWT, usuarios,
    configuración, incidentes, observabilidad, auto-documentación LLM y pruebas de carga k6
  - Embeddings locales con ONNX Runtime (`paraphrase-multilingual-MiniLM-L12-v2`, 384 dims)
  - Solución `argos-api/Argos.slnx` en capas (Entities / Repository / Business / Controllers) con Autofac;
    ver `argos-api/README.md`

---

## Despliegue

### Requisitos previos

1. **Docker + Docker Compose v2.20+** instalados
2. (Opcional) **Terraform** + **LocalStack Pro** solo si se quiere gestionar la infra simulada de AWS

### Despliegue rápido (recomendado)

```powershell
cd D:\DEV\argos

# Una sola vez: crear la red compartida entre stacks
docker network create argos-net

# Levantar todos los servicios (monitoring + apps + proxy + qdrant)
docker compose up -d --build
```

El compose raíz (`docker-compose.yml`) incluye los 3 stacks vía `include`. El contenedor
`db-init` inicializa `MembresiaDB` automáticamente y termina; la API espera a que complete.

**Duración típica:** 3-5 minutos en primer run (incluye descarga de imágenes y build de la API).

#### Verificar despliegue

```powershell
# Ver estado de todos los contenedores
docker compose ps

# Smoke tests
curl http://localhost/api/status
curl "http://localhost/api/usuarios?pageNumber=1&pageSize=10"
curl http://localhost:9090/targets
curl http://localhost:3000
curl http://localhost:9093
curl http://localhost:6333/readyz
curl http://localhost:8000/health          # RAG API
curl http://localhost:5678/healthz         # n8n

# Probar el RAG (debería clasificar como documentado, is_documented=true).
# Requiere el token interno (ARGOS_INTERNAL_TOKEN de docker/monitoring/.env)
curl -X POST http://localhost:8000/query -H "Content-Type: application/json" `
  -H "X-Argos-Token: $env:ARGOS_INTERNAL_TOKEN" `
  -d '{"text":"alta tasa de errores 5xx en nginx"}'
```

#### Sprint 4: activar el flujo inteligente (n8n + Claude)

Tras `docker compose up -d --build`, el servicio `n8n` queda en `http://localhost:5678` pero el
workflow debe importarse y activarse una vez:

```powershell
# 1. Completar docker/monitoring/.env con ANTHROPIC_API_KEY y N8N_ENCRYPTION_KEY (ver sección Config)

# 2. Importar el workflow versionado (o hacerlo desde la UI: Import from File)
docker exec argos-n8n n8n import:workflow --input=/workflows/alert-analysis.json

# 3. En la UI (http://localhost:5678): crear la credencial SMTP para el nodo "Notify Email"
#    y activar el workflow "Argos — Alert Analysis & Resolution".

# 4. Probar el webhook con un payload simulado de Alertmanager (>= ALERT_THRESHOLD alertas)
curl -X POST http://localhost:5678/webhook/argos-alerts -H "Content-Type: application/json" `
  -d '{"status":"firing","commonLabels":{"alertname":"NginxHighErrorRate","severity":"warning"},"commonAnnotations":{"summary":"Tasa de errores 5xx > 5%"},"alerts":[{"labels":{"alertname":"NginxHighErrorRate","severity":"warning"}},{"labels":{"alertname":"NginxHighResponseTime","severity":"warning"}},{"labels":{"alertname":"NginxErrorLogSpike","severity":"warning"}}]}'
```

En *Executions* de n8n verás: RAG consultado → Claude genera síntesis + pasos → email enviado.

### Despliegue por stack (modo standalone)

Si preferís levantar los stacks por separado (requiere crear argos-net previamente):

```powershell
docker network create argos-net

# Monitoring (Prometheus, Loki, Grafana, Alertmanager, Qdrant)
docker compose -p argos-monitoring up -d
Start-Sleep 10

# Apps (SQL Server, db-init, API)
docker compose -p argos-apps up -d

# Proxy (Nginx, Alloy, exporters)
docker compose -p argos-proxy build --quiet
docker compose -p argos-proxy up -d
```

### Limpieza

```powershell
# Bajar todos los servicios
docker compose down

# Bajar y eliminar volúmenes (borra datos de BD, métricas, etc.)
docker compose down -v
```

### Infraestructura como código (Terraform: LocalStack y AWS)

```
terraform/
├── modules/argos-node/          — la infraestructura (compartida)
│   ├── main.tf                  VPC + subred pública + IGW, security group, EC2, Elastic IP
│   ├── bootstrap.tf             user_data (cloud-init) + secretos generados
│   ├── scripts/base.sh          Docker Engine + Compose plugin + git
│   └── templates/deploy.sh.tftpl  .env, clone del repo, argos-net, docker compose up -d --build
└── environments/
    ├── localstack/              provider con endpoints de LocalStack + credenciales de prueba
    └── aws/                     provider real (credenciales por la cadena estándar de AWS)
```

`main.tf`, `variables.tf` y `outputs.tf` son **idénticos** en los dos entornos: ambos instancian el módulo
`argos-node`. Lo que cambia es `providers.tf` (endpoints/credenciales) y los valores de `terraform.tfvars`.
LocalStack agrega además `localstack.tf` (bloque `moved` para el state anterior y el despliegue local opcional).

**Qué crea el módulo:** una VPC dedicada con una subred pública, un security group con superficie mínima y
una instancia Ubuntu 24.04 (por defecto `t3.xlarge`, 4 vCPU / 16 GiB, disco gp3 cifrado de 50 GB, IMDSv2
obligatorio) con Elastic IP.

| Puerto | Origen | Servicio |
|--------|--------|----------|
| 80, 8443 | Internet | Nginx (HTTP / HTTPS autofirmado) |
| 3001, 3000 | `admin_cidr_blocks` | Panel de Argos, Grafana |
| 22 | `admin_cidr_blocks` (solo si hay `ssh_public_key`) | SSH |

Prometheus, Loki, Alertmanager, Qdrant, n8n, la API y SQL Server **no** se exponen: se accede por túnel SSH
(`terraform output ssh_command`).

**Primer arranque (cloud-init):** instala Docker, escribe `/etc/argos/argos.env` desde `argos_env` (variable
`sensitive`; `N8N_ENCRYPTION_KEY`, `JWT_SECRET`, `ARGOS_INTERNAL_TOKEN` y `ARGOS_ADMIN_PASSWORD` se generan al azar
si no se pasan), clona `repo_url`@`repo_ref` en `/opt/argos`, copia el `.env` a `docker/monitoring/.env`, crea
`argos-net`, ejecuta `docker compose -p argos-platform up -d --build` e importa/activa el workflow de n8n.
Log en `/var/log/argos-deploy.log`. Para actualizar el código después: `sudo argos-deploy` en la VM.

#### AWS

```powershell
cd terraform\environments\aws
copy terraform.tfvars.example terraform.tfvars   # completar: admin_cidr_blocks, ssh_public_key, argos_env
$env:AWS_PROFILE = "mi-perfil"                    # o AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY
terraform init
terraform plan
terraform apply
terraform output urls
terraform output -raw argos_admin_password        # usuario admin@argos.local del panel
```

Después del apply (el build inicial tarda varios minutos):

1. Seguir el progreso: `ssh ubuntu@<ip> tail -f /var/log/argos-deploy.log`.
2. Crear la credencial SMTP del nodo "Notify Email" en n8n (túnel: `ssh -L 5678:localhost:5678 ubuntu@<ip>`,
   abrir http://localhost:5678) y reactivar el workflow.
3. `terraform destroy` al terminar (la instancia y la Elastic IP tienen costo).

**Secretos:** `terraform.tfvars` y el state están en `.gitignore`. El state y el user_data contienen el `.env`
en claro: no compartir el state; para trabajo en equipo usar un backend S3 cifrado.

#### LocalStack

```powershell
cd terraform\environments\localstack
copy terraform.tfvars.example terraform.tfvars
terraform init
terraform plan
terraform apply                                   # VPC, SG, EC2 y EIP simulados
terraform apply -var deploy_local_stack=true      # además: docker compose up con el Docker local
```

En LocalStack la EC2 es simulada: el user_data se registra pero no corre en una VM real, por eso
`deploy_local_stack` reproduce el `docker compose up` contra el Docker local (usa el `docker/monitoring/.env`
local). El despliegue local habitual sigue siendo `docker compose up -d --build` desde la raíz.

### Integración continua (GitHub Actions)

`.github/workflows/ci.yml` corre en cada push y pull request. Es **CI, no CD**: valida y compila, no despliega.

| Job | Qué valida |
|-----|-----------|
| `terraform` | `terraform fmt -check -recursive` + `init -backend=false` y `validate` de `localstack` y `aws` |
| `observability-config` | `promtool check config` / `check rules`, `amtool check-config`, `alloy fmt` + arranque del grafo de Alloy, JSON del workflow de n8n |
| `compose` | `docker compose config` del compose raíz (con `docker/monitoring/.env.example`) |
| `argos-api` | `dotnet restore/build` de `Argos.slnx` + `dotnet test` (corre los proyectos de test que existan) |
| `membresia-api` | `dotnet build` de `MembresiaAPI.csproj` |
| `dashboard` | `npm install` + `next build` (export estático); `npm install` porque `package-lock.json` está ignorado |

### Logs de aplicación con Alloy

Además de los archivos de Nginx, Alloy lee stdout/stderr de los contenedores **de la aplicación monitoreada** por
el socket de Docker (`discovery.docker` → `discovery.relabel` → `loki.source.docker`, socket montado `:ro`):

- Opt-in: solo los contenedores con el label de Docker `argos.logs=true` (hoy `membresia-api` y
  `membresia-sqlserver`, en `docker/apps/docker-compose.yml`). Los de la plataforma Argos quedan afuera.
- Etiquetas: `job="docker"`, `container="<nombre>"`. Sin `service`/`log_type`, así que no afectan a las reglas
  de Loki ni a los tableros, que filtran por `{service="nginx", ...}`.

```logql
{job="docker", container="membresia-api"} |= "Exception"
```

Argos no lee estos logs por nombre: los usa solo cuando Nginx reporta a ese contenedor como upstream de los errores
(ver `argos-api/README.md`, "Evidencia de logs"). Aplicar el cambio en un stack ya levantado:
`docker compose -p argos-platform up -d alloy api sqlserver`.

---

## Acceso a servicios

| Servicio | URL | Credenciales | Notas |
|----------|-----|--------------|-------|
| Grafana | http://localhost:3000 | admin / admin | Dashboards provisionados |
| Prometheus | http://localhost:9090 | — | Métricas, alertas activas |
| Alertmanager | http://localhost:9093 | — | Enrutamiento alertas |
| Loki | http://localhost:3100 | — | API logs, sin UI (usar Grafana Explore) |
| Qdrant | http://localhost:6333 | — | Vector DB; UI en `/dashboard` |
| RAG API | http://localhost:8000 (solo loopback) | — | `/health`; `/query`, `/upsert` con header `X-Argos-Token` (n8n) |
| n8n | http://localhost:5678 | (crear en 1er login) | Orquestador; workflow `alert-analysis` (Sprint 4) |
| API Status | http://localhost/api/status | — | Health check |
| API Usuarios | http://localhost/api/usuarios | — | Paginación: ?pageNumber=1&pageSize=10 |
| SQL Server | localhost:1433 | sa / YourStrong@Passw0rd | Conex. externa ADO.NET |

---

## Configuración (variables de entorno)

### `docker/monitoring/.env` (opcional)

```env
# Prometheus, Loki, Grafana — no requieren variables (config hardcoded)

# Alertmanager — SMTP email alerts
# Descomentar y llenar en producción:
# REPLACE_WITH_DESTINATION_EMAIL=tu-email@example.com
# REPLACE_WITH_SENDER_EMAIL=sender@gmail.com
# REPLACE_WITH_SMTP_USER=your-smtp-user
# REPLACE_WITH_SMTP_APP_PASSWORD=your-app-password

# n8n + Claude (Sprint 4)
N8N_ENCRYPTION_KEY=<openssl rand -hex 32>
ANTHROPIC_API_KEY=sk-ant-...            # requerido por el workflow Alert Analysis
CLAUDE_MODEL=claude-sonnet-4-6          # o claude-opus-4-8
RAG_API_URL=http://rag-api:8000
SIMILARITY_THRESHOLD=0.63
ALERT_THRESHOLD=3                       # alertas activas que disparan el análisis
ALERT_RECIPIENT_EMAIL=your-email@example.com
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=observabilidad@example.com
SMTP_PASSWORD=<app-password>
SMTP_FROM=observabilidad@example.com
# Contexto del entorno de los prompts (valor inicial; después se edita en Configuración del panel). Vacío = prompt genérico.
ARGOS_ENVIRONMENT_CONTEXT="Un único host con Docker Compose, sin Kubernetes ni systemd. Servicio monitoreado: membresia-api ..."
```

Copiá `docker/monitoring/.env.example` a `docker/monitoring/.env` y completá los valores reales.
`.env` está en `.gitignore` — nunca commitees la `ANTHROPIC_API_KEY`.

### `terraform/environments/<entorno>/terraform.tfvars`

Copiar el `terraform.tfvars.example` del entorno (`localstack` o `aws`). Ver
[Infraestructura como código](#infraestructura-como-código-terraform-localstack-y-aws).

---

## Alertas configuradas

### Alertas Prometheus (infraestructura)

| Alerta | Expresión | Umbral | Severidad |
|--------|-----------|--------|-----------|
| NginxDown | `up{job="nginx-exporter"} == 0` | — | critical |
| NginxHighActiveConnections | `nginx_connections_active > 800` | 800 conn | warning |
| NginxHighRequestRate | `rate(nginx_http_requests_total[5m]) > 500` | 500 req/s | warning |
| NginxWaitingConnections | `nginx_connections_waiting > 100` | 100 conn | warning |
| HostCPUUsage | `100 - avg(rate(node_cpu_seconds_total{mode="idle"}[5m])) * 100 > 80` | >80% | warning |
| HostMemoryUsage | `(1 - node_memory_MemAvailable_bytes / node_memory_MemTotal_bytes) * 100 > 85` | >85% | warning |
| HostDiskUsage | `(1 - node_filesystem_avail_bytes / node_filesystem_size_bytes) * 100 > 90` | >90% | warning |
| ExporterDown | `up == 0` (node, nginx, cadvisor) | — | critical |

### Alertas Loki (aplicación / HTTP)

| Alerta | Expresión LogQL | Umbral | Severidad |
|--------|-----------------|--------|-----------|
| NginxHighErrorRate | `5xx error% en 5m > 5%` | >5% | warning |
| NginxCriticalErrorRate | `5xx error% en 5m > 20%` | >20% | critical |
| NginxHighResponseTime | `P95 upstream latencia > 2s` | >2s | warning |
| NginxSuspiciousIPActivity | `>200 req/min desde IP única` | >200 req/min | warning |
| NginxErrorLogSpike | `>1 error/s en error.log` | >1 err/s | warning |

---

## Limitaciones conocidas (heredadas de infra-observability)

1. **cAdvisor DOWN en Podman/Windows**
   - Limitación: `/sys` mount no soportado desde Windows host
   - Impacto: sin métricas de contenedores (non-critical, node-exporter compensa)
   - Mitigación: documented, target DOWN esperado en Prometheus

2. **MembresiaDB se reinicializa tras `down -v`**
   - El contenedor `db-init` re-ejecuta `init-db.sql` en cada `docker compose up` (idempotente)
   - Si se hace `down -v`, el volumen `sqlserver_data` se elimina y la BD se recrea en el siguiente up

3. **SMTP credential n8n NO persiste** (cuando exista n8n en Sprint 4)
   - Limitación: se pierde al `down/up` del stack
   - Mitigación: guardar credential ID en n8n UI tras setup manual

4. **Logs de contenedores requieren el socket de Docker**
   - Alloy monta `/var/run/docker.sock` (Docker Engine en Linux y Docker Desktop lo soportan; con Podman en
     Windows el montaje puede fallar). Los logs de Nginx por archivo no dependen del socket.

---

## Roadmap futuro (Sprints 3-6)

- **Sprint 3:** ✅ Base vectorial Qdrant + RAG pipeline + API HTTP del RAG (`argos-api/`, .NET 10)
- **Sprint 4:** 🔄 Orquestador n8n + integración API Claude + workflow `alert-analysis` (config lista; falta validación E2E)
- **Sprint 5:** Auto-documentación: generar + persistir docs técnicas
- **Sprint 6:** k6 load testing (tráfico normal, spike, ataque) + validación MTTR + despliegue AWS/GCP

---

## Documentación

- **`docs/Anexoll_Acosta_Juan_Agustin.docx.md`** — Propuesta académica completa (objetivos, fases, metodología)
- **`CLAUDE.md`** — Instrucciones para Claude Code (context, tools, referencias técnicas)

---

## Historial de cambios

Migración completa de `D:\DEV\infra-observability` (Sprints 1-4) a `D:\DEV\argos` (Sprints 1-2 base):

- ✅ Terraform + LocalStack (todo funcional)
- ✅ 3 stacks Docker (monitoring, proxy, apps)
- ✅ Reglas de alerta Prometheus + Loki
- ✅ Grafana dashboards + provisioning
- ✅ Alloy log collection
- ✅ API .NET + SQL Server
- ✅ Qdrant + RAG (Sprint 3) · ✅ RAG API HTTP + n8n (SQLite) + Claude API vía workflow (Sprint 4, en validación)
- ⏸️ k6 scenarios — (pendiente Sprint 6)

---

## Contacto

- **Alumno:** Juan Agustín Acosta (juanagustinacost@gmail.com)
- **Email (trabajo):** aacosta@desarrollosnea.com
- **Profesor:** Rios, Leopoldo Jose (ljr@comunidad.unne.edu.ar)
