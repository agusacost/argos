# Speech — Argos · Simulación de defensa del PFC

**Duración objetivo:** 8 min 30 s (~1 200 palabras a ritmo de exposición). 12 diapositivas: se quitaron el estado del arte, las conclusiones, el cierre, las referencias y los anexos (2026-10-28).
**Si hay que bajar a 8 min:** omitir los bloques marcados con ✂️.

---

### Diapositiva 1 — Carátula · *0:00 – 0:20*

Buenos días. Mi nombre es Juan Agustín Acosta y voy a presentar el avance de mi Proyecto Final de Carrera, **Argos**: una plataforma inteligente de AIOps para la detección, resolución asistida y documentación automática de incidentes en entornos web, con la orientación del profesor Leopoldo Ríos.

### Diapositiva 2 — Contexto y problema · *0:20 – 1:05*

Cualquier equipo que opera un servicio en producción conoce esta situación: suena una alerta, pero la alerta dice *qué* número se pasó, no *por qué* ni *cómo* se arregla. La respuesta depende de quién esté de guardia y de lo que recuerde. El resultado es fatiga de alertas, dependencia del conocimiento de unas pocas personas y un tiempo medio de resolución, el MTTR, alto. Y, sobre todo, errores que ya se resolvieron una vez se vuelven a investigar desde cero. Las herramientas de AIOps actuales diagnostican, pero casi ninguna **guarda** ese conocimiento para reutilizarlo: ese es el vacío que cubre Argos.

### Diapositiva 3 — Objetivos · *1:05 – 1:35*

El objetivo general es una plataforma que detecte incidentes por umbrales, genere con un LLM y RAG una síntesis con pasos de resolución, y documente automáticamente lo que es nuevo. Lo dividí en tres objetivos específicos: primero, la base de observabilidad como infraestructura como código; segundo, la capa inteligente; tercero, la validación con pruebas de carga k6.

### Diapositiva 4 — Métodos · *1:35 – 2:20*

Trabajé con un Scrum adaptado, en seis sprints, donde cada uno entrega la base del siguiente: infraestructura, observabilidad, base vectorial, orquestación con LLM, autodocumentación con el panel, y validación con despliegue en la nube.

Cuatro decisiones clave. Los **embeddings son locales**, con un modelo MiniLM multilingüe de 384 dimensiones en ONNX: no cuesta nada por consulta y los logs no salen del servidor. Toda la persistencia está en **Qdrant**. El LLM se llama a través de una **cadena de respaldo**, porque una caída del proveedor no puede dejar un incidente sin registrar. Y la API es una única API en **.NET 10** por capas, con un dashboard en React.

### Diapositiva 5 — Arquitectura · *2:20 – 3:10*

Este es el recorrido de una alerta, contado como manual.

Paso uno: el usuario pega contra **Nginx**, que hace de proxy de la API .NET y SQL Server. Paso dos, por dos caminos separados: **Grafana Alloy** lee el archivo de logs de Nginx y lo envía a **Loki**; y, por otro lado, **Prometheus** va a buscar las métricas que exponen los exporters de host, contenedores y Nginx. Alloy solo maneja logs: Prometheus no pasa por él. Paso tres: cuando una regla se cumple, **Alertmanager** agrupa y la manda por webhook a **n8n**. Paso cuatro: n8n le pide a la **Argos API** que busque en **Qdrant** y que llame al **LLM**. Paso cinco: el resultado sale por **email** al equipo y queda visible en el **dashboard**.

Todo esto se levanta con un solo `docker compose up` sobre la red `argos-net`, con tres stacks: monitoreo, proxy y aplicaciones.

### Diapositiva 6 — Herramientas · *3:10 – 3:25*

Acá resumo las herramientas: Terraform y Docker para infraestructura, el stack de Grafana para observabilidad, Qdrant y tres proveedores de LLM para la IA, n8n para orquestar, k6 y Playwright para pruebas, y Git con un product backlog por sprint para gestionar el trabajo.

### Diapositiva 7 — Resultado OE1: detección · *3:25 – 4:30*

Primer resultado: hay **dieciocho reglas** de detección funcionando.

Las cinco reglas de Loki sobre los logs de Nginx son las que disparan Argos. Error rate de respuestas 5xx por encima del **5 %** durante dos minutos es warning; por encima del **20 %** durante un minuto es crítico. Latencia p95 del upstream mayor a **2 segundos** por tres minutos. Una IP con más de **200 requests por minuto** durante dos minutos se marca como sospechosa. Y más de **un error por segundo** en el error.log es un pico de errores.

Las trece reglas de Prometheus cubren la infraestructura: CPU del host al **80 %** como warning y al **95 %** como crítico; memoria y disco al **85 %**, disco crítico al **95 %**; contenedores al 80 y 85 %; Nginx con más de 800 conexiones activas o 500 requests por segundo; y cualquier exporter caído durante un minuto es crítico.

✂️ El ruteo es deliberado: Alertmanager agrupa con 30 segundos de espera, manda a n8n las cinco alertas de Loki y todo lo crítico con repetición cada una hora, y los warnings de infraestructura van por email directo. Además, si una alerta está en crítico, silencia su versión warning para no duplicar.

### Diapositiva 8 — Resultado OE2: flujo inteligente · *4:30 – 5:35*

Segundo resultado: la capa inteligente. Cuando llega el webhook, n8n primero filtra: avanza si hay **tres o más alertas** o al menos **una crítica**; así no gastamos IA en ruido.

