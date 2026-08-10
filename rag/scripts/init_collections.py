"""Crea la colección 'incidentes' en Qdrant si no existe. Idempotente."""
import sys
import time
import urllib.request

sys.path.insert(0, str(__import__("pathlib").Path(__file__).parent.parent))

from argos_rag import config
from argos_rag.qdrant_store import get_client, ensure_collection


def _wait_for_qdrant(timeout: int = 60) -> None:
    url = f"{config.QDRANT_URL}/readyz"
    deadline = time.time() + timeout
    while time.time() < deadline:
        try:
            with urllib.request.urlopen(url, timeout=3) as r:
                if r.status == 200:
                    print(f"Qdrant listo en {config.QDRANT_URL}")
                    return
        except Exception:
            pass
        print("Esperando a Qdrant...", flush=True)
        time.sleep(3)
    raise TimeoutError(f"Qdrant no respondió en {timeout}s ({url})")


if __name__ == "__main__":
    _wait_for_qdrant()
    client = get_client()
    ensure_collection(client)
    info = client.get_collection(config.COLLECTION_NAME)
    print(f"Vectores en colección: {info.vectors_count}")
