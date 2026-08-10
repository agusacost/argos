import os
from dotenv import load_dotenv

load_dotenv()

QDRANT_URL         = os.getenv("QDRANT_URL", "http://localhost:6333")
COLLECTION_NAME    = os.getenv("COLLECTION_NAME", "incidentes")
EMBEDDING_MODEL    = os.getenv("EMBEDDING_MODEL", "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2")
VECTOR_SIZE        = int(os.getenv("VECTOR_SIZE", "384"))
DISTANCE           = os.getenv("DISTANCE", "Cosine")
SIMILARITY_THRESHOLD = float(os.getenv("SIMILARITY_THRESHOLD", "0.75"))
