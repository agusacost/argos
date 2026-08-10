import uuid
from dataclasses import asdict

from qdrant_client import QdrantClient
from qdrant_client.models import Distance, VectorParams, PointStruct

from . import config
from .models import Incident
from .embeddings import embed


def get_client() -> QdrantClient:
    return QdrantClient(url=config.QDRANT_URL)


def ensure_collection(client: QdrantClient) -> None:
    existing = [c.name for c in client.get_collections().collections]
    if config.COLLECTION_NAME not in existing:
        distance = Distance[config.DISTANCE.upper()]
        client.create_collection(
            collection_name=config.COLLECTION_NAME,
            vectors_config=VectorParams(size=config.VECTOR_SIZE, distance=distance),
        )
        print(f"Colección '{config.COLLECTION_NAME}' creada (size={config.VECTOR_SIZE}, distance={config.DISTANCE}).")
    else:
        print(f"Colección '{config.COLLECTION_NAME}' ya existe.")


def upsert_incident(client: QdrantClient, incident: Incident) -> str:
    vector = embed(incident.resumen, input_type="document")
    point_id = str(uuid.uuid5(uuid.NAMESPACE_DNS, f"{incident.tipo_alerta}::{incident.resumen}"))
    client.upsert(
        collection_name=config.COLLECTION_NAME,
        points=[PointStruct(id=point_id, vector=vector, payload=asdict(incident))],
    )
    return point_id


def query_incidents(client: QdrantClient, query: str, top_k: int = 3) -> list[dict]:
    vector = embed(query, input_type="query")
    results = client.search(
        collection_name=config.COLLECTION_NAME,
        query_vector=vector,
        limit=top_k,
        with_payload=True,
    )
    return [
        {
            "score": r.score,
            "clasificacion": "documentado" if r.score > config.SIMILARITY_THRESHOLD else "nuevo",
            **r.payload,
        }
        for r in results
    ]
