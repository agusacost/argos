"""Consulta la base de conocimiento y muestra resultados con clasificación documentado/nuevo."""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from argos_rag import config
from argos_rag.qdrant_store import get_client, query_incidents


def main() -> None:
    if len(sys.argv) < 2:
        print("Uso: python -m scripts.query_demo \"<texto de consulta>\"")
        sys.exit(1)

    query = " ".join(sys.argv[1:])
    client = get_client()
    results = query_incidents(client, query, top_k=3)

    if not results:
        print("No se encontraron resultados.")
        return

    print(f"\nConsulta: \"{query}\"\nUmbral: {config.SIMILARITY_THRESHOLD}\n")
    print("-" * 60)
    for i, r in enumerate(results, 1):
        tag = "✅ DOCUMENTADO" if r["clasificacion"] == "documentado" else "🆕 NUEVO"
        print(f"{i}. [{tag}] score={r['score']:.4f}")
        print(f"   tipo_alerta : {r['tipo_alerta']}")
        print(f"   resumen     : {r['resumen'][:100]}...")
        if r.get("pasos"):
            print(f"   pasos       : {len(r['pasos'])} paso(s)")
        print()


if __name__ == "__main__":
    main()
