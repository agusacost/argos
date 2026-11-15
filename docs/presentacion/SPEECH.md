# Speech — Argos · Simulación de defensa del PFC

**Duración objetivo:** 8 min (~1 100 palabras). 18 diapositivas con la estructura de la plantilla (Anexo III, IMRD):
introducción, métodos y herramientas en ~2 min 45 s; **resultados en ~3 min 30 s**; conclusiones y cierre en ~1 min 45 s.
La diapositiva 18 (referencias) no se lee. **Si hay que bajar a 7 min:** omitir los bloques marcados con ✂️.

---

### 1 — Carátula · *0:00 – 0:15*

Buenos días. Soy Juan Agustín Acosta y presento el avance de mi Proyecto Final de Carrera, **Argos**: una plataforma de AIOps para la detección, la resolución asistida y la documentación automática de incidentes en entornos web, con la orientación del profesor Leopoldo Ríos.

### 2 — Contexto y problema · *0:15 – 0:50*

Cuando suena una alerta en producción, dice *qué* número se pasó, pero no *por qué* ni *cómo* se arregla. La respuesta depende de quién esté de guardia y de lo que recuerde. Eso produce fatiga de alertas, dependencia de unas pocas personas y un MTTR alto, y hace que problemas ya resueltos se vuelvan a investigar desde cero. Argos agrega una capa inteligente sobre un stack de observabilidad abierto: detecta, explica, propone la solución y la documenta.

### 3 — Estado del arte · *0:50 – 1:20*

En la tabla comparo los enfoques. Los umbrales fijos solo detectan. Prometheus, Loki y Grafana suman contexto. Las soluciones de AIOps y los LLM aplicados a logs ya explican y proponen. Pero ninguna de ellas guarda lo aprendido para reutilizarlo. Ese es el nicho: documentar cada incidente nuevo para que la próxima vez la solución ya esté.

### 4 — Objetivos · *1:20 – 1:45*

El objetivo general es una plataforma que detecte por umbrales, genere con LLM y RAG una síntesis con pasos de resolución y documente lo nuevo. Tiene tres objetivos específicos: la base de observabilidad como código, la capa inteligente y la validación con k6.

### 5 — Métodos · *1:45 – 2:20*

Trabajé con Scrum adaptado en seis sprints, cada uno sobre la base del anterior. Cuatro decisiones clave: embeddings **locales**, sin costo por consulta y sin sacar los logs del servidor; toda la persistencia en **Qdrant**; el LLM detrás de una **cadena de respaldo**, para que una caída del proveedor no deje un incidente sin registrar; y una única API en **.NET 10** con un panel en **Next.js**.

### 6 — Arquitectura · *2:20 – 2:55*

El recorrido de una alerta: el tráfico entra por **Nginx**. Alloy envía sus logs a **Loki**, y **Prometheus** consulta las métricas de los exporters. Cuando una regla se cumple, **Alertmanager** la manda a **n8n**, que le pide a la **Argos API** buscar en Qdrant y consultar al LLM. El resultado sale por **email** y queda en el **panel**. Todo se levanta con un solo `docker compose`.

### 7 — Herramientas · *2:55 – 3:05*

Estas son las herramientas: todas abiertas o con plan gratuito, desde Terraform y Docker hasta k6 y Playwright para las pruebas.

### 8 — Resultado OE1: detección · *3:05 – 3:40*

Primer resultado: **18 reglas** activas. Las cinco de Loki miran los logs de Nginx: errores 5xx por encima del 5 % son warning y por encima del 20 %, crítico; también latencia p95 mayor a 2 segundos, una IP con más de 200 pedidos por minuto y picos en el log de errores. Las trece de Prometheus cubren CPU, memoria, disco, contenedores, Nginx y exporters caídos. ✂️ Alertmanager manda a n8n lo de Loki y todo lo crítico; los warning de infraestructura van por email directo.

### 9 — Resultado OE2: flujo inteligente · *3:40 – 4:25*

