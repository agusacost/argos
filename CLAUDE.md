# Argos: Instrucciones para Claude Code

**Proyecto:** Argos — Plataforma inteligente de AIOps  
**Alumno:** Juan Agustín Acosta (LU 57851)  
**Fecha:** 2026-08-03

---

## Contexto del proyecto

Argos es una plataforma AIOps construida sobre un stack de observabilidad open-source (Prometheus, Loki, Grafana, Alertmanager). Detecta incidentes por umbrales, genera síntesis del problema + pasos de resolución mediante IA (Claude API + RAG), y documenta automáticamente casos no registrados.

**Estado actual:** Sprints 1-3 completados
- ✅ Terraform + LocalStack + 3 stacks Docker (monitoring, proxy, apps)
- ✅ Alertas Prometheus (13) + Loki (5)
- ✅ Grafana + 3 dashboards
- ✅ Alloy log collection
- ✅ BD MembresiaDB inicializada automáticamente (db-init en docker/apps)
- ✅ Qdrant + pipeline RAG (`rag/` — embeddings Voyage AI, colección `incidentes`)
- ✅ Despliegue unificado vía `docker-compose.yml` raíz (desacoplado de Terraform)
- ⏳ Sprints 4-6: n8n + Claude API, auto-documentación, k6, cloud

---

## Estructura del proyecto

```
D:\DEV\argos/
├── docker-compose.yml                        — Compose raíz (include de 3 stacks)
├── docs/
│   └── Anexoll_Acosta_Juan_Agustin.docx.md  — Propuesta académica
├── terraform/
│   ├── environments/localstack/              — Infraestructura LocalStack (solo EC2 mock)
│   │   ├── main.tf, variables.tf, outputs.tf
│   │   ├── providers.tf
│   │   └── scripts/get_or_create_ami.ps1
│   └── scripts/                              — Base y provisioning (VMs cloud)
├── docker/
│   ├── monitoring/                           — Stack observabilidad + Qdrant
│   │   ├── docker-compose.yml
│   │   ├── prometheus/ (config + rules)
│   │   ├── loki/ (config + rules)
│   │   ├── grafana/ (dashboards provisioned)
│   │   ├── alertmanager/ (routing config)
│   │   └── .env.example
│   ├── proxy/                                — Nginx + Alloy
│   │   ├── docker-compose.yml
│   │   ├── nginx/ (config + Dockerfile)
│   │   └── alloy/ (log collector config)
│   └── apps/                                 — API .NET + SQL Server + db-init
│       ├── docker-compose.yml
│       ├── db/init-db.sql                    — Esquema + SP + seed (idempotente)
│       └── api/MembresiaAPI/ (.NET 10 Minimal)
├── rag/                                      — Sprint 3: base de conocimiento + RAG
│   ├── argos_rag/ (config, models, embeddings, qdrant_store)
│   ├── scripts/ (init_collections, seed, query_demo)
│   ├── knowledge/ (5 docs de incidentes conocidos)
│   ├── requirements.txt
│   └── .env.example
├── k6/                                       — Tests de carga (Sprint 6)
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

**Archivo clave:** `terraform/environments/localstack/main.tf`

Cambios típicos:
- Modificar `instance_type` en `variables.tf` (t3.medium/t3.large)
- Cambiar proyectos Docker que se levantan en `main.tf` (orden importa: monitoring → apps → proxy)
- Para desplegar en GCP (Sprint 6): copiar scaffold de `environments/gcp/` y llenar recursos reales

**Comando:**
```powershell
terraform plan      # Ver cambios sin aplicar
terraform apply     # Aplicar
terraform destroy   # Destruir
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
- Receivers: email, webhook (n8n en Sprint 4)
- **Nota:** webhook-n8n removido en migración, se reintroduce en Sprint 4

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
```

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

### Sprint 4: Orquestación n8n + Claude API

- Reintroducir `n8n` en `docker/monitoring/docker-compose.yml`
- Recrear workflows: alertmanager-analysis, scheduled-report
- **Cambio clave:** reemplazar Ollama por API de Claude (ANTHROPIC_API_KEY en .env)
- Reintroducir ruta webhook-n8n en alertmanager.yml

### Sprint 5: Auto-documentación

- Añadir servicio de persistencia (PostgreSQL con pgvector, o Qdrant endpoint + API)
- Implementar endpoint POST `/api/incidents/{id}/documentation` para auto-generar docs
- Integrar en workflow n8n: si incident es nuevo → generar doc → persistir

### Sprint 6: Validación + Cloud

- Crear stack k6 con 3 escenarios (normal, spike, ataque)
- Desplegar en AWS/GCP adaptando provider de Terraform
- Medir MTTR, latencia, error rate

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
