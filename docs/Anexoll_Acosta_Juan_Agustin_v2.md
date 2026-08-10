05 de mayo de 2026

A la Sra. Decana de la  
Facultad de Ciencias Exactas y  Naturales y Agrimensura  
Mgter. María Viviana Godoy Guglielmone  
S	     /		 D:		  
De mi mayor consideración:  
Por la presente me dirijo a Usted, con el objeto de solicitarle tenga a bien considerar el inicio de mi Proyecto Final de Carrera denominado “Evaluación de un sistema inteligente de observabilidad para servicios web HTTP”, proponiendo como profesor(a) orientador(a) a Rios, Leopoldo Jose.   
Se adjunta a la presente nota, información anexa respecto al tema elegido, cronograma de actividades y datos personales.  
Sin otro particular, saludo a Usted muy atentamente.

\----------------------------------

Acosta, Juan Agustin  
LU. Nº 57851  
DNI Nº 43532954  
Moreno 445 Capital, Corrientes \- Argentina  
\+54 379 401 0731  
juanagustinacost@gmail.com

|  |  \----------------------------------------------Rios, Leopoldo Jose FIRMA  |
| :---- | :---- |

**ANEXO II**

**Plan de trabajo propuesto por el alumno con el aval del Profesor Orientador**

**Formulario 1\. Presentación de la Propuesta**

| Título del Trabajo | Argos: plataforma inteligente de AIOps para la detección, resolución asistida y documentación automática de incidentes en entornos web HTTP |  |  |  |  |
| :---- | :---- | :---- | :---- | :---- | :---- |
| Consignar tres palabras claves del trabajo |  AIOps, Observabilidad, Redes de datos, Inteligencia Artificial |  |  |  |  |
| Prof. Orientador (1) |  Rios, Leopoldo Jose |  |  |  Gob Velazco 1605, 3794598878, ljr@comunidad.unne.edu.ar |  |
| Prof. Orientador (2) |   |  |  | Dirección, teléfono y e-mail |  |
| Juan Agustin Acosta | 57851 | 43532954 | 2022 | Moreno 445\. \+543794010731  juanagustinacost@gmail.com |   |
| Clasificación del trabajo: (s/ Anexo I, pto.3) | Desarrollo de aplicaciones/sistemas |  |  |  |  |
| Área de la disciplina | Infraestructura y redes informáticas, Inteligencia Artificial |  |  |  |  |
| Cronograma |   |  |  |  |  |
| Trabajo enmarcado en: | Redes de datos |  |  |  |  |
| Los alumnos y profesores orientadores conocen y aceptan, que todos los resultados obtenidos estarán disponibles en la Facultad para la consulta. **Firmar alumnos y profesores orientadores**: |  |  |  |  |  |

# 

# **Introducción**

La creciente complejidad de los entornos de producción modernos —arquitecturas distribuidas, contenedores y exposición continua a Internet— ha incrementado los requisitos de visibilidad sobre el comportamiento de los servicios \[1\]. Las herramientas tradicionales de monitoreo, basadas en umbrales fijos sobre métricas de infraestructura, resultan insuficientes ante el volumen y la heterogeneidad de los datos generados \[2\], \[3\]: producen fatiga de alertas e impiden respuestas efectivas ante incidentes \[4\].  
En la operación cotidiana de las áreas de TI y DevOps, los incidentes en producción suelen ser difíciles de identificar y resolver. Con frecuencia ocurren sin un monitoreo adecuado y sin la información necesaria para comprender qué sucedió, qué servicio falló o cuál fue la causa raíz. Estas situaciones generan tiempos prolongados de resolución, trabajo manual adicional y una fuerte dependencia del conocimiento individual de cada integrante del equipo, incrementando el tiempo medio de resolución (MTTR) y el riesgo de repetir errores previamente resueltos.  
El presente trabajo propone el desarrollo de Argos, una plataforma de AIOps que se construye sobre un stack de observabilidad open-source y le agrega una capa inteligente de análisis, resolución asistida y documentación automática. Cuando el volumen de alertas emitidas por las herramientas de monitoreo supera un umbral configurable, Argos activa un flujo automatizado de orquestación que recupera contexto desde una base de conocimiento interna mediante recuperación aumentada de información (RAG), consulta un modelo de lenguaje (LLM) para sintetizar el problema y proponer pasos concretos de resolución, y —cuando el incidente no se encuentra documentado— genera y persiste automáticamente la documentación técnica correspondiente. De este modo, cada incidente se convierte en conocimiento reutilizable, fortaleciendo de forma continua el acervo organizacional y reduciendo el MTTR. La observabilidad, entendida como la capacidad de inferir el estado interno de un sistema a partir de sus salidas externas —métricas, logs y trazas— \[1\], constituye la base sobre la que opera la plataforma. La validación se realiza sobre un entorno simulado con Nginx como reverse proxy, elegido por su representatividad en despliegues reales y la riqueza semántica de sus logs HTTP.

