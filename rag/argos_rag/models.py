from dataclasses import dataclass, field
from datetime import datetime
from typing import Literal


@dataclass
class Incident:
    tipo_alerta: str
    resumen: str
    pasos: list[str]
    fecha: str = field(default_factory=lambda: datetime.utcnow().isoformat())
    estado: Literal["resuelto", "pendiente", "documentado_previamente"] = "documentado_previamente"
    fuente: Literal["manual", "auto-generado"] = "manual"
