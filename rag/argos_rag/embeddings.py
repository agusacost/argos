from functools import lru_cache
from typing import Literal
from fastembed import TextEmbedding
from . import config


@lru_cache(maxsize=1)
def _get_model() -> TextEmbedding:
    return TextEmbedding(model_name=config.EMBEDDING_MODEL)


def embed(text: str, input_type: Literal["document", "query"] = "document") -> list[float]:
    # input_type kept for signature compatibility; MiniLM multilingual doesn't require prefixes.
    vector = next(iter(_get_model().embed([text])))
    return vector.tolist()