## **Breve estado del arte**

Tradicionalmente, los equipos de operaciones dependían de herramientas como Nagios y Zabbix, que proporcionan alertas basadas en umbrales fijos sobre métricas de CPU, memoria, disco y red. Con la adopción masiva de microservicios y contenedores, Prometheus y Grafana se consolidaron como estándar de facto, introduciendo métricas en series temporales y tableros interactivos. Paralelamente, la gestión de logs evolucionó hacia sistemas centralizados como Loki, que optimiza el almacenamiento mediante indexación por labels en lugar de indexación de texto completo.  
El surgimiento de LLMs como Claude (Anthropic) y GPT (OpenAI) abrió nuevas posibilidades para el análisis automatizado de logs: a diferencia de los enfoques basados en expresiones regulares o aprendizaje automático supervisado, los LLMs comprenden el contexto semántico, detectan patrones sutiles y generan explicaciones en lenguaje natural \[4\], \[5\]. Sobre esta base, el campo de AIOps ha propuesto marcos para la gestión del ciclo de vida de incidentes \[6\]; sin embargo, la mayoría de las soluciones se concentran en el diagnóstico y la reducción de ruido de alertas, y son escasas las que cierran el ciclo generando y persistiendo documentación técnica reutilizable a partir de los incidentes analizados.  
En este contexto, Argos aborda esa brecha integrando, sobre un stack de observabilidad open-source, un flujo de orquestación que combina detección por umbrales, recuperación aumentada (RAG) sobre una base de conocimiento propia y análisis mediante LLM, incorporando como diferencial la auto-documentación de incidentes no registrados previamente. El aporte del trabajo reside en el diseño e implementación de esta capa inteligente y en la evidencia empírica sobre su capacidad para reducir el MTTR y enriquecer el conocimiento organizacional.

## **Objetivos**

**General:** Diseñar e implementar una plataforma de AIOps que, sobre un stack de observabilidad open-source, detecte incidentes a partir de umbrales de alertas, genere mediante LLM y RAG una síntesis del problema con pasos de resolución, y produzca automáticamente documentación técnica de los incidentes no documentados, con el fin de reducir el tiempo medio de resolución (MTTR) y fortalecer la base de conocimiento institucional.  
**Específicos:**

