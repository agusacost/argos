# Evaluación del OE3 — Validación de la plataforma

**Objetivo (propuesta, OE3):** validar el comportamiento de la plataforma mediante escenarios simulados con k6,
evaluando **la detección**, **la calidad de la propuesta generada por el LLM** y **la correcta persistencia de la
documentación**, incluyendo una **estimación cuantitativa de la reducción del MTTR frente a un proceso manual de
referencia**.

Herramientas y protocolo: [`eval/README.md`](../eval/README.md). Estado: ✅ **cerrado (2026-10-28)**, con una limitación declarada: la línea base manual del MTTR no se midió (§5, §6).

| Eje | Estado | Sección |
|---|---|---|
| Detección | ✅ | [1](#1-detección) |
| Calidad de la propuesta del LLM | ✅ (puntuación asistida, a revisar por el autor) | [2](#2-calidad-de-la-propuesta-del-llm) |
| Clasificación del RAG (umbral) | ✅ | [3](#3-clasificación-del-rag-y-umbral) |
| Persistencia y aprendizaje | ✅ | [4](#4-persistencia-y-aprendizaje) |
| MTTR vs. proceso manual | ⚠️ condición Argos medida (10 corridas automáticas); línea base manual **no medida: limitación** | [5](#5-mttr-frente-a-un-proceso-manual) |

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
- El score de F6 depende de la IP y del valor de la descripción, que cambian en cada disparo. Una mejora posible es
  sumar `alertname` igual como señal (match exacto de tipo) además de la similitud.
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
  diagnóstico, no como arreglo.
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

## 6. Limitaciones

- **Sin línea base manual de MTTR:** no se midió cuánto tarda un operador humano sin Argos. Se reporta el tiempo
  hasta la propuesta correcta y hasta la acción correcta, no un porcentaje de reducción. El experimento con
  operador humano se canceló después de la 1ª corrida; sus herramientas quedan para trabajo futuro.
- **Operador simulado:** el MTTR "con Argos" corresponde a un operador que aplica el arreglo en cuanto llega la
  propuesta. Un humano agregaría el tiempo de leerla y ejecutarla.
- **Rúbrica de calidad puntuada por un LLM (Claude), con revisión del autor**, no por evaluadores humanos
  independientes.

- **N chico** (10 corridas automáticas, 7 escenarios de calidad) y fallas simples, de causa única y con arreglo de
  un comando.
- **Entorno local** (Docker Desktop); los tiempos de detección dependen de los `for:` de las reglas y del
  `group_wait`/`group_interval` de Alertmanager, iguales en las dos condiciones.
- **k6 desde una sola IP:** simula un DoS, no un ataque distribuido.
