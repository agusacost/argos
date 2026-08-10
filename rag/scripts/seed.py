"""Carga los documentos de knowledge/ en Qdrant. Idempotente (upsert por ID determinístico)."""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

import yaml
from argos_rag.models import Incident
from argos_rag.qdrant_store import get_client, ensure_collection, upsert_incident

KNOWLEDGE_DIR = Path(__file__).parent.parent / "knowledge"


def load_knowledge_file(path: Path) -> Incident:
    text = path.read_text(encoding="utf-8")
    # Parsear frontmatter YAML delimitado por ---
    if text.startswith("---"):
        _, fm, _ = text.split("---", 2)
        data = yaml.safe_load(fm)
    else:
        raise ValueError(f"{path}: archivo sin frontmatter YAML")
    return Incident(
        tipo_alerta=data["tipo_alerta"],
        resumen=data["resumen"],
        pasos=data.get("pasos", []),
        estado=data.get("estado", "documentado_previamente"),
        fuente=data.get("fuente", "manual"),
    )


if __name__ == "__main__":
    client = get_client()
    ensure_collection(client)

    docs = sorted(KNOWLEDGE_DIR.glob("*.md"))
    if not docs:
        print(f"No se encontraron archivos .md en {KNOWLEDGE_DIR}")
        sys.exit(1)

    for path in docs:
        incident = load_knowledge_file(path)
        point_id = upsert_incident(client, incident)
        print(f"[OK] {path.name} -> {incident.tipo_alerta} (id={point_id[:8]}...)")

    info = client.get_collection("incidentes")
    print(f"\nColección 'incidentes': {info.vectors_count} vectores totales.")