1. Implementar la arquitectura base de observabilidad —Nginx como reverse proxy, una API REST (ASP.NET Core) y una base de datos (SQL Server 2022\) como superficie HTTP, y un nodo centralizado con Prometheus, Loki, Grafana y Alertmanager—, desplegada como infraestructura como código mediante Terraform sobre LocalStack Pro y docker-compose para las aplicaciones, con migración posterior a un servicio en la nube.   
2. Definir reglas de detección basadas en umbrales sobre las alertas de Loki que actúen como trigger del flujo inteligente al superar el umbral configurable.   
3. Implementar una base de conocimiento vectorial y un pipeline de recuperación aumentada de información (RAG) que permita recuperar contexto relevante de incidentes previos.   
4. Implementar en n8n el motor de orquestación de Argos que, ante un trigger por umbral, ejecute el flujo completo: recuperación de contexto mediante RAG, consulta a la API del LLM (Claude/GPT) para generar una síntesis del problema y pasos concretos de resolución, y notificación automática al equipo responsable por email y/o Microsoft Teams.  
5. Implementar el mecanismo de auto-documentación: cuando el RAG no encuentre coincidencias suficientes en la base de conocimiento, Argos determina que el incidente es nuevo, genera la documentación técnica mediante el LLM y la persiste simultáneamente en la base vectorial —para futuras consultas RAG— y en el dashboard propio de la plataforma —para visibilidad del equipo—, asegurando la mejora continua del conocimiento organizacional.  
6. Desarrollar el dashboard propio de Argos, una interfaz web que centralice: el historial de incidentes analizados (fecha, tipo de alerta, resumen, pasos sugeridos y estado: documentado previamente, nuevo o auto-documentado); métricas agregadas (MTTR estimado por incidente y promedio histórico, cantidad de incidentes por tipo y tendencias temporales); y una sección de base de conocimiento donde se visualicen los documentos generados automáticamente por la plataforma.  
7. Validar el comportamiento de la plataforma mediante escenarios simulados con k6 (tráfico normal, picos de carga y patrones de ataque), evaluando la detección, la calidad de la propuesta generada por el LLM y la correcta persistencia de la documentación.  
8. Analizar los resultados, estimar la reducción de MTTR frente a un proceso manual de referencia e identificar limitaciones y líneas de mejora, tales como la incorporación de tickets de Jira como fuente adicional o la remediación automática de incidentes.

## **Fundamentación**

El proyecto se alinea con los contenidos de la Licenciatura en Sistemas de Información, particularmente con las materias de Redes de Datos e Internetworking. El enfoque en tráfico HTTP (capa 7 del modelo OSI) permite aplicar conceptos de protocolos de red, análisis de tráfico y seguridad en un contexto práctico. Nginx fue seleccionado como caso de estudio por su amplia adopción en producción y por la riqueza de sus logs de acceso, representativos de cualquier servicio web expuesto a Internet.  
La motivación surge de una problemática concreta y recurrente en entornos productivos reales: la dificultad para identificar y resolver incidentes cuando no se dispone de monitoreo adecuado ni de documentación organizada. La ausencia de logs completos y de una base de conocimiento consolidada obliga a búsquedas extensas, suposiciones y pruebas iterativas, afectando la eficiencia operativa y favoreciendo la repetición de errores. Argos responde a esta necesidad al capturar, unificar y analizar la información de los incidentes de forma automática, facilitando su detección temprana, su interpretación y la documentación del proceso de resolución. La integración de infraestructura como código (Docker, Terraform, LocalStack, AWS/GCP), orquestación (n8n) e inteligencia artificial (LLM y RAG) demuestra, además, capacidad de integración de sistemas modernos, competencia crítica en el área.

**Metodología**

Se adopta un marco ágil basado en Scrum Adaptado (Scrum Técnico), justificado por la naturaleza incremental de los proyectos de infraestructura y observabilidad, que requieren construir bases sólidas antes de agregar capas de funcionalidad. El enfoque se organiza bajo tres principios:

* **Entregas incrementales de arquitectura:** cada Sprint entrega un componente técnico habilitador del siguiente (slicing horizontal).  
* **Timeboxing:** Sprints de 2 a 3 semanas para evitar parálisis por análisis.  
* **Definition of Done verificable:** cada Sprint tiene entregables técnicos concretos (scripts funcionales, contenedores desplegados, flujos operativos) que evitan arrastrar deuda técnica.

