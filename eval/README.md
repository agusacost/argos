# Evaluación del OE3

Herramientas para cerrar el **Objetivo Específico 3** de la propuesta: validar la detección, **la calidad de la
propuesta del LLM**, la **persistencia de la documentación** y estimar la **reducción del MTTR frente a un
proceso manual de referencia**. Los resultados se vuelcan en [`docs/EVALUACION_OE3.md`](../docs/EVALUACION_OE3.md).

| Archivo | Para qué |
|---|---|
| `scenarios.json` | Catálogo de fallas F1–F7 con su causa raíz de referencia, lo que el RAG debería decir y los `labels` por escenario (los que la regla real agrega: `evidence`, `instance`, `job`) |
| `faults.ps1` | Inyecta/restaura una falla (o una al azar, sin mostrarla: **modo ciego**) |
| `timer.ps1` | Cronómetro objetivo: registra startsAt/fin de cada alerta desde Alertmanager → `results/alerts.csv` |
| `mark.ps1` | Hitos manuales de una corrida (start/diag/fix) → `results/runs.csv` |
| `summarize-mttr.mjs` | Cruza todo y calcula MTTR, diagnóstico y tiempo hasta la propuesta por condición |
| `run-llm-eval.mjs` | RAG + LLM por proveedor con los prompts reales del workflow → `results/llm-*.md/.csv` para puntuar |
| `run-auto.mjs` | Corridas automáticas por falla (inyecta, espera la alerta, lee el análisis de n8n, aplica el arreglo, mide) → `results/auto-*.md/.csv` |
| `n8n-analysis.cjs` | Lee de la SQLite de n8n el análisis, el LLM, `evidence_lines` y `match_reason` de cada ejecución (lo usa `run-auto.mjs`) |

`results/` no se versiona.

## 1. Calidad del RAG y del LLM

```powershell
node eval/run-llm-eval.mjs --rag-only          # matriz conocida/nueva + barrido de umbral (sin gastar LLM)
node eval/run-llm-eval.mjs                     # + análisis (y doc si es nueva) con cada proveedor
node eval/run-llm-eval.mjs --scenario F3 --providers groq,openrouter
node eval/run-llm-eval.mjs --no-evidence       # sin evidencia de logs (comparar con el prompt anterior)
node eval/run-llm-eval.mjs --evidence-at <ISO> # ventana de evidencia anclada en ese startsAt (default: ahora)
```

El script ejecuta el código real de los nodos `Prepare`, `Build Prompt` y `Build Doc Prompt` del workflow y llama
a `POST /internal/llm/generate` con `provider`/`model` (un solo proveedor, sin fallback ni aviso en la campana).
Los proveedores sin API key se saltean. El RAG se consulta con el `alertname` (señal por nombre, piso 0,30) y la tabla
muestra el `match_reason`; la evidencia (`POST /internal/evidence`) solo tiene contenido con la falla inyectada y
tráfico en curso (p. ej. `faults.ps1 -Scenario F2 -Action inject`); si no, sale vacía.
El script manda los `labels` de cada escenario a `/internal/evidence` (como el nodo `Get Evidence`): con `evidence: nginx` la API
infiere el contenedor desde Nginx y con `evidence: target` desde el `instance`; un escenario sin `labels` no trae evidencia.

**Cuidado con `run-auto.mjs`:** descarta las alertas que empezaron antes de la inyección, así que las pruebas manuales
que provocan 5xx (p. ej. detener la API a mano) contaminan la corrida.

**Puntuar** el `.csv` (abre en Excel, separador `;`) leyendo el `.md`:

| Criterio | 1 | 3 | 5 |
|---|---|---|---|
| `diagnostico_1a5` | causa equivocada | causa genérica ("revisar el servicio") | identifica la causa de referencia |
| `accionabilidad_1a5` | sin pasos ejecutables | pasos genéricos | comandos válidos para **este** stack (nombres reales de contenedores) |
| `completitud_1a5` | solo describe | diagnóstico **o** arreglo | diagnóstico + arreglo + verificación |
| `alucinaciones_n` | cantidad de afirmaciones falsas (servicios/archivos/comandos inexistentes) | | |
| `claridad_1a5` | confuso o cortado | entendible | conciso y ordenado |

También puntuar los documentos auto-generados reales (Base de Conocimiento → filtro auto-documentado).

## 2. Persistencia y aprendizaje

1. `./eval/faults.ps1 -Scenario F4 -Action inject` (CAdvisorDown: hoy **no** está en la KB con el texto largo).
2. Esperar el email; verificar en el dashboard: incidente `auto_documentado`, visible en la Base de Conocimiento.
3. `./eval/faults.ps1 -Scenario F4 -Action restore`, esperar la resolución (campana "Resuelto", MTTR).
4. Repetir 1–3. Lo esperado: **incidente nuevo** (otro `detected_at`) en estado `pendiente` con el documento copiado,
   **sin** "Doc LLM Call" en la ejecución de n8n.

> Si el `CAdvisorDown` viejo (resumen corto) aparece como match, no importa: no supera el umbral.

## 3. Experimento de MTTR (operador único)

**Condiciones**
- **Argos:** workflow activo; se usa el email/dashboard de Argos.
- **Manual (referencia):** workflow apagado; solo Alertmanager (`:9093`), Grafana (`:3000`), `docker ps`/logs.

```powershell
# apagar / prender el workflow de n8n
docker exec argos-n8n n8n update:workflow --id=argos-alert-analysis --active=false; docker restart argos-n8n
docker exec argos-n8n n8n update:workflow --id=argos-alert-analysis --active=true;  docker restart argos-n8n
```

**Sesgo de aprendizaje.** Como el operador conoce el catálogo, las fallas se inyectan en **modo ciego** desde un
pool: no sabés cuál es y tenés que diagnosticarla. Pool recomendado `F1,F2,F3,F4,F7` (las de CPU/ataque delatan su
causa). Se alterna la condición corrida a corrida empezando por **Argos**, y se hacen **al menos 6 por condición**.

**Una corrida** (terminal 1: `./eval/timer.ps1` corriendo todo el tiempo):

```powershell
./eval/faults.ps1 -Action inject -Run R01 -Pool F1,F2,F3,F4,F7
./eval/mark.ps1 -Run R01 -Condition argos -Event start
#   ...esperar la alerta, investigar...
./eval/mark.ps1 -Run R01 -Condition argos -Event diag -Note "la causa que creés que es"
#   ...aplicar VOS el arreglo (p.ej. docker start X)...
./eval/mark.ps1 -Run R01 -Condition argos -Event fix -Note "comando aplicado"
#   ...esperar que timer.ps1 muestre RESOLVED; si el arreglo fue incorrecto, seguir investigando
./eval/faults.ps1 -Action restore -Run R01     # limpieza (k6 y lo que haya quedado)
```

Entre corridas: esperar ~5 min sin alertas activas (el `group_interval` de Alertmanager y el `for:` de las reglas).

**Resumen**

```powershell
node eval/summarize-mttr.mjs                   # → eval/results/mttr-summary.md
```

Desglose por corrida: **detección** (falla → alerta activa), **propuesta** (startsAt → incidente con análisis en
Argos), **diagnóstico** (alerta → causa identificada), **reparación** (causa → arreglo) y **MTTR**
(startsAt → resolución según Alertmanager). La reducción se calcula sobre medianas.
