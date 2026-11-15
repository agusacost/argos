# Evaluación del OE3 — Validación de la plataforma

**Objetivo (propuesta, OE3):** validar el comportamiento de la plataforma mediante escenarios simulados con k6,
evaluando **la detección**, **la calidad de la propuesta generada por el LLM** y **la correcta persistencia de la
documentación**, incluyendo una **estimación cuantitativa de la reducción del MTTR frente a un proceso manual de
referencia**.

Herramientas y protocolo: [`eval/README.md`](../eval/README.md). Estado: ✅ **cerrado (2026-10-28)**, con una limitación declarada: la línea base manual del MTTR no se midió (§5, §7). **Re-evaluación parcial del 11-12/11/2026** tras las mejoras de cierre (señal por nombre, evidencia de logs): §6.

| Eje | Estado | Sección |
|---|---|---|
| Detección | ✅ | [1](#1-detección) |
| Calidad de la propuesta del LLM | ✅ (puntuación asistida, a revisar por el autor) | [2](#2-calidad-de-la-propuesta-del-llm) |
| Clasificación del RAG (umbral) | ✅ | [3](#3-clasificación-del-rag-y-umbral) |
| Persistencia y aprendizaje | ✅ | [4](#4-persistencia-y-aprendizaje) |
| MTTR vs. proceso manual | ⚠️ condición Argos medida (10 corridas automáticas); línea base manual **no medida: limitación** | [5](#5-mttr-frente-a-un-proceso-manual) |
| Re-evaluación de noviembre (nombre de alerta, evidencia de logs) | ✅ RAG 7/7; F2 con N = 1 | [6](#6-re-evaluación-de-noviembre-2026) |

## 1. Detección

Ver [`k6/RESULTS.md`](../k6/RESULTS.md): tráfico normal sin alertas (0 falsos positivos), el ataque dispara 4 alertas
Loki que llegan a n8n, y el flood se corta con 429 en Nginx. Además, cada falla del catálogo (`eval/scenarios.json`)
dispara la alerta esperada; el tiempo de detección por corrida está en la sección 5.

## 2. Calidad de la propuesta del LLM

**Método.** Para cada falla F1–F7 se generan el análisis (y la documentación si el RAG la clasifica como nueva) con
cada proveedor de la cadena, con los prompts reales del workflow (`node eval/run-llm-eval.mjs`). Un evaluador
(el autor) puntúa con la rúbrica de `eval/README.md`: diagnóstico, accionabilidad, completitud y claridad (1–5) y
cantidad de alucinaciones. También se puntúan los documentos auto-generados en corridas reales.

**Configuración.** Corrida 1 con la configuración original: principal `gemini-flash-latest`, umbral 0.63. A partir
de sus resultados se pasó `gemini-flash-lite-latest` a principal y el umbral a 0.68 (`PATCH /settings`), y se
repitió todo (corrida 2). La cadena vigente es flash-lite → groq (sin key, se saltea) → openrouter.

**Disponibilidad y latencia, corrida 1 (2026-10-27 22:43, 7 escenarios, 27 generaciones).** Groq no participó (sin API key).

| Proveedor / modelo | OK / intentos | Latencia mediana (OK) | Fallas |
|---|---|---|---|
| gemini / gemini-flash-latest (principal) | **0 / 9** | – | 5× HTTP 503 "saturado", 4× timeout 60 s |
| gemini / gemini-flash-lite-latest (1er respaldo) | **9 / 9** | ~1,4 s | – |
| openrouter / openrouter/free (último respaldo) | 9 / 9 HTTP OK | ~16 s | 1 respuesta inválida: "User Safety: safe" (F4) |

Hallazgos:
- El modelo **principal no respondió nunca** en la corrida: sin la cadena de respaldo, el pipeline no habría
  producido ninguna propuesta. Es evidencia directa del valor del fallback, y una razón para evaluar si conviene
  dejar `gemini-flash-lite-latest` como principal.
- `openrouter/free` es un **router**: a veces elige un modelo clasificador de seguridad (tipo Llama Guard) que no
  responde a la consigna. Argos registra `openrouter/free` como modelo y no el modelo real que respondió.
- Las salidas completas, para puntuar, están en `eval/results/llm-2026-10-27-22-43-41.md/.csv`.

**Corrida 2 (2026-10-27 23:00, 24 generaciones; con 0.68 F7 pasa a "nueva" y suma un documento).**

| Proveedor / modelo | OK / intentos | Latencia mediana (OK) | Fallas |
|---|---|---|---|
| gemini / gemini-flash-latest (ya fuera de la cadena, se mide para comparar) | **0 / 8** | – | 3× HTTP 503, 5× timeout 60 s |
| gemini / gemini-flash-lite-latest (**principal**) | **8 / 8** | ~1,5 s | – |
| openrouter / openrouter/free (respaldo) | 8 / 8 HTTP OK | ~19,5 s | 1 respuesta inválida: "User Safety: safe" (F2) |

Lo inválido de OpenRouter **se repite** (1 de cada 8 o 9). La causa es que el router `openrouter/free` a veces elige
`nvidia/nemotron-3.5-content-safety:free`, un clasificador de seguridad.

**Corrección (2026-10-27): se fijó el modelo de OpenRouter.** Se probaron candidatos gratuitos con F1 y F7:

| Modelo | Resultado |
|---|---|
| `nvidia/nemotron-3-ultra-550b-a55b:free` | ✅ 2/2 |
| `nvidia/nemotron-3.5-super-vl:free` | ✅ 2/2 (más lento) |
| `google/gemma-4-31b-it:free`, `google/gemma-4-26b-a4b-it:free` | ❌ HTTP 429 (cuota) |
| `thinkingmachines/inkling:free` | ❌ HTTP 403 |

Se eligió **`nvidia/nemotron-3-ultra-550b-a55b:free`** (`OPENROUTER_MODEL`). Al verificarlo con los 7 escenarios
respondió **8 / 8** generaciones válidas, **0** "User Safety", con latencia mediana de ~13 s
(`eval/results/llm-2026-10-27-23-17-34.md`).

**Efecto en el pipeline real** (duración de la ejecución de n8n desde el webhook hasta el email y el upsert):

| Principal | Ejecuciones con análisis | Duración |
|---|---|---|
| `gemini-flash-latest` (2026-10-26) | #62, #65, #67 (+ #61 falló) | 68 s, 23 s, 65 s (#61: 315 s y error) |
| `gemini-flash-lite-latest` (2026-10-27) | #75, #77, #79 | **6,0 s, 6,7 s, 5,1 s** |

El tiempo hasta la propuesta (+62–64 s después del `startsAt`) se lo lleva casi todo la entrega de Alertmanager
(`group_wait` 30 s más la evaluación de la regla); Argos procesa en unos 6 s.

Salidas de la corrida 2: `eval/results/llm-2026-10-27-23-00-27.md/.csv`.

**Rúbrica (2026-10-27, `eval/results/llm-2026-10-27-23-32-27-puntuado.csv`).** Son 16 generaciones: los 7
escenarios con los dos modelos de la cadena vigente.

> **Método de puntuación:** la puntuó **Claude (Opus 5.5) como evaluador asistido**, con la rúbrica de
> `eval/README.md` y el catálogo de causas, y la revisa el autor. Se aplicó un criterio explícito: el modelo solo ve
> la **alerta**, no la falla. Por eso, en F2 (SQL caído, misma alerta que F1), proponer revisar "API o BD" es un
> diagnóstico correcto. Los errores que vienen de un runbook (`-p argos-apps` en `nginx-5xx.md`) **no** cuentan
> como alucinación del modelo. Limitación: el evaluador también es un LLM.

| Modelo | N | Diagnóstico | Accionabilidad | Completitud | Claridad | Alucinaciones (total) | Latencia mediana |
|---|---|---|---|---|---|---|---|
| gemini-flash-lite-latest (principal) | 8 | 3,63 | 3,13 | 3,25 | **5,00** | **4** | **~1,4 s** |
| nemotron-3-ultra-550b (OpenRouter, respaldo) | 8 | 3,50 | 3,25 | **4,63** | 3,75 | 7 | ~14 s |

| Contexto RAG del escenario | Diagnóstico | Accionabilidad |
|---|---|---|
| Con runbook manual (F1, F2, F5, F6) | **3,90** | **3,60** |
| Solo con documentos auto-generados (F3, F4, F7) | 3,00 | 2,50 |

Hallazgos:
1. **Los dos modelos rinden parecido en diagnóstico y accionabilidad.** flash-lite es más claro, más rápido y alucina
   menos; nemotron es más completo (verificación y ramas de decisión), pero se extiende y a veces asume otro
   entorno: en F4 resolvió todo con Kubernetes y ningún comando aplicaba. Confirma la elección de flash-lite como
   principal.
2. **Lo que más influye en la calidad es el contexto del RAG, no el modelo.** Con runbook, los comandos usan los
   nombres reales del stack (`nginx-proxy`, `membresia-sqlserver`, `sqlcmd`). Sin runbook, las respuestas son
   genéricas (systemd, SSH, kubectl).
3. **Brecha en el aprendizaje:** el nodo `Build Prompt` le pasa al LLM solo los `pasos` de los incidentes
   parecidos. Los auto-documentados guardan la solución en `documento` y tienen `pasos` vacío. Resultado: la
   plataforma **reconoce** la alerta repetida (§4) pero el **análisis no aprovecha** el documento aprendido. Los
   modelos lo dicen textualmente: "los incidentes históricos no tienen pasos documentados". El documento sí se
   copia al incidente del dashboard. **Mejora propuesta:** incluir un extracto del documento en el contexto (o
   extraer los pasos al auto-documentar) y describir el stack en el system prompt (Docker Compose, nombres de
   contenedores).
4. **Error en un runbook manual:** `nginx-5xx.md` indica `docker compose -p argos-apps restart api`, pero el
   proyecto es `argos-platform`. Los dos modelos lo repiten.

**Corrección y re-evaluación (2026-10-27 23:51, `eval/results/llm-2026-10-27-23-51-13-puntuado.csv`).** Se aplicaron
los hallazgos 3 y 4:
- `Build Prompt` pasa, para cada match sin `pasos`, la sección "Pasos de resolución" del documento aprendido (hasta
  900 caracteres).
- `Build Prompt`, `Build Doc Prompt` e `IncidentDocumentPrompt` (el botón "Generar documentación") describen el
  stack real: Docker Compose, sin Kubernetes ni systemd, con los nombres de los contenedores.
- Se corrigió el runbook `nginx-5xx.md` y los 2 incidentes de Qdrant que tenían `-p argos-apps`.
- Se reimportó y reactivó el workflow.

Mismos 7 escenarios, mismos 2 modelos y la misma rúbrica y evaluador:

| Modelo | Diagnóstico | Accionabilidad | Completitud | Claridad | Alucinaciones |
|---|---|---|---|---|---|
| flash-lite: antes → **después** | 3,63 → **3,88** | 3,13 → **4,13** | 3,25 → **3,75** | 5,00 → 5,00 | 4 → **0** |
| nemotron: antes → **después** | 3,50 → **4,25** | 3,25 → **4,50** | 4,63 → **5,00** | 3,75 → 3,63 | 7 → 9 |

| Contexto RAG | Diagnóstico antes → después | Accionabilidad antes → después |
|---|---|---|
| Con runbook manual (F1, F2, F5, F6) | 3,90 → 4,00 | 3,60 → 4,20 |
| Solo documentos auto-generados (F3, F4, F7) | 3,00 → **4,17** | 2,50 → **4,50** |

Lectura:
- Mejoró sobre todo donde solo había conocimiento **aprendido**: ahora el análisis usa el documento generado. Los
  modelos lo citan ("la solución histórica asume systemd; traducida a Docker") y proponen el arreglo exacto
  (`docker start node-exporter`, `docker restart cadvisor`). El caso extremo es nemotron en F4, que pasó de 1 a 5
  en accionabilidad (antes respondía todo con Kubernetes).
- flash-lite quedó **sin alucinaciones** y con claridad máxima. nemotron es el más completo, pero sus respuestas
  largas siguen inventando detalles (métricas, puertos, nombres de red), así que conviene mantenerlo como respaldo.
- F7 (exporter caído) sigue siendo el más difícil: los dos modelos sospechan primero de Nginx, aunque ahora el
  procedimiento revisa el exporter y llega al arreglo. Los documentos auto-generados **antes** de esta corrección
  (NodeExporterDown, CAdvisorDown, NginxDown) se escribieron sin contexto del stack y hablan de systemd. Se pueden
  regenerar desde el dashboard ("Generar documentación") para que el conocimiento guardado también quede alineado.

> **Nota (15/11/2026):** la descripción del stack que se agregó a los prompts en esta corrección ya no está escrita en el
> código: desde el 15/11 vive en un campo de configuración (`environment_context`, editable en el panel) y el código solo
> conserva una instrucción genérica (§6.7). Las cifras de esta sección se midieron con el texto en el código.

## 3. Clasificación del RAG y umbral

**Método.** `node eval/run-llm-eval.mjs --rag-only`: para cada falla se compara `is_documented` con lo esperado
(`expected_kb`) y se barre el umbral entre 0.55 y 0.75.

**Corrida 1 (umbral 0.63).** Aciertos: **5 / 7**.

| Falla | Alerta | Top score | Top match | Clasificó | Esperado | |
|---|---|---|---|---|---|---|
| F1 | NginxCriticalErrorRate | 0.982 | NginxCriticalErrorRate | conocida | conocida | ✅ |
| F2 | NginxCriticalErrorRate | 0.979 | NginxCriticalErrorRate | conocida | conocida | ✅ |
| F3 | NodeExporterDown | 1.000 | NodeExporterDown | conocida | conocida | ✅ |
| F4 | CAdvisorDown | 0.460 | NodeExporterDown | nueva | nueva | ✅ |
| F5 | HostCriticalCPU | 0.789 | HostHighCPU | conocida | conocida | ✅ |
| F6 | NginxSuspiciousIPActivity | 0.607 | NginxSuspiciousIPActivity | nueva | conocida | ❌ falso "nueva" |
| F7 | NginxDown | 0.669 | NginxHighResponseTime | conocida | nueva | ❌ falso "conocida" |

| Umbral | Aciertos | Falsos "conocida" | Falsos "nueva" |
|---|---|---|---|
| 0.55 – 0.60 | 6/7 | 1 (F7) | 0 |
| **0.63 (actual)** | 5/7 | 1 (F7) | 1 (F6) |
| 0.68 – 0.75 | 6/7 | 0 | 1 (F6) |

Lectura:
- Ningún umbral fijo acierta las 7: F6 (0.607, alerta conocida con otro texto) y F7 (0.669, alerta nueva parecida a
  otra de Nginx) se cruzan. 0.63 queda justo en la zona donde se equivocan las dos.
- **Un falso "conocida" (F7) es peor que un falso "nueva" (F6):** el primero copia una solución equivocada y no
  documenta; el segundo solo genera un documento de más. Eso favorece un umbral **≥ 0.68**.
- El score de F6 depende de la IP y del valor de la descripción, que cambian en cada disparo. La mejora de sumar
  `alertname` igual como señal se implementó en noviembre (§6.1).
- N = 7: el barrido orienta, no es concluyente.

**Corrida 2 (umbral 0.68, después de la prueba de aprendizaje).** Aciertos: **6 / 7**, sin falsos "conocida".

| Falla | Top score | Top match | Clasificó | Esperado | |
|---|---|---|---|---|---|
| F1 / F2 / F3 / F5 | 0.982 / 0.979 / 1.000 / 0.789 | (propio) | conocida | conocida | ✅ |
| F4 CAdvisorDown | **1.000** | CAdvisorDown (auto-documentado en §4) | conocida | conocida* | ✅ |
| F6 NginxSuspiciousIPActivity | 0.607 | NginxSuspiciousIPActivity | nueva | conocida | ❌ falso "nueva" |
| F7 NginxDown | 0.669 | NginxHighResponseTime | **nueva** | nueva | ✅ |

\* F4 era "nueva" en la corrida 1. Pasó a "conocida" porque la plataforma la documentó en la prueba de aprendizaje,
y el catálogo se actualizó.

## 4. Persistencia y aprendizaje

**Método.** Una alerta nueva (F4 `CAdvisorDown`) se dispara dos veces. Esperado: 1ª ocurrencia
`auto_documentado` en la KB; 2ª ocurrencia, incidente propio `pendiente` con el documento copiado y sin llamada al
LLM para documentar. Antecedente: corregir el texto guardado (`summary. description`) llevó la misma alerta de score
0.398 contra su propio documento a superar el umbral.

**Resultados (2026-10-27, F4 `CAdvisorDown`)** ✅

| Ocurrencia | startsAt | Score RAG | Rama n8n | Estado | ¿Documento? | Propuesta lista | MTTR |
|---|---|---|---|---|---|---|---|
| 1ª | 22:38:05 | 0.398 (no supera 0.63) | nueva → auto-doc | `auto_documentado` (en la KB) | generado por el LLM | +63 s | 1,5 min |
| 2ª | 22:45:35 | **0.968** | conocida → `Register Occurrence` | `pendiente` (incidente propio) | copiado (1958 caracteres), sin llamar al LLM | +62 s | – |

La plataforma **aprendió**: la misma alerta que antes no tenía match pasó a reconocerse con 0.968 contra el
documento que generó ella misma, y la 2ª vez no se gastó LLM para documentar.

**Repetición con la configuración nueva (umbral 0.68, flash-lite principal; F7 `NginxDown`)** ✅

| Ocurrencia | startsAt | Score RAG | Rama n8n (ejecución) | Estado final | ¿Documento? | Propuesta lista | MTTR |
|---|---|---|---|---|---|---|---|
| 1ª | 22:53:46 | 0.669 contra *otra* alerta (< 0.68) | nueva → auto-doc (#77, 6,7 s) | `auto_documentado` | generado | +64 s | 1,5 min |
| 2ª | 23:01:16 | **0.987** contra su propio doc | conocida → `Register Occurrence` (#79, 5,1 s) | `pendiente` → `resuelto` | copiado (1829 caracteres) | +62 s | 1,5 min |

Con 0.63, esta misma falla se habría tomado como "conocida" y se le habría copiado el runbook de *latencia de
Nginx*, que no corresponde. Con 0.68 se documentó bien y la siguiente vez se reconoció.

## 5. MTTR frente a un proceso manual

**Cambio de método (2026-10-28).** El experimento con operador humano en modo ciego se canceló después de la 1ª
corrida (quedó archivado en `eval/results/intento-manual-2026-10-28/`). Lo reemplazaron **corridas automáticas**
(`eval/run-auto.mjs`). Por cada falla, el runner inyecta, espera la alerta, lee de n8n el análisis que salió por
email, verifica si contiene el arreglo exacto (`docker start|restart <contenedor correcto>`), aplica el arreglo
como lo haría un **operador que sigue la propuesta** y espera la resolución en Alertmanager. **No mide un
diagnóstico humano** (el runner sabe qué falla inyectó). Mide lo que aporta Argos: cuánto tarda en entregar una
propuesta y si esa propuesta es correcta.

**Resultados (2026-10-28, 10 corridas: F1, F2, F3, F4, F7 × 2; `eval/results/auto-2026-10-28-00-16-30.csv/.md`)**

| Corrida | Falla | Detección (inyección → alerta) | Propuesta (startsAt → email) | ¿Arreglo exacto? | startsAt → arreglo | Arreglo → resolución | MTTR |
|---|---|---|---|---|---|---|---|
| A01 / A06 | F1 API caída | 3,05 / 2,78 | 1,10 / 1,08 | ✅ / ✅ | 1,19 / 1,25 | 7,87 / 8,78 | 9,06 / 10,02 |
| A02 / A07 | F2 SQL caído | 2,98 / 2,97 | 1,10 / 1,07 | ⚠️ / ⚠️ | 1,23 / 1,17 | 8,78 / 8,86 | 10,02 / 10,03 |
| A03 / A08 | F3 node-exporter | 1,94 / 1,94 | 1,12 / 1,08 | ✅ / ✅ | 1,19 / 1,18 | 0,84 / 0,84 | 2,03 / 2,02 |
| A04 / A09 | F4 cAdvisor | 2,10 / 2,10 | 1,07 / 1,09 | ✅ / ✅ | 1,09 / 1,24 | 0,92 / 0,76 | 2,01 / 1,99 |
| A05 / A10 | F7 exporter Nginx | 1,69 / 1,69 | 1,08 / 1,10 | ✅ / ✅ | 1,19 / 1,19 | 0,84 / 0,84 | 2,03 / 2,03 |

(minutos)

Lectura:
- **Propuesta en ~1,1 min** desde el `startsAt`, en las 10 corridas (rango 1,07 – 1,12). Siempre respondió
  `gemini-flash-lite-latest`, sin necesidad de fallback. Casi todo ese tiempo es la entrega de Alertmanager
  (`group_wait` 30 s más la evaluación de la regla); n8n procesa en ~6 s.
- **Arreglo exacto en 8 / 10.** En F2 (SQL caído) la alerta es idéntica a la de F1: la propuesta incluye el chequeo
  `sqlcmd` contra `membresia-sqlserver`, que revela la caída, pero solo propone reiniciar la API. Es correcta como
  diagnóstico, no como arreglo. Con la evidencia de logs en el prompt esto cambió (§6.3).
- **MTTR siguiendo la propuesta:** ~2,0 min en las fallas de exporters; ~10 min en las de errores 5xx. En estas
  últimas, ~8,8 min corresponden a que la regla se apague después del arreglo: `rate(...[5m])` necesita que la
  ventana de 5 min se limpie. Es un piso que impone la regla, igual con o sin Argos.
- **Tiempo hasta la acción correcta (startsAt → arreglo) ≈ 1,2 min.** Es el indicador comparable con un proceso
  manual: un operador sin Argos tiene que llegar al mismo arreglo partiendo solo de la alerta.

**Línea base manual: no medida (decisión del autor, 2026-10-28) → limitación declarada.** Sin un operador humano
sin Argos no hay datos propios para calcular el **porcentaje de reducción** del MTTR. Lo que sí queda medido:
- **Tiempo hasta una propuesta con el arreglo exacto: ~1,1 min** desde el `startsAt` (8 de 10 corridas; en las otras 2
  la propuesta incluye el chequeo que revela la causa).
- **Tiempo hasta la acción correcta siguiendo la propuesta: ~1,2 min.**
- El piso que imponen las reglas: ~0,8 min en exporters y ~8,8 min en errores 5xx para que la alerta se apague
  después del arreglo. Es igual con o sin Argos.

Queda como trabajo futuro un estudio con operadores humanos (idealmente varios y que no conozcan el catálogo), en
condiciones con y sin Argos, usando las herramientas que ya están en `eval/` (`faults.ps1` en modo ciego,
`timer.ps1`, `mark.ps1`, `summarize-mttr.mjs`).

## 6. Re-evaluación de noviembre 2026

Después del cierre de octubre se agregaron tres mejoras y se re-verificó lo que cambiaban (11 y 12/11/2026, stack
local `argos-platform`): coincidencia por nombre de alerta en el RAG, evidencia de logs en el prompt con
seudonimización de IP, y un arreglo en Nginx que apareció durante las corridas. El 15/11 la evidencia se rediseñó para
que Argos no conozca los contenedores de la aplicación (§6.2) y se repitieron F1, F2 y F7 (§6.5). Ese mismo día la
descripción del entorno de los prompts pasó del código a configuración (§6.7) y se repitieron F1, F2 y F7 otra vez.

### 6.1 Señal por nombre de alerta en el RAG

`POST /query` acepta `alertname` y devuelve `match_reason` (`similarity`, `alert_name` o `none`). Primero rige el
criterio de siempre (similitud sobre el umbral 0,68); si no alcanza, la alerta cuenta como conocida cuando existe un
documento **con solución del mismo tipo** cuya similitud supera un piso (`RAG_NAME_MATCH_MIN_SCORE`). El nombre suma,
no filtra; se hace una segunda búsqueda filtrada por tipo porque ese documento puede no estar entre los tres más
parecidos. El registro de ocurrencias usa la misma regla.

`node eval/run-llm-eval.mjs --rag-only` (11/11/2026, `eval/results/llm-2026-11-11-22-07-35.md`): **7 de 7**, frente a
6 de 7 solo con similitud.

| Falla | Alerta | Mejor puntaje | Motivo | Clasificó | Esperado |
|---|---|---|---|---|---|
| F1 | NginxCriticalErrorRate | 0,995 | similitud | conocida | conocida |
| F2 | NginxCriticalErrorRate | 0,994 | similitud | conocida | conocida |
| F3 | NodeExporterDown | 1,000 | similitud | conocida | conocida |
| F4 | CAdvisorDown | 1,000 | similitud | conocida | conocida |
| F5 | HostCriticalCPU | 0,789 (contra HostHighCPU) | similitud | conocida | conocida |
| F6 | NginxSuspiciousIPActivity | 0,607 | **nombre de alerta** | conocida | conocida |
| F7 | NginxDown | 0,990 | similitud | conocida | conocida |

Lectura:
- Solo F6 necesita la señal por nombre (0,607 < 0,68). F4 y F7 ya son conocidas porque la plataforma las documentó en
  la evaluación de octubre (§4); por eso F7 ya no es el falso "conocida" de la corrida 1.
- **Casos negativos (a mano):** `HostDiskCritical` (mejor puntaje 0,523, contra otra alerta) y `ContainerHighMemory`
  (0,430) no tienen documento propio y siguen siendo "nuevas" (`none`). Sin `alertname`, el endpoint responde como antes.
- **Ajuste del piso, de 0,50 a 0,30.** En una corrida real, `NginxHighResponseTime` (que tiene runbook manual) puntuó
  0,355 contra ese runbook: el resumen escrito a mano se parece poco al texto real de la alerta. Quedó por debajo de
  0,50 y se autodocumentó de más. Se bajó el default a 0,30. Es una elección apoyada en dos casos (0,607 y 0,355), no
  una calibración. Quedó un documento autogenerado de `NginxHighResponseTime` (11/11, 23:00 UTC), creado antes del
  cambio; es legítimo.
- **Limitación:** las siete fallas del catálogo ya son "conocidas", así que el catálogo no ejercita el falso
  "conocida"; lo cubren solo los dos casos negativos manuales.

### 6.2 Evidencia de logs y seudonimización de IP

Hasta octubre el LLM veía solo el texto de la alerta y el contexto del RAG. Ahora el nodo `Get Evidence` (entre
`RAG Query` y `Build Prompt`; si falla, el flujo sigue) llama a `POST /internal/evidence` y agrega al prompt una muestra
acotada de Loki para la ventana de la alerta (10 min antes del inicio hasta 5 min después como máximo):
(1) distribución de `status` y `upstream_status` del access.log, (2) mensajes del error.log de Nginx agrupando
repetidos y (3) últimas líneas del contenedor que Nginx señala como upstream. Presupuesto fijo: 18 líneas de hasta 200 caracteres
(un contenedor sin líneas en la ventana ocupa una sola, con su estado). `Build Prompt`, `Build Doc Prompt` y el
botón "Generar documentación" le piden al modelo que use la evidencia para discriminar la causa y que no afirme lo que
no muestra. Variable: `ALERT_EVIDENCE_ENABLED` (default `true`).

**Diseño vigente (15/11/2026): todo se infiere desde Nginx.** La primera versión (11-12/11) tenía escritos en el código
de Argos los contenedores de la aplicación de ejemplo (`membresia-api`, `membresia-sqlserver`) y Alloy recolectaba los
logs de todos los contenedores `argos*`: la plataforma "sabía" de antemano qué había detrás del proxy. La API de
membresías es un servicio independiente, y la propuesta (Anexo II) pone al proxy como componente central y las alertas
de Loki sobre sus logs como disparador, así que el camino hacia los logs de la aplicación debe partir de lo que reporta
Nginx. Ahora:
- **La regla declara la fuente** con el label `evidence`: `nginx` en las 5 reglas de Loki y en las 3 de tráfico de
  Prometheus (`NginxHighActiveConnections`, `NginxHighRequestRate`, `NginxWaitingConnections`); `target` en `NginxDown`,
  `NodeExporterDown` y `CAdvisorDown` (un componente que dejó de responder: se usa el host del label `instance`, p. ej.
  `node-exporter:9100`); sin label (recursos del host y de contenedores) no hay evidencia, igual que antes.
- **Con `nginx`, el upstream sale de los logs de Nginx:** del campo `upstream_addr` del access.log en las respuestas 5xx
  y del error.log (`upstream: "http://IP:puerto/..."`, o `<nombre> could not be resolved` cuando el contenedor detenido
  desaparece del DNS de Docker, §6.4). Como mucho dos upstream, los más frecuentes.
- **`DockerContainerDirectory`** traduce ese host (IP, nombre, servicio de Compose o alias de red) al contenedor
  consultando Docker, e informa su estado ("Up 2 minutes", "Exited (0) 3 minutes ago"). Recién entonces se piden a Loki
  las últimas líneas de ese contenedor. Si Nginx no reporta ningún upstream no se mira ningún contenedor; si Docker o
  Loki no responden, la evidencia queda solo en Nginx o vacía.
- **La base de datos ya no se consulta:** Nginx solo conoce a la API. La causa en la base se ve únicamente si la API la
  registra en sus logs (§6.3, punto 4).
- **Alloy es opt-in:** solo recolecta los contenedores con el label de Docker `argos.logs=true` (hoy `membresia-api` y
  `membresia-sqlserver`, en `docker/apps/docker-compose.yml`); los de la plataforma quedan afuera. En Loki pasaron de 13
  flujos a 2.
- n8n (nodos `Prepare` y `Get Evidence`) envía los labels de la alerta. El botón "Generar documentación" del panel no los
  conoce (el incidente no los guarda): usa la evidencia de Nginx para las alertas cuyo nombre empieza con "Nginx" y
  ninguna para el resto (criterio de respaldo).
- Verificado: compila, 94 pruebas unitarias en verde y desplegado. Prueba manual: con `instance = nginx-exporter:9113` la
  API resolvió al contenedor `nginx-prometheus-exporter` (por el servicio de Compose) y mostró su estado; una alerta de
  CPU del host sin label no trajo evidencia.

**Privacidad.** En `LlmService`, por donde pasa todo lo que sale a un proveedor, cada IPv4/IPv6 se reemplaza por un
marcador estable `[IP-n]` antes de enviar, y en la respuesta se restauran las direcciones reales para el operador
(`LLM_PSEUDONYMIZE_IPS`, default `true`; la API registra "N IP seudonimizadas antes de llamar al proveedor").
- Es **seudonimización, no anonimización**: la correspondencia marcador-IP vive solo en memoria durante la llamada y
  nunca sale del servidor, pero las líneas pueden contener rutas solicitadas o agentes de usuario.
- Falso positivo aceptado: cuatro números de 0 a 255 separados por puntos (p. ej. una versión `1.2.3.4`) se tratan
  como IP; como la respuesta se restaura, el operador no lo ve.
- Cambia lo que se afirmaba antes ("al LLM externo solo llega el texto de la alerta y el contexto recuperado"): ahora
  sale además una muestra de hasta 18 líneas de log. Los embeddings siguen siendo locales.

### 6.3 Corridas de F1 y F2 con evidencia (`eval/run-auto.mjs`)

Se hicieron cuatro tandas; las válidas son las del 12/11 y la F1 del 11/11 22:07. Tiempos en minutos; cuando hubo
análisis respondió `gemini-flash-lite-latest`. **Todas se obtuvieron con la primera versión de la evidencia** (contenedores
`membresia-api` y `membresia-sqlserver` escritos en Argos); el diseño vigente se probó el 15/11 (§6.5).

| Tanda (UTC) | Falla | Detección | Propuesta | ¿Arreglo exacto? | Evidencia en el prompt | MTTR (hasta que se apaga la alerta) | Válida |
|---|---|---|---|---|---|---|---|
| 11/11 22:07 (`eval/results/auto-2026-11-11-22-07-56.md`) | F1 API detenida | 2,61 | 1,10 | Sí | 5 líneas | 10,05 | Sí |
| 11/11 22:07 | F2 SQL detenido | 2,96 | 1,08 | No | 2 líneas | 10,06 | **No**: Nginx apuntaba a la IP vieja de la API (§6.4) |
| 11/11 22:39 (`.../auto-2026-11-11-22-39-51.md`) | F1 | - | - | - | - | - | **No**: la alerta se registró pero el runner la descartó (empezó antes de la inyección) |
| 11/11 22:39 | F2 | 2,63 | 1,62 | No | 5 líneas | 8,06 | **No**: los dos proveedores de LLM fallaron por error de red |
| 12/11 01:43 (`.../auto-2026-11-12-01-43-30.md`) | F1 | 3,04 | 1,08 | Sí | 3 líneas | 7,04 | Sí |
| 12/11 01:57 | F2 | 2,97 | 1,09 | No | 2 líneas (solo distribución de códigos) | 8,06 | Sí |
| 12/11 02:08 (`.../auto-2026-11-12-02-08-49.md`) | F2, con la API de ejemplo registrando sus errores | 2,72 | 1,08 | **Sí** | 7 líneas | 8,03 | Sí |

Cronología:
1. **11/11 22:07.** F1 correcta; F2 incorrecta. La evidencia mostró 502 "connection refused" en F2: Nginx apuntaba a la
   IP anterior de la API (§6.4). La corrida de F2 no era válida.
2. **11/11 22:39**, ya con Nginx corregido. En F1, la alerta quedó registrada pero el runner la descartó: había empezado
   un minuto antes de la inyección, disparada por las pruebas manuales de cambio de IP (el runner solo toma alertas
   posteriores a la inyección). En F2, los dos proveedores fallaron por un error de red transitorio ("Resource
   temporarily unavailable"): el incidente quedó registrado y el correo salió con el aviso de análisis no disponible,
   como está diseñado (RNF-01).
3. **12/11 01:43**, limpia. F1 correcta. En F2 el modelo ya distinguió el caso ("el upstream está recibiendo las
   peticiones pero responde con código HTTP 500 (125 requests frente a 93 exitosas), descartando problemas de
   conectividad") pero siguió proponiendo reiniciar la API: la evidencia tenía solo la distribución de códigos, porque
   la API de ejemplo capturaba la excepción de SQL y respondía 500 **sin escribir nada en sus logs**.
4. Se corrigió la API de ejemplo (`docker/apps/api/MembresiaAPI`) para que registre el error además de devolverlo.
5. **12/11 02:08.** Con la `SqlException` visible en la evidencia ("A network-related or instance-specific error
   occurred while establishing a connection to SQL Server"), la síntesis dice que la API falla al conectar con la base
   (`membresia-sqlserver`) y la propuesta incluye verificar el contenedor de la base y
   `docker restart membresia-api membresia-sqlserver`.

Lectura:
- F1 y F2 disparan la misma alerta y ahora reciben propuestas distintas; la de F2 contiene el arreglo. En octubre F2
  fue "parcial" en las dos corridas.
- **Es una sola corrida correcta de F2 (N = 1)**, y la propuesta reinicia los dos contenedores en lugar de señalar solo
  la base. No se repitió la rúbrica de calidad con evaluador.
- La mejora dependió de **dos** cosas: llevar la evidencia al prompt **y** que la aplicación monitoreada registre sus
  errores. Una aplicación que calla sus errores deja al asistente (y a una persona) sin la pista.
- Tiempo hasta la propuesta: 1,08-1,09 min, igual que en octubre; buscar la evidencia en Loki no lo alargó de forma
  apreciable.
- **Episodio de proveedores (RNF-01).** El 11/11 a las 22:59 UTC los dos eslabones con clave (Gemini y OpenRouter)
  fallaron por error de red. El workflow no se cortó: el incidente quedó `pendiente` y el correo salió con "Análisis IA
  no disponible: ningún proveedor LLM respondió". Dos minutos después el principal volvió a responder.

### 6.4 Defecto de Nginx: guardaba la IP del upstream

- **Síntoma.** En la primera corrida de F2, Nginx devolvía 502 "connection refused" hacia la API de membresía con el
  contenedor en ejecución. El error.log mostraba como upstream la IP del cliente (el contenedor de k6).
- **Causa.** Con `upstream { server membresia-api:8080; }` Nginx resuelve el nombre una sola vez, al arrancar. F1
  detiene y vuelve a iniciar la API; mientras estaba detenida, k6 tomó su IP, la API volvió con otra y Nginx siguió
  enviando tráfico a la vieja hasta reiniciar el proxy. El arreglo que propone Argos para F1 (`docker start
  membresia-api`) podía no restablecer el servicio.
- **Corrección.** Los dos Nginx (proxy de la superficie HTTP y el del panel) resuelven en cada request con el DNS de
  Docker (`resolver 127.0.0.11 valid=10s` y una variable en `proxy_pass`).
- **Comprobación.** Con la API detenida se lanzaron ocho contenedores para ocuparle la IP; al iniciarla pasó de
  172.20.0.16 a 172.20.0.23 y Nginx respondió 200 sin reiniciarse. Detenida, responde 502 en 3-4 s y el error.log dice
  `membresia-api could not be resolved`.
- **Efecto en el panel.** El mismo defecto explicaba el paso manual "reiniciar `argos-dashboard` tras recrear la API".
  Ya no hace falta: se recreó la API y el panel siguió respondiendo.
- **Consecuencia para octubre.** El runner detiene k6 al aplicar el arreglo, así que la alerta se apaga cuando se vacía
  la ventana de la regla, vuelva o no el servicio. **Los MTTR de F1 y F2 miden cuándo se apaga la alerta, no cuándo
  vuelve a responder el servicio.** Los logs de octubre ya no están en Loki (retención de 7 días), así que no se puede
  saber si entonces la API volvió con la misma IP.

### 6.5 Corridas con el diseño nuevo de la evidencia (15/11/2026)

F1, F2 y F7 con `eval/run-auto.mjs` (`eval/results/auto-2026-11-15-18-56-24.md`, UTC), ya con la evidencia inferida desde
Nginx (§6.2) y sin que Argos conozca ningún contenedor de antemano. Tiempos en minutos; respondió siempre el modelo
principal (`gemini-flash-lite-latest`).

| Falla | Detección | Propuesta | ¿Arreglo exacto? | Evidencia en el prompt | MTTR (hasta que se apaga la alerta) |
|---|---|---|---|---|---|
| F1 API detenida | 2,20 | 1,11 | Sí | 4 líneas | 6,01 |
| F2 SQL detenido | 3,05 | 1,08 | Sí | 5 líneas | 4,02 |
| F7 exporter de Nginx detenido | 1,77 | 1,09 | Sí | 1 línea | 2,07 |

Qué vio el modelo en cada caso:
- **F1:** el error.log de Nginx decía que `membresia-api` "could not be resolved"; Argos tradujo ese nombre al contenedor
  y Docker informó "Exited (0)". La síntesis: el contenedor `membresia-api`, "el upstream configurado, se encuentra
  detenido (Exited (0))"; la propuesta incluyó `docker start membresia-api`.
- **F2:** Nginx registró que las respuestas 500 venían del upstream 172.20.0.16; Argos lo tradujo a `membresia-api` y
  sus logs mostraban la excepción de SQL Server. La síntesis: "Nginx logra comunicarse con el upstream (172.20.0.16,
  correspondiente a membresia-api), pero este responde con errores HTTP 500. La causa raíz identificada en los logs del
  contenedor membresia-api es un fallo de conectividad con la base de datos". La propuesta incluye el chequeo con
  `sqlcmd` y `docker restart membresia-api` / `docker restart membresia-sqlserver` (reinicia los dos; el runner lo cuenta
  como exacto porque contiene el del contenedor correcto).
- **F7:** el `instance` de la alerta llevó al contenedor `nginx-prometheus-exporter`, que Docker informó detenido. La
  propuesta incluyó reiniciarlo (junto con `nginx-proxy`, que no hacía falta).

Lectura:
- Con el diseño nuevo las tres propuestas contienen el arreglo. F2 reproduce el resultado de la corrida del 12/11
  02:08 (§6.3), que **se había obtenido con la versión anterior**, la que leía la base por nombre. Esa corrida queda
  como antecedente: muestra que la API debía registrar sus errores, pero su evidencia no es comparable con la actual.
- **Es una corrida por falla (N = 1)**; no se repitió la rúbrica de calidad con evaluador.
- Las propuestas siguen agregando pasos de más (reiniciar los dos contenedores en F2, reiniciar Nginx en F7).
- El MTTR mide cuándo se apaga la alerta (§6.4). Los de F1 y F2 fueron menores que los del 11 y 12/11 (6,0 y 4,0 min
  frente a 7-10 min); no se analizó la causa, así que **no debe leerse como mejora de Argos**.

### 6.6 Qué no se re-evaluó

Calidad con rúbrica de los modelos (solo se leyó la propuesta de F1, F2 y F7), MTTR contra línea base manual, F3-F6 con
evidencia, y la suite E2E del panel posterior a estos cambios (ver `docs/FIXES_DASHBOARD.md`). Cada corrida envió correos
reales al pool de destinatarios.

### 6.7 El contexto del entorno pasa del código a configuración (15/11/2026)

**Por qué.** Desde octubre (§2) los prompts del análisis y de la documentación (nodos `Build Prompt` y `Build Doc Prompt`
de n8n, e `IncidentDocumentPrompt` del botón "Generar documentación") llevaban escrita en el código una descripción del
entorno con los nombres de todos los contenedores, incluidos `membresia-api` y `membresia-sqlserver`: otra forma de que
Argos conociera de antemano el servicio monitoreado (ver §6.2). Borrarla sin reemplazo habría obligado a repetir la
evaluación de calidad, porque esa descripción fue lo que subió la accionabilidad de 2,5 a 4,5 en las alertas aprendidas
(sin ella los modelos proponían systemd o kubectl). El autor eligió pasarla a configuración, que además es lo que dice
la propuesta: conocimiento que escribe quien opera el servicio, editable desde el panel, y montar Argos sobre otro
servicio sin modificar código.

**Qué se hizo** (compila, 102 pruebas unitarias en verde, panel compila, desplegado):
- Campo `environment_context` en la configuración (documento `argos_settings`), editable en Configuración (solo
  Administrador), con tope de 3000 caracteres (422 con mensaje claro si se excede, verificado). n8n lo recibe en el nodo
  `Get Config` (`GET /internal/config`).
- Si nunca se editó, se usa la variable `ARGOS_ENVIRONMENT_CONTEXT` del despliegue; el `.env.example` trae como valor de
  ejemplo la misma descripción que antes estaba en el código. Vacío a propósito = prompt genérico.
- El código conserva solo una instrucción genérica, igual en n8n y en la API: usar los nombres que aparezcan en el
  contexto del entorno, en la evidencia o en los incidentes históricos, y no inventar nombres ni usar herramientas de otras
  plataformas que el contexto no mencione.
- `eval/run-llm-eval.mjs` toma el contexto de `/internal/config`, igual que n8n.
- Verificado sobre el stack: tras desplegar, `GET /internal/config` devolvía el contexto vacío; se cargó por la API de
  configuración y n8n lo recibe (591 caracteres).

**Corridas de F1, F2 y F7** con el contexto desde configuración (`eval/results/auto-2026-11-15-20-00-41.md`, UTC; modelo
principal `gemini-flash-lite-latest`; tiempos en minutos):

| Falla | Detección | Propuesta | ¿Arreglo exacto? | Evidencia en el prompt | MTTR (hasta que se apaga la alerta) |
|---|---|---|---|---|---|
| F1 API detenida | 2,87 | 1,47 | Sí | 4 líneas | 7,06 |
| F2 SQL detenido | 2,98 | 1,09 | No (parcial) | 5 líneas | 5,00 |
| F7 exporter de Nginx detenido | 1,77 | 1,11 | Sí | 1 línea | 2,07 |

Lectura:
- **F1:** la síntesis cita el error.log ("membresia-api could not be resolved") y el estado de Docker ("Exited (0) 2
  minutes ago") y propone `docker start membresia-api`.
- **F2:** el diagnóstico es correcto ("membresia-api experimenta fallas de conectividad con la base de datos
  (SqlException: el servidor SQL no fue encontrado o no es accesible)") y el paso 3 verifica `membresia-sqlserver` con
  `sqlcmd`, pero el comando de arreglo es `docker restart membresia-api`; reiniciar la base aparece solo como alternativa
  en el texto. El runner lo cuenta como no exacto: el mismo tipo de resultado "parcial" que F2 tuvo en octubre.
- **F7:** esta vez la propuesta reinicia solo el exporter (en la corrida de §6.5 reiniciaba también Nginx).
- Contra la corrida de una hora antes (§6.5), donde F2 sí tuvo el arreglo exacto, **la diferencia no se puede atribuir al
  cambio**: el texto del contexto es el mismo, solo cambió de dónde sale. Con N = 1 por falla, muestra la variabilidad del
  modelo con la misma entrada. Entre las dos corridas del 15/11: 5 de 6 propuestas con el arreglo exacto; en la sexta,
  diagnóstico correcto y arreglo parcial.
- Es una corrida por falla; no se repitió la rúbrica de calidad con evaluador, así que no se afirma que la calidad de §2
  se mantenga con el texto en configuración.

**Limitación.** Si quien opera el servicio deja el campo vacío o desactualizado, pueden volver las propuestas genéricas
(systemd, kubectl) que se vieron antes de octubre.
**Verificado después (15/11):** el campo quedó cubierto por tres pruebas E2E en navegador (guardar y persistir, tope de
3000 → 422, solo lectura para el Operador); ver `docs/FIXES_DASHBOARD.md`.

### 6.8 Tercera tanda, interrumpida (15/11/2026, 23:23 UTC)

Se lanzó `node eval/run-auto.mjs --scenarios F1,F2,F7 --rounds 3` para pasar de una corrida por falla a tres. **La tanda
se interrumpió en la segunda corrida** (el equipo se quedó sin memoria y el proceso fue detenido), así que solo dejó dos
datos (`eval/results/auto-2026-11-15-23-23-55.md` y `.csv`; la segunda corrida solo figura en el log de la consola):

| Corrida | Falla | Detección | Propuesta | ¿Arreglo exacto? | MTTR |
|---|---|---|---|---|---|
| A01 | F1 API detenida | 2,62 | 1,17 | Sí | 7,04 |
| A02 | F2 SQL detenido | ~1,9 | ~1,2 | No | sin medir (interrumpida) |

- **F1** vuelve a citar el error.log y el estado de Docker y propone `docker start membresia-api`.
- **F2** volvió a salir sin el arreglo exacto. De A02 solo quedó el veredicto del runner (tiempos aproximados, del log); el texto de la propuesta no se
  guardó.
- **Acumulado del 15/11 con el diseño actual:** F1 3 de 3 con arreglo exacto, F7 2 de 2 y **F2 1 de 3**. En total 6 de 8.
  F2 es el caso inestable: el modelo identifica la causa en la base, pero suele proponer reiniciar la API. Argos llega a
  la base solo a través de lo que la API registra (§6.2), y con tres corridas ya no parece azar de una sola.
- El servicio quedó sano después de la interrupción (el runner ya había vuelto a iniciar la base); no se repitió la tanda.
- Antes de esta tanda se quitó la contraseña `sa` de los runbooks y de 17 documentos de la base (ver
  `docs/PRODUCT_BACKLOG.md`); el paso de `sqlcmd` de las propuestas ahora muestra un marcador en su lugar.

## 7. Limitaciones

- **Sin línea base manual de MTTR:** no se midió cuánto tarda un operador humano sin Argos. Se reporta el tiempo
  hasta la propuesta correcta y hasta la acción correcta, no un porcentaje de reducción. El experimento con
  operador humano se canceló después de la 1ª corrida; sus herramientas quedan para trabajo futuro.
- **El MTTR de F1/F2 mide cuándo se apaga la alerta**, no cuándo vuelve a responder el servicio (§6.4): el runner
  detiene k6 al aplicar el arreglo, así que la alerta se apaga al vaciarse la ventana de la regla haya o no
  recuperado el servicio. Los logs de octubre ya no están en Loki (retención de 7 días) y no se puede comprobar si
  en esas corridas la API volvió con la misma IP.
- **Operador simulado:** el MTTR "con Argos" corresponde a un operador que aplica el arreglo en cuanto llega la
  propuesta. Un humano agregaría el tiempo de leerla y ejecutarla.
- **Rúbrica de calidad puntuada por un LLM (Claude), con revisión del autor**, no por evaluadores humanos
  independientes.

- **N chico** (10 corridas automáticas, 7 escenarios de calidad) y fallas simples, de causa única y con arreglo de
  un comando.
- **Entorno local** (Docker Desktop); los tiempos de detección dependen de los `for:` de las reglas y del
  `group_wait`/`group_interval` de Alertmanager, iguales en las dos condiciones.
- **k6 desde una sola IP:** simula un DoS, no un ataque distribuido.
- **Re-evaluación de noviembre (§6):** F2 con evidencia correcta es **una sola corrida (N = 1)**; no se repitió la
  rúbrica de calidad con evaluador; el catálogo F1-F7 no ejercita el falso "conocida" (las siete ya eran conocidas) y
  el piso de 0,30 se apoya en dos casos, no en una calibración.
- **Diseño nuevo de la evidencia (§6.5):** F1, F2 y F7 con una sola corrida cada una; las de 11-12/11 corresponden a la
  versión anterior y no son comparables. La inferencia del contenedor depende de que Nginx reporte un upstream (en
  `evidence: nginx`) y de que la regla tenga su label `evidence`; la causa en la base solo aparece si la API la registra.
  El botón "Generar documentación" usa un criterio de respaldo (no conoce los labels).
- **Tercera tanda interrumpida (§6.8):** de las nueve corridas previstas quedaron dos (F1 exacto, F2 no). Acumulado del
  15/11: F1 3/3, F7 2/2, F2 1/3.
- **Contexto del entorno en configuración (§6.7):** F1, F2 y F7 con una sola corrida cada una; F2 volvió a salir parcial
  (diagnóstico correcto, arreglo `docker restart membresia-api`) y no se atribuye al cambio. Las puntuaciones de §2 se
  midieron con el texto en el código y no se repitieron con el texto en configuración. Depende de que quien opera el
  servicio mantenga el campo; el campo nuevo no se vio en un navegador.
- **Evidencia de logs y privacidad:** al proveedor externo ahora sale una muestra de hasta 18 líneas de log (con las
  IP seudonimizadas). Es seudonimización, no anonimización: las líneas pueden traer rutas o agentes de usuario.