Segundo resultado. n8n filtra primero: avanza con tres alertas o una crítica. Después el RAG busca en Qdrant: con similitud mayor a **0,68**, la alerta es conocida y se reutiliza su solución. El LLM principal es `flash-lite`, con Groq y OpenRouter de respaldo; el que falla queda cinco minutos en pausa. Con este modelo, n8n procesa cada alerta en unos **seis segundos**; con el modelo anterior tardaba entre 23 y 68.

### 10 — Resultado OE2: autodocumentación · *4:25 – 5:00*

Este es el diferencial. Si la alerta es **nueva**, el LLM la documenta y el documento se suma a la base. Si es **conocida**, queda pendiente con la solución copiada, sin gastar LLM. Cuando Alertmanager avisa que se **resolvió**, Argos cierra la ocurrencia y calcula el MTTR real. ✂️ Todo se ve en un panel de siete vistas con configuración en caliente.

### 11 — Resultado OE3: k6 · *5:00 – 5:35*

Tercer resultado. Con tráfico **normal** no hubo ninguna alerta: cero falsos positivos. En el **pico**, el rate limit de Nginx rechazó el exceso. En el **ataque**, se dispararon cuatro alertas reales que llegaron a n8n y no se re-documentaron porque ya tenían runbook. El **flood** se corta en Nginx con un 429.

### 12 — Resultado OE3: tiempos, calidad y aprendizaje · *5:35 – 6:20*

Además inyecté fallas reales en diez corridas. La propuesta llegó en **1,1 minutos** en todas, y en **ocho de diez** traía el comando exacto. El MTTR fue de 2 minutos en exporters y de unos 10 en errores 5xx, porque la regla necesita que se limpie su ventana de 5 minutos. Argos también aprende: una alerta nueva que la primera vez puntuaba 0,40 se reconoció la segunda con 0,97. Y la accionabilidad subió de 2,5 a 4,5 al darle al modelo lo aprendido y el stack real.

### 13 — Cumplimiento · *6:20 – 6:40*

El OE1 está al **90 %**: la infraestructura es un módulo de Terraform probado contra LocalStack, con integración continua, y falta aplicarlo en AWS. El OE2 está **completo**. El OE3, al **95 %**: no medí la línea base manual y lo declaro como limitación. En total, el prototipo cumple el 95 %.

### 14 — Aporte · *6:40 – 7:00*

El aporte central es que Argos **aprende**: conserva lo resuelto, no depende de un solo proveedor de IA y se replica con un comando. Eso cubre el vacío del estado del arte.

### 15 — Análisis · *7:00 – 7:20*

Los resultados muestran que el contexto que recupera el RAG pesa más que el modelo: describir el stack real en el prompt fue el ajuste de mayor efecto. El umbral se calibra por el costo del error. Y no afirmo un porcentaje de reducción del MTTR, porque falta la línea base.

### 16 — Dificultades y limitaciones · *7:20 – 7:40*

Las dificultades vinieron de los servicios externos: el modelo original falló las 17 solicitudes por saturación, y una alerta no se reconocía a sí misma. Cada una tiene su ajuste. En el cierre encontré además que Argos conocía de antemano los contenedores del servicio que observa: hoy la evidencia se infiere desde Nginx, y el contexto y los runbooks son configuración. ✂️ Las limitaciones son el operador simulado, la rúbrica puntuada por un LLM y las muestras chicas.

### 17 — Trabajos futuros · *7:40 – 8:00*

Lo que sigue es aplicar el módulo de Terraform en AWS, hacer el estudio con operadores reales para medir la reducción del MTTR, sumar detección por contenido de los requests y revisar secretos en los documentos antes de enviarlos al modelo. Muchas gracias; quedo a disposición para sus preguntas.

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
| ¿Por qué Next.js con export estático y no con SSR? | El panel está detrás de un login y sus datos llegan en vivo desde la API con JWT: el SSR no aporta. El export estático da una ruta por vista y lo sirve el mismo Nginx, que conserva el proxy `/api` y el bloqueo de los endpoints internos. |