Después, la consulta RAG: el texto de la alerta se convierte en un vector y se busca en Qdrant. Si la similitud supera **0,68**, el incidente se considera **conocido** y se usa su solución. Si no, es **nuevo**. Ese umbral empezó en 0,63 y lo ajusté con la evaluación, como voy a mostrar.

En cualquier caso, se arma el prompt con ese contexto y se llama al LLM a través de la Argos API. El modelo principal es `gemini-flash-lite`; si falla, siguen Groq con Llama 3.3 y OpenRouter. Empecé con `gemini-flash-latest`, pero en la evaluación falló las 17 veces por saturación y lo saqué de la cadena. Con flash-lite, n8n procesa cada alerta en unos **seis segundos**; antes tardaba entre 23 y 68. El proveedor que falla queda **300 segundos** en cooldown al final de la cola. Esto se probó con un incidente real: un `CAdvisorDown` donde Gemini dio timeout y el respaldo respondió solo.

Por último, el email se envía a un **pool de destinatarios** configurable desde el panel, con la variable de entorno como respaldo.

### Diapositiva 9 — Resultado OE2: autodocumentación · *5:35 – 6:25*

Acá está el diferencial. Si el incidente es **nuevo**, el LLM redacta la documentación técnica y se guarda como *auto-documentado*: la próxima vez que ocurra, el RAG ya lo va a reconocer. Si es **conocido**, queda pendiente con la solución copiada del runbook, sin gastar LLM. Y cuando Alertmanager avisa que la alerta se **resolvió**, Argos cierra esa ocurrencia y calcula el **MTTR real**, como fin menos inicio.

✂️ Todo esto se ve en el dashboard de siete vistas: incidentes, base de conocimiento, métricas de Nginx, pruebas de carga on-demand, usuarios y una configuración que se aplica en caliente: proveedor de IA, umbral, autodocumentación y destinatarios.

### Diapositiva 10 — Resultado OE3: k6 · *6:25 – 7:20*

Tercer resultado: la validación con k6. Con **tráfico normal**, diez usuarios virtuales durante cinco minutos, el p95 fue de 3 milisegundos, con cero errores y **cero alertas**: no hay falsos positivos. En el **pico** de cien usuarios, la latencia se mantuvo plana y el rate limit de Nginx, de 30 requests por segundo por IP, rechazó el exceso. En el escenario de **ataque**, con paths inexistentes y ráfagas, se dispararon **cuatro alertas** reales que llegaron a n8n. No se autodocumentaron porque ya tenían runbook, que es lo que tiene que pasar. Y el escenario de **flood** confirma que Nginx corta el tráfico con un 429 antes de que llegue a la base de datos.

### Diapositiva 11 — Resultado OE3: calidad, aprendizaje y tiempos · *7:20 – 7:50*

Además de k6, inyecté fallas reales: contenedores caídos de la API, la base y los exporters, en diez corridas. Argos entregó la propuesta en **un minuto** desde que arranca la alerta, y en **ocho de diez** la propuesta traía el comando exacto que lo arreglaba. El aprendizaje también se ve en números: una alerta nueva que la primera vez tenía 0,40 de similitud, la segunda vez se reconoció con 0,99 contra el documento que generó la propia plataforma. Y la calidad de las respuestas, puntuada con una rúbrica, subió de 2,5 a 4,5 en accionabilidad cuando le pasé al modelo el documento aprendido y el contexto real del stack.

### Diapositiva 12 — Cumplimiento y cierre · *7:50 – 8:30*

En cumplimiento: el objetivo uno está en un **85 %**, solo falta migrar a la nube; el objetivo dos está **completo**; y el tres en un **95 %**: lo que no medí es la línea base manual, cuánto tarda una persona sin Argos, y lo declaro como limitación.

El aporte es que Argos **aprende**: cada incidente nuevo se convierte en conocimiento, y no depende de un único proveedor de IA. Lo que queda es el despliegue en GCP o AWS y la memoria final. Muchas gracias; quedo a disposición para sus preguntas.

---

## Preguntas probables del tribunal

| Pregunta | Respuesta corta |
|---|---|
| ¿Por qué 0,68? | Barrido de umbral sobre 7 fallas con resultado conocido: con 0,63 había un falso "conocida" (NginxDown copiaba el runbook de latencia); con 0,68 se aciertan 6/7 y no hay falsos "conocida", que es el error más caro. Se cambia en caliente desde Configuración. |
| ¿Cuánto reduce el MTTR? | No medí la línea base manual (limitación declarada). Lo medido: propuesta correcta en ~1,1 min y acción correcta en ~1,2 min desde el inicio de la alerta; el resto del MTTR lo impone la ventana de la regla (5 min en errores 5xx). |
| ¿Por qué ≥3 alertas o 1 critical? | Evita gastar LLM en warnings sueltos (ruido); un crítico solo ya justifica el análisis. |
| ¿Qué pasa si todos los LLM fallan? | El email sale igual con aviso, el incidente se guarda como "sin documentar" y queda una notificación `pipeline_error` en la campana. |
| ¿Por qué embeddings locales? | Costo cero, privacidad de logs y soporte multilingüe. |
| ¿Cómo se levanta? | Red `argos-net`, `.env`, `docker compose up -d --build`, import + activación del workflow n8n con credencial SMTP. |
| ¿Por qué no Next.js como decía la propuesta? | El panel es una SPA sin SSR ni SEO: React + Vite es más simple y Nginx la sirve estática. |
