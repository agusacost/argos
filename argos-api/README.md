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
│   │                             Notifications, LoadTests (32 endpoints, rutas por atributo)
│   ├── Authentication/         — handler Bearer JWT (401/403 con {"detail"})
│   ├── Binding/                — lectura y validación de bodies (IRequestDto → 422 / 400)
│   ├── Errors/                 — middleware de errores {"detail"}, mapeo excepción → status, 404/405
│   ├── Filters/                — token interno de n8n, parámetros inválidos → 422
│   ├── Hosting/                — inicialización al arrancar, modo `healthcheck`
│   ├── Modules/ApiModule.cs    — módulo Autofac de la capa
│   └── knowledge/              — runbooks que se siembran al arrancar
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

## Correr

```powershell
docker compose -p argos-platform up -d --build argos-api   # desde la raíz del repo
docker restart argos-dashboard                             # su nginx cachea la IP de rag-api
```

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
