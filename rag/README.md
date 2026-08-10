# Argos RAG Toolkit — Sprint 3

Pipeline de recuperación aumentada de información (RAG) sobre Qdrant para la base de conocimiento
de incidentes de Argos.

## Prerequisitos

- Python 3.11+
- Qdrant corriendo (incluido en el stack de monitoring)
- Conexión a internet solo la primera vez (para descargar el modelo de embeddings ~90MB)

## Setup

```powershell
cd D:\DEV\argos\rag

python -m venv .venv
.\.venv\Scripts\Activate.ps1

pip install -r requirements.txt
```

El `.env` ya viene configurado con defaults funcionales (embeddings locales, sin API key).

## Secuencia de ejecución

### 1. Levantar Qdrant

```powershell
# Desde la raíz del proyecto
docker compose -p argos-monitoring -f docker/monitoring/docker-compose.yml up -d qdrant
```

Verificar: `curl http://localhost:6333/readyz` (responde OK)  
Dashboard visual: `http://localhost:6333/dashboard`

### 2. Inicializar la colección en Qdrant

```powershell
python -m scripts.init_collections
```

Hace polling hasta que Qdrant responde, luego crea la colección `incidentes` si no existe
(384 dims, distancia Cosine).

### 3. Cargar el conocimiento inicial

```powershell
python -m scripts.seed
```

Lee los 5 documentos de `knowledge/`, genera embeddings vía FastEmbed (modelo local) y los
inserta en Qdrant (upsert idempotente — se puede correr múltiples veces sin duplicar).

### 4. Validar el pipeline

```powershell
# Caso documentado (score alto esperado)
python -m scripts.query_demo "nginx devuelve 502 al 30% de las requests"

# Caso nuevo (score bajo esperado)
python -m scripts.query_demo "el servidor de NTP tiene drift de 5 segundos"
```

Si los scores del caso "documentado" no superan el umbral 0.75, ajustar `SIMILARITY_THRESHOLD`
en `.env` a un valor más bajo (típico para MiniLM multilingual: ~0.5–0.65).

## Estructura

```
rag/
├── argos_rag/
│   ├── config.py         # Variables de entorno
│   ├── models.py         # Dataclass Incident
│   ├── embeddings.py     # embed() via FastEmbed (local, sin API key)
│   └── qdrant_store.py   # get_client, ensure_collection, upsert_incident, query_incidents
├── scripts/
│   ├── init_collections.py
│   ├── seed.py
│   └── query_demo.py
├── knowledge/            # 5 docs de incidentes conocidos (YAML frontmatter + markdown)
├── requirements.txt
├── .env                  # config activa (no commitear si hay secretos)
└── .env.example          # plantilla
```

## Embeddings locales (FastEmbed)

El modelo `sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2` corre completamente
offline después de la primera descarga. Soporta español nativo, produce vectores de 384 dimensiones.
No requiere API key ni servicio externo.

La primera vez que se llama a `embed()` (durante `seed.py`), FastEmbed descarga el modelo
a `~/.cache/fastembed/` (~90MB). Las ejecuciones siguientes son instantáneas.

## Uso desde Sprint 4 (n8n)

En el contexto de n8n, las funciones clave son:

```python
from argos_rag.qdrant_store import get_client, query_incidents
from argos_rag import config

client = get_client()
results = query_incidents(client, alert_summary, top_k=3)

if results and results[0]["score"] > config.SIMILARITY_THRESHOLD:
    # Incidente documentado → usar pasos del contexto RAG
    context = results
else:
    # Incidente nuevo → activar auto-documentación (Sprint 5)
    context = []
```

La URL de Qdrant dentro de `argos-net` es `http://qdrant:6333`
(configurar `QDRANT_URL=http://qdrant:6333` en el entorno de n8n).
