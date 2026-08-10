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
| **Sprint 4** | Orquestación en n8n + API Claude | 3 sem | ⏳ Pendiente |
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
  - Colección: `incidentes` (inicializada con `rag/scripts/init_collections.py`)

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
```

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

### (Opcional) Terraform + LocalStack

Solo para gestionar la infraestructura simulada AWS (EC2 mock). **No levanta Docker** por defecto:

```powershell
cd D:\DEV\argos\terraform\environments\localstack
terraform init
terraform apply   # solo gestiona infra LocalStack; Docker se maneja manualmente
```

Para usar el provisioner Terraform (modo legacy): `terraform apply -var provision_docker=true`

---

## Acceso a servicios

| Servicio | URL | Credenciales | Notas |
|----------|-----|--------------|-------|
| Grafana | http://localhost:3000 | admin / admin | Dashboards provisionados |
| Prometheus | http://localhost:9090 | — | Métricas, alertas activas |
| Alertmanager | http://localhost:9093 | — | Enrutamiento alertas |
| Loki | http://localhost:3100 | — | API logs, sin UI (usar Grafana Explore) |
| Qdrant | http://localhost:6333 | — | Vector DB; UI en `/dashboard` |
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

# n8n (Sprint 4+)
# N8N_ENCRYPTION_KEY=your-secure-key-here
# ANTHROPIC_API_KEY=sk-ant-...  # Será necesario en Sprint 4
# SMTP_FROM=alerts@argos.local
# ALERT_RECIPIENT_EMAIL=your-email@example.com
```

### `terraform/environments/localstack/terraform.tfvars` (opcional)

```hcl
instance_type = "t3.medium"
project_root  = "D:/DEV/argos"
```

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

4. **Alloy docker discovery deshabilitado**
   - Limitación: Podman/Windows no soporta socket mount
   - Fallback: file-based log collection activo (funciona correctamente)

---

## Roadmap futuro (Sprints 3-6)

- **Sprint 3:** ✅ Base vectorial Qdrant + RAG pipeline (`rag/`)
- **Sprint 4:** Orquestador n8n + integración API Claude + workflows análisis IA
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
- ⏸️ n8n, Ollama, Qdrant — (pendiente Sprint 3-4 con API Claude)
- ⏸️ k6 scenarios — (pendiente Sprint 6)

---

## Contacto

- **Alumno:** Juan Agustín Acosta (juanagustinacost@gmail.com)
- **Email (trabajo):** aacosta@desarrollosnea.com
- **Profesor:** Rios, Leopoldo Jose (ljr@comunidad.unne.edu.ar)
