# Argos API (.NET 10)

Backend de Argos: RAG de incidentes, auth, configuración, observabilidad, auto-documentación con LLM y
pruebas de carga k6. Es la única API de la plataforma: la consumen el dashboard (nginx `/api` →
`rag-api:8000`) y el workflow de n8n (`/query`, `/upsert`, `/internal/config`, `/notifications/recipients`).

Solución `Argos.slnx` (abrir en Visual Studio / Rider / VS Code). Cada capa depende solo de las de abajo:

```
argos-api/
├── Argos.slnx                  — solución
├── Directory.Build.props       — net10.0, nullable, warnings como errores (común a todos los proyectos)
├── Argos.Api/                  — Controllers: capa HTTP (ASP.NET Core MVC)
│   ├── Controllers/            — Health, Rag, Internal, Auth, Users, Settings, Incidents, Observability,
│   │                             Notifications, LoadTests (35 endpoints, rutas por atributo)
│   ├── Authentication/         — handler Bearer JWT (401/403 con {"detail"})
│   ├── Binding/                — lectura y validación de bodies (IRequestDto → 422 / 400)
│   ├── Errors/                 — middleware de errores {"detail"}, mapeo excepción → status, 404/405
│   ├── Filters/                — token interno de n8n, parámetros inválidos → 422
│   ├── Hosting/                — inicialización al arrancar, modo `healthcheck`
│   └── Modules/ApiModule.cs    — módulo Autofac de la capa
├── Argos.Business/             — Business: reglas de negocio (interfaz + implementación por servicio)
│   ├── Services/               — Auth, Users, Settings, Llm, Rag, Incidents, Audit, Notifications,
│   │                             Observability, LoadTests
│   ├── Security/               — bcrypt, JWT
│   ├── Seeding/                — admin inicial, runbooks, incidentes demo
│   └── Modules/BusinessModule.cs
├── Argos.Repository/           — Repository: acceso a datos y a sistemas externos
│   ├── Repositories/           — IRepository<T> + QdrantRepository<T> / MetadataRepository<T> (template
│   │                             method) + uno por colección (incidentes, usuarios, auditoría, …)
│   ├── Qdrant/                 — cliente REST, filtros, serialización de payloads
│   ├── Embeddings/             — ONNX + tokenizer XLM-R
│   ├── Observability/          — Loki, Prometheus, probes HTTP
│   ├── Llm/                    — ILlmProvider: Gemini y Anthropic
│   ├── LoadTests/              — contenedor k6 (Docker.DotNet) y archivos compartidos
│   ├── Knowledge/              — runbooks markdown con front-matter YAML
│   └── Modules/RepositoryModule.cs
├── Argos.Entities/             — Entities: entidades de dominio (Incident, User, AppSettings, …) y Dtos/
├── Argos.Common/               — transversal: configuración por entorno (interfaces por área),
│                                 excepciones, reloj, ids uuid5, formato JSON, CommonModule
├── Argos.Tests/                — pruebas unitarias xUnit de la lógica pura (dotnet test Argos.slnx)
└── Dockerfile                  — multi-stage; descarga el modelo ONNX con checksum
```

Dependencias: `Api → Business → Repository → Entities → Common`. Todo se registra con módulos de Autofac
(`CommonModule`, `RepositoryModule`, `BusinessModule`, `ApiModule`) y se consume por interfaz; no hay clases
anidadas (un tipo por archivo).

## Embeddings

`sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2` (384 dims) en ONNX cuantizado
(`qdrant/paraphrase-multilingual-MiniLM-L12-v2-onnx-Q`) + tokenizer SentencePiece de XLM-R
(`Microsoft.ML.Tokenizers`, ids con offset fairseq) + truncado a 128 tokens + mean pooling. El modelo se
descarga en el build de la imagen (Dockerfile, con checksum) y se carga en el primer uso.

## Datos en Qdrant

- Colección vectorial `incidentes` (Cosine) y colecciones de metadatos `argos_users`, `argos_settings`,
  `argos_audit`, `argos_notifications`, `argos_loadtests` (vector dummy `[1.0]`, los datos en el payload).
- Ids de incidentes determinísticos: UUID v5 (namespace DNS) de `"tipo_alerta::resumen"`; re-guardar el
  mismo incidente lo actualiza en vez de duplicarlo.