En conclusión, el Scrum Adaptado aporta la estructura y el orden secuencial que requiere la infraestructura moderna, manteniendo la agilidad, el control de tiempos y la validación continua necesarios para el éxito del proyecto.

## **Fases de implementación**

**Sprint 1 — Infraestructura base (2 semanas).** Provisionar la infraestructura mediante Terraform sobre LocalStack Pro, orquestando los stacks Docker Compose interconectados. Entregable: flujo end-to-end Nginx → Alloy → Loki → Grafana operativo.

**Sprint 2 — Stack de observabilidad y reglas de detección (2 semanas).** Desplegar Prometheus, Loki, Grafana y Alertmanager; validar la recolección de métricas y logs; definir reglas de detección (umbrales sobre alertas de Loki) que actúan como trigger. Entregable: tableros funcionales con datos reales y reglas de alerta configuradas.

**Sprint 3 — Base de conocimiento y RAG (2 a 3 semanas).** Implementar base de conocimiento vectorial y pipeline de recuperación aumentada (RAG) que recupera contexto de incidentes previos; cargar la base con documentación técnica inicial de incidentes conocidos para habilitar las primeras consultas. Entregable: base de conocimiento con contenido inicial, pipeline RAG validado y criterio de similitud definido para determinar cuándo un incidente se considera nuevo.

**Sprint 4 — Orquestación de Argos en n8n (3 semanas).** Implementar los workflows que ejecutan el flujo completo: trigger por umbral → consulta RAG → construcción del prompt con contexto → llamada a la API del LLM → síntesis del incidente con pasos de resolución → notificación por email y/o Microsoft Teams. Entregable: flujo de extremo a extremo documentado, probado y con notificaciones operativas.

**Sprint 5 — Auto-documentación de incidentes (2 semanas).** Implementar el mecanismo de auto-documentación: cuando el RAG no encuentre coincidencias suficientes, el LLM genera la documentación técnica del incidente y la persiste simultáneamente en la base vectorial y en el dashboard. Desarrollar el dashboard propio de Argos con historial de incidentes (fecha, tipo, resumen, pasos y estado), métricas agregadas (MTTR estimado, incidentes por tipo, tendencias) y sección de consulta de la base de conocimiento generada automáticamente. Entregable: auto-documentación operativa, documentos visibles en el dashboard y métricas de MTTR calculadas.

**Sprint 6 — Simulación, evaluación y despliegue en cloud (2 semanas).** Ejecutar escenarios con k6 (tráfico normal, picos y patrones de ataque), evaluar detección, calidad de la propuesta y persistencia de documentación, y migrar la infraestructura a AWS o GCP reapuntando el provider de Terraform. Entregable: evidencia de pruebas, análisis de resultados e infraestructura desplegada en cloud.

# **Ámbito de trabajo**

**Dentro del alcance:** centralización y visualización de métricas de infraestructura; centralización de logs de Nginx; reglas de detección por umbrales como disparador; base de conocimiento vectorial y pipeline RAG; orquestación en n8n con análisis asistido por LLM; generación y persistencia automática de documentación de incidentes no registrados; propuesta de pasos de resolución (sin ejecución automática); notificaciones por email y Microsoft Teams; pruebas de carga y escenarios de ataque con k6; despliegue final en cloud mediante Terraform.

**Fuera del alcance:** remediación automática de incidentes (Argos solo propone, no ejecuta acciones correctivas); integración de fuentes adicionales de incidentes como tickets de Jira, Teams o email; despliegue productivo en un cliente real; sustitución de herramientas de seguridad perimetral (WAF, IDS/IPS); escalado horizontal masivo del stack de observabilidad.

# **Recursos**

**Humanos:** alumno responsable del proyecto con orientación del docente tutor.

**Hardware:** equipo anfitrión con un mínimo de 16 GB de RAM para ejecutar los stacks en paralelo; opcionalmente, créditos en AWS o GCP para el despliegue en cloud.