- Contraseñas bcrypt `$2b$` (costo 12); sesiones JWT HS256 (`sub/email/role/exp`, 12 h).
- Qdrant se usa por REST (6333) con JSON crudo para preservar los payloads.
- Runbooks: **la imagen no trae ninguno**. Son del despliegue: `docker/monitoring/knowledge/*.md` se monta en
  `/app/knowledge` (`KNOWLEDGE_DIR`); si la carpeta no existe, no se siembra nada. El front-matter va a `tipo_alerta/resumen/pasos` y el cuerpo Markdown a `documento`.
  Al arrancar, un runbook ya sembrado con `documento` vacío se completa (idempotente; no toca nada más).

## Capa inteligente

- **Clasificación conocida/nueva** (`POST /query`, `Rag/KnowledgeClassifier`): conocida si un documento con
  conocimiento supera el umbral (`match_reason=similarity`) o, si no, si hay un documento con solución del mismo
  `tipo_alerta` (campo opcional `alertname` del body) con similitud ≥ `RAG_NAME_MATCH_MIN_SCORE` (default 0.30;
  `match_reason=alert_name`). La misma regla la usa `POST /internal/incidents/occurrence` (`Rag/KnowledgeMatcher`).
- **Evidencia de logs** (`POST /internal/evidence`, `Evidence/*`): para la ventana de la alerta (startsAt − 10 min →
  ahora, máx. +5 min). La fuente la declara la regla con el label `evidence` (n8n manda los `labels` de la alerta):
  - `evidence: nginx`: distribución status/upstream_status y error.log de Nginx; del `upstream_addr` de las respuestas
    5xx y del error.log (`upstream: "http://…"`, `… could not be resolved`) sale el upstream que Nginx reporta
    (`NginxUpstreamExtractor`), Docker lo traduce a un contenedor (`DockerContainerDirectory`: IP, nombre, servicio de
    Compose o alias) y se agregan sus últimas líneas (`{job="docker", container="…"}`) y su estado.
  - `evidence: target`: el host del label `instance` (por ejemplo `node-exporter:9100`), traducido igual.
  - sin label: sin evidencia. Sin `labels` (botón "Generar documentación"): Nginx para las alertas `Nginx*`.

  **Argos no conoce los contenedores del servicio**: no hay nombres escritos en el código. La descripción del entorno
  que va en los prompts (cómo se opera, contenedores, puertos) es configuración: `environment_context` en
  `GET/PATCH /settings` (tope 3000 caracteres) y en `GET /internal/config` para n8n; si nunca se editó, se usa
  `ARGOS_ENVIRONMENT_CONTEXT`. El código solo agrega una instrucción genérica (`IncidentDocumentPrompt.EnvironmentGuidance`). Tope de 18 líneas de 200
  caracteres; un contenedor sin líneas aparece con su estado en una sola línea. Si Loki o Docker no responden, la
  evidencia queda vacía o solo con Nginx. `ALERT_EVIDENCE_ENABLED` (default true), `DOCKER_HOST`.
- **Privacidad** (`Privacy/IpPseudonymizationSession`): toda generación (`ILlmService.GenerateAsync/GenerateWithAsync`)
  reemplaza las IP (IPv4/IPv6) por `[IP-n]` antes de llamar al proveedor y las restaura en la respuesta
  (`pseudonymized_ips` en la respuesta de `/internal/llm/generate`). `LLM_PSEUDONYMIZE_IPS` (default true).

## Pruebas

`dotnet test Argos.slnx` corre `Argos.Tests` (xUnit + Moq, sin Qdrant/Loki/LLM): seudonimización de IP, regla de
clasificación, evidencia, filtros, validación proveedor/modelo, alta manual de runbooks y seed. El Dockerfile copia
los proyectos por nombre: el de pruebas no entra en la imagen.

## Correr

```powershell
docker compose -p argos-platform up -d --build argos-api   # desde la raíz del repo
```

No hace falta reiniciar `argos-dashboard` al recrear la API: su nginx resuelve `rag-api` por el DNS de Docker en cada request.

Para trabajar en el código: abrir `Argos.slnx` (`dotnet build Argos.slnx`).
Local (fuera de Docker) necesita el modelo en `EMBEDDING_MODEL_DIR` (`model_optimized.onnx` +
`sentencepiece.bpe.model`, ver URLs en el Dockerfile) y `QDRANT_URL`.

## Contrato de errores

| Caso | Respuesta |
|---|---|
| Error esperado | status correspondiente + `{"detail": "..."}` |
| Body ilegible (UTF-8 inválido) | 400 |
| Body o parámetros inválidos | 422 con `detail` en texto |
| Falla del LLM en `/incidents/{id}/document` | 502 (sin la URL del proveedor: Gemini lleva la key en el query string) |
| Qdrant caído | 503 `Qdrant no disponible: …` |
| Ruta inexistente / método no permitido | 404 / 405 |
| Error no controlado | 500 texto plano `Internal Server Error` |