**Software:** Docker y Docker Compose; Terraform con provider AWS (compatible con LocalStack y cloud real); Prometheus, Grafana, Loki, Alertmanager, Grafana Alloy, n8n y k6; una base de datos vectorial para el RAG; Git y GitHub Actions para CI/CD; y la API de un LLM (Claude de Anthropic o GPT de OpenAI), con costo según el volumen de llamadas.

# **Desarrollos propuestos**

El desarrollo de Argos avanza en fases incrementales. En primer lugar se provisiona la infraestructura mediante Terraform sobre LocalStack Pro y se despliega el stack de observabilidad, con recolección de métricas y logs desde Nginx y el nodo de aplicación mediante Grafana Alloy. Sobre esa base se implementa la base de conocimiento vectorial y el pipeline RAG. A continuación se desarrolla el motor de orquestación en n8n, que implementa el flujo principal de Argos: ante un trigger por superación de umbral de alertas de Loki, el sistema consulta la base de conocimiento mediante RAG, construye el prompt con el contexto recuperado, consulta la API del LLM para generar una síntesis del incidente y pasos de resolución, y notifica al equipo por email y/o Microsoft Teams. Cuando el RAG no encuentra coincidencias suficientes, se activa el mecanismo de auto-documentación: el LLM genera la documentación técnica del incidente y la persiste simultáneamente en la base vectorial y en el dashboard propio de la plataforma. Este dashboard constituye la interfaz central de Argos e incluye el historial de incidentes analizados con su estado (documentado previamente / nuevo / auto-documentado), métricas agregadas como el MTTR estimado y la frecuencia de incidentes por tipo, y una sección de consulta de la base de conocimiento generada automáticamente. La validación se apoya en scripts k6 con escenarios de tráfico normal, picos y patrones de ataque. Como fase final, el stack completo —observabilidad y Argos— se despliega en AWS o GCP adaptando únicamente las credenciales del provider de Terraform.

# **Resultados esperados**

Se espera obtener un prototipo funcional documentado de la plataforma Argos: la arquitectura de observabilidad operativa mediante Terraform y Docker; tableros de Grafana con métricas e infraestructura; logs de Nginx centralizados en Loki; una base de conocimiento vectorial con su pipeline RAG; flujos en n8n que, ante incidentes, ejecuten el ciclo completo de Argos: análisis RAG \+ LLM, notificación por email/Teams, y auto-documentación de casos no registrados con persistencia en base vectorial y dashboard; un dashboard propio con historial de incidentes, métricas de MTTR y base de conocimiento visible; y evidencia de las pruebas con k6 sobre los escenarios simulados, incluyendo una estimación de la reducción de MTTR frente a un proceso manual de referencia. Como resultado académico se elaborará una memoria que describa las decisiones técnicas, las limitaciones del prototipo y las líneas de mejora, junto con el código versionado que permita reproducir el despliegue, edición manual de la base de conocimiento desde el dashboard (la escritura la realiza exclusivamente el mecanismo de auto-documentación); gestión de autenticación y roles de usuario en el dashboard (el prototipo académico no contempla control de acceso).

**Referencias Bibliográficas**  
\[1\] Y. Chen et al., "AlertGuardian: Intelligent Alert Life-Cycle Management for Large-scale Cloud Systems," *arXiv preprint arXiv:2601.14912*, 2026\. \[Online\]. Available: [https://arxiv.org/abs/2601.14912](https://arxiv.org/abs/2601.14912) 

\[2\] C. Albuquerque et al., "Tracing and Metrics Design Patterns for Monitoring Cloud-native Applications," *arXiv preprint arXiv:2510.02991*, 2025\. \[Online\]. Available: [https://arxiv.org/abs/2510.02991](https://arxiv.org/abs/2510.02991)

\[3\] M. Zagane et al., "Monitoring Data for Anomaly Detection in Cloud-Based Systems: A Systematic Mapping Study," *ACM Transactions on Software Engineering and Methodology*, 2025\. \[Online\]. Available: [https://dl.acm.org/doi/10.1145/3744556](https://dl.acm.org/doi/10.1145/3744556)

\[4\] S. Zhang et al., "LogEval: A Comprehensive Benchmark Suite for Large Language Models in Log Analysis," *arXiv preprint arXiv:2407.01896*, Jul. 2024\. \[Online\]. Available: [https://arxiv.org/abs/2407.01896](https://arxiv.org/abs/2407.01896)

\[5\] P. Gupta et al., "Scalable and Efficient Large-Scale Log Analysis with LLMs: An IT Software Support Case Study," *arXiv preprint arXiv:2511.14803*, Nov. 2024\. \[Online\]. Available: [https://arxiv.org/abs/2511.14803](https://arxiv.org/abs/2511.14803)

\[6\] E. Lim et al., "AIOps Solutions for Incident Management: Technical Guidelines and A Comprehensive Literature Review," *arXiv preprint arXiv:2404.01363*, Apr. 2024\. \[Online\]. Available: [https://arxiv.org/abs/2404.01363](https://arxiv.org/abs/2404.01363)   
\[7\] G. Greco et al., "A Comparative Analysis of Load Testing Tools for Microservice Architectures," *IEEE Access*, vol. 8, pp. 196354–196375, 2020\. \[Online\]. Available: [https://ieeexplore.ieee.org/document/9247453](https://ieeexplore.ieee.org/document/9247453) 

5 de mayo de 2026  
Sra. Decano de la Facultad de Ciencias Exactas y Naturales y Agrimensura   
Dr. Jose Luis Fontana  
S 	/	D:   
De mi mayor consideración:   
Me dirijo a Usted, a los efectos de solicitarle la evaluación del Proyecto Final de Carrera titulado*: “Evaluación de un sistema inteligente de observabilidad para servicios web HTTP”* al Comité de Ética en investigación para su evaluación. Este proyecto será dirigido por quienes suscriben y será desarrollado en la Facultad de Ciencias Exactas y Naturales y Agrimensura. 

Adjunto a la presente una versión digital del proyecto, como así también una hoja de datos exigida en el reglamento vigente.   
Sin otro particular, saludo a Usted muy atentamente.

FIRMAR ESTUDIANTE  
FIRMAR PROFESORES ORIENTADORES

**ANEXO II – RES 0182/18**  
**HOJA DE DATOS**  
**DIRECTOR O RESPONSABLE DEL PROYECTO**   
**Apellido y Nombre:**   
**Correo electrónico:**   
**Lugar de trabajo:**   
**Teléfono fijo o celular:** 

**CO-DIRECTOR O O CO-RESPONSABLE DEL PROYECTO**   
**Apellido y Nombre**  
**Correo electrónico**  
**Lugar de trabajo**  
**Teléfono fijo o celular**

 **Completar la siguiente tabla marcando con una X**

| En el proyecto se ha trabajado con | SI | NO |
| :---- | ----- | :---: |
| **Datos de personas (historias clínicas, encuestas, etc.)** |  | X |
| **Muestras biológicas humanas** |  | X |
| **Animales de laboratorio** |  | X |
| **Microorganismos** |  | X |
| **Virus** |  | X |
| **Compuestos químicos peligrosos (tóxicos o explosivos)** |  | X |
| **Radiaciones** |  | X |

**Nota:** las encuestas realizadas tendrán un carácter estrictamente confidencial y contarán con una nota de consentimiento para los encuestados.  
**LOS DATOS CONSIGNADOS EN ESTE DOCUMENTO TIENEN CARÁCTER DE DECLARACIÓN JURADA**

**FIRMAR PROF. ORIENTADORES Y ESTUDIANTE**