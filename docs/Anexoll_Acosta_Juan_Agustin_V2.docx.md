05 de mayo de 2026

Al Sr. Decano de la  
Facultad de Ciencias Exactas y  Naturales y Agrimensura  
Dr. José L. Fontana  
S	     /		 D:		  
De mi mayor consideración:  
Por la presente me dirijo a Usted, con el objeto de solicitarle tenga a bien considerar el inicio de mi Proyecto Final de Carrera denominado “Argos: plataforma inteligente de AIOps para la detección, resolución asistida y documentación automática de incidentes en entornos web”, proponiendo como profesor(a) orientador(a) a Rios, Leopoldo Jose.   
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

| Título del Trabajo | Argos: plataforma inteligente de AIOps para la detección, resolución asistida y documentación automática de incidentes en entornos web |  |  |  |  |
| :---- | :---- | :---- | :---- | :---- | :---- |
| Consignar tres palabras claves del trabajo |  AIOps, Observabilidad, Redes de datos, Inteligencia Artificial |  |  |  |  |
| Prof. Orientador (1) |  Rios, Leopoldo Jose |  |  |  Gob Velazco 1605, 3794598878, ljr@comunidad.unne.edu.ar |  |
| Prof. Orientador (2) |   |  |  | Dirección, teléfono y e-mail |  |
| Juan Agustin Acosta | 57851 | 43532954 | 2022 | Moreno 445\. \+543794010731  juanagustinacost@gmail.com |   |
| Clasificación del trabajo: (s/ Anexo I, pto.3) | Desarrollo de aplicaciones/sistemas |  |  |  |  |
| Área de la disciplina | Infraestructura y redes informáticas, Inteligencia Artificial aplicada al monitoreo de software |  |  |  |  |
| Cronograma |  Ver tabla 1 \- Cronograma de actividades |  |  |  |  |
| Trabajo enmarcado en: | Asignatura: Proyecto Final de Carrera |  |  |  |  |
| Los alumnos y profesores orientadores conocen y aceptan, que todos los resultados obtenidos estarán disponibles en la Facultad para la consulta. **Firmar alumnos y profesores orientadores**: |  |  |  |  |  |

# 

# **I) Introducción**

La creciente complejidad de los entornos de producción modernos, arquitecturas distribuidas, contenedores y exposición continua a Internet ha incrementado los requisitos de visibilidad sobre el comportamiento de los servicios \[1\]. Las herramientas tradicionales de monitoreo, basadas en umbrales fijos sobre métricas de infraestructura, resultan insuficientes ante el volumen de los datos generados \[2\], \[3\] produciendo fatiga de alertas que impiden respuestas efectivas ante incidentes \[4\].  
Cotidianamente en las áreas de TI y DevOps, los incidentes en entornos de producción pueden ser difíciles de identificar y resolver. Ya que el análisis ocurre sin un monitoreo adecuado y sin información comprensible para abordar lo qué sucedió, qué servicio falló o cuál fue la causa raíz. Estas situaciones generan tiempos prolongados de resolución y una fuerte dependencia al conocimiento individual de cada integrante del equipo, incrementando el tiempo medio de resolución (MTTR) y el riesgo de repetir errores resueltos.  
El presente trabajo propone el desarrollo de Argos, una plataforma de Inteligencia Artificial para Operaciones de TI (AIOps) que se construye sobre un stack de observabilidad de código abierto, agregando una capa inteligente de análisis, resolución asistida y documentación automática. Cuando el volumen de alertas emitidas por las herramientas de monitoreo supera un umbral configurable, Argos activa un flujo automatizado de orquestación que recupera contexto desde una base de conocimiento interna mediante recuperación aumentada de información (RAG), consulta un modelo de lenguaje (LLM) para sintetizar el problema y proponer pasos concretos de resolución, y, cuando el incidente no se encuentra documentado, genera y persiste automáticamente la documentación técnica correspondiente.   
La observabilidad, entendida como la capacidad de inferir el estado interno de un sistema a partir de sus métricas, logs y trazas \[1\], constituye la base sobre la que opera la plataforma. La validación se realiza sobre un entorno simulado con un proxy inverso como componente central, aprovechando la riqueza semántica de sus logs HTTP para la evaluación del sistema. 

## **a. Breve estado del arte**

Tradicionalmente, los equipos de operaciones dependían de herramientas como Nagios y Zabbix, que proporcionan alertas basadas en umbrales fijos sobre métricas de CPU, memoria, disco y red. Con la adopción masiva de microservicios y contenedores, Prometheus y Grafana se consolidaron como estándar de facto, introduciendo métricas en series temporales y tableros de visualización. Paralelamente, la gestión de logs evolucionó hacia sistemas centralizados como Loki, que optimiza el almacenamiento mediante indexación por labels en lugar de indexación de texto completo.  
La evolución de los LLMs abrió nuevas posibilidades para el análisis automatizado de logs ya que comprenden el contexto semántico, detectan patrones sutiles y generan explicaciones en lenguaje natural \[4\], \[5\]. Sobre esta base, el campo de AIOps ha propuesto marcos para la gestión del ciclo de vida de incidentes \[6\]; sin embargo, la mayoría de las soluciones se concentran en el diagnóstico y la reducción de ruido de alertas, y son escasas las que cierran el ciclo generando y persistiendo documentación técnica reutilizable a partir de los incidentes analizados. En este contexto, Argos aborda esa brecha incorporando, como diferencial frente a las soluciones existentes, la autodocumentación de incidentes no registrados previamente. El aporte del trabajo reside en el diseño e implementación de esta capa inteligente con un panel administrativo y de historial, y en la evidencia empírica sobre la capacidad para reducir el MTTR.

## **b. Objetivos**

**General:** Diseñar e implementar una plataforma de software para soportar la detección de incidentes a partir de umbrales de alertas, genere mediante LLM y RAG una síntesis del problema con pasos de resolución, y produzca en forma automatizada reportes con documentación técnica de los incidentes no documentados.  
**Específicos:**

1. Implementar la arquitectura base de observabilidad (reverse proxy, API REST y base de datos) como superficie HTTP, y un nodo centralizado con Prometheus, Loki, Grafana y Alertmanager, desplegada como infraestructura como código mediante Terraform sobre LocalStack Pro y Docker Compose para las aplicaciones, con migración posterior a un servicio en la nube, y definir las reglas de detección basadas en umbrales que actúen como trigger del flujo inteligente.  
2. Implementar la capa inteligente de Argos: una base de conocimiento vectorial con su pipeline de recuperación aumentada de información (RAG), y un motor de orquestación que, ante un trigger, recupere contexto mediante RAG, consulte la API de un LLM para generar una síntesis del problema con pasos de resolución, y active la autodocumentación de incidentes no registrados en la base de conocimiento.  
3. Validar el comportamiento de la plataforma mediante escenarios simulados con k6 (tráfico normal, picos de carga y patrones de ataque), evaluando la detección, la calidad de la propuesta generada por el LLM y la correcta persistencia de la documentación.

## **c. Fundamentación**

La resolución de incidentes en entornos de producción representa uno de los mayores costos de operación de TI ya que cada minuto de indisponibilidad de un servicio impacta directamente en ingresos, reputación y confianza del usuario. En la práctica, cuando no existe monitoreo adecuado ni documentación organizada, la identificación de la causa raíz depende del conocimiento tácito de unos pocos integrantes del equipo, lo que extiende los tiempos de resolución (MTTR), incrementa el costo operativo y expone a la organización a repetir errores ya resueltos anteriormente.  
Argos propone una respuesta concreta a esta problemática: automatizar la captura, el análisis y la documentación de incidentes para reducir la dependencia de expertos puntuales, acelerar la resolución y convertir cada incidente en conocimiento reutilizable para la organización \[1\], \[6\]. Al integrar detección de anomalías \[3\], generación de diagnósticos asistidos por LLM \[4\], \[5\] y documentación automática en un único flujo, la plataforma plantea una alternativa a herramientas de monitoreo tradicionales \[2\] que no capitalizan el conocimiento generado en cada incidente ni ayudan al equipo de trabajo.

# **II) Metodología**

Se adopta un marco ágil basado en Scrum Adaptado (Scrum Técnico), justificado por la naturaleza incremental de los proyectos de infraestructura y observabilidad, que requieren construir bases sólidas antes de agregar capas de funcionalidad. El enfoque se organiza bajo tres principios:

* Entregas incrementales de arquitectura: cada Sprint entrega un componente técnico habilitador del siguiente (slicing horizontal).  
* Timeboxing (asignación de plazos fijos): Sprints de 2 a 3 semanas para evitar parálisis por análisis.  
* Definition of Done verificable: cada Sprint tiene entregables técnicos concretos (scripts funcionales, contenedores desplegados, flujos operativos) que evitan arrastrar deuda técnica.

En síntesis, el Scrum Adaptado aporta la estructura y el orden secuencial que requiere la infraestructura moderna, manteniendo la agilidad, el control de tiempos y la validación continua necesarios para el éxito del proyecto \[8\].  
La ejecución del proyecto bajo este marco demanda, en la práctica, un dominio técnico transversal a varias disciplinas: la implementación de infraestructura como código (Terraform, Docker) y su orquestación en entornos simulados y en la nube \[9\]; el desarrollo de sistemas de recuperación aumentada de información (RAG) y su integración con modelos de lenguaje (LLM) para el análisis semántico de datos operativos \[4\], \[5\]; y el desarrollo web propiamente dicho, tanto en la construcción de la superficie HTTP expuesta (API REST y base de datos) como en la interpretación de tráfico y logs a nivel de aplicación. La organización por Sprints permite, además, validar cada uno de estos componentes de forma incremental antes de integrarlos en el flujo completo de la plataforma.

# **III) Ámbito de trabajo**

Las actividades de investigación y desarrollo técnico se llevan a cabo de forma independiente por el estudiante, en un entorno de desarrollo y simulación propio, sin vinculación con convenios externos ni proyectos de investigación financiados por terceros. El diseño, desarrollo y validación toman como caso de estudio el tráfico HTTP hacia un servicio web expuesto , empleando escenarios de tráfico simulado, tráfico normal, picos de carga y patrones de ataque, generados por el estudiante, lo que permite ejercitar de forma reproducible tanto los caminos normales como las condiciones de falla del sistema.

# **IV) Recursos**

**Humanos:** alumno responsable del proyecto con orientación del docente tutor.  
**Hardware:** el proyecto contempla dos entornos de ejecución con requisitos diferenciados.  
**Entorno local de desarrollo:** equipo anfitrión con un mínimo de 8 GB de RAM, 4 núcleos de CPU y 60GB de almacenamiento, sobre el cual se ejecutan todos los *stacks* de forma simultánea. Este entorno se utiliza durante las fases de desarrollo e integración de cada Sprint, antes de la validación en infraestructura distribuida.  
**Entorno productivo (despliegue en la nube):** infraestructura distribuida en la nube, con un requisito mínimo global de 7 vCPU, 10 GB de RAM y 50 GB de almacenamiento aproximado, sobre la que se despliegan la superficie HTTP, el stack de observabilidad , los agentes de recolección de métricas y la capa de orquestación e inteligencia. Opcionalmente, se contemplan créditos en AWS o GCP para cubrir el costo del despliegue.  
**Software:** Docker y Docker Compose; Terraform con provider AWS; LocalStack Pro para la simulación local de servicios de AWS; Nginx como reverse proxy; ASP.NET Core (.NET SDK) para la API REST; SQL Server 2022 como motor de base de datos; Prometheus, Grafana, Loki, Alertmanager, Grafana Alloy, n8n y scripts simulados o software para generar tráfico red; una base de datos vectorial para el RAG; Git y GitHub Actions para CI/CD; y la API de un LLM .  
**V) Desarrollos propuestos**  
El desarrollo avanza en fases. En primer lugar se provisiona la infraestructura de stacks Docker mediante Terraform sobre LocalStack Pro y se despliega el stack de observabilidad, compuesto por Prometheus, Loki, Grafana y Alertmanager, con recolección de métricas y logs desde los nodos de Nginx y de aplicación mediante agentes de Grafana Alloy, y se definen las reglas de detección basadas en umbrales sobre las alertas de Loki que actúan como disparador del flujo inteligente. Sobre esa base se implementa la base de conocimiento vectorial y el pipeline de recuperación aumentada de información (RAG), y luego el motor de orquestación de Argos en n8n que, ante un disparador por umbral, recupera contexto mediante RAG, consulta la API del LLM y genera una síntesis del incidente con pasos de resolución. A continuación se implementa el mecanismo de auto documentación, que genera y persiste documentación técnica para los incidentes no registrados. Sobre esta base se desarrolla, en Next.js, el panel de la plataforma Argos, que centraliza la visualización de incidentes, las síntesis y propuestas de resolución generadas por el LLM, y el acceso a la documentación autogenerada. La API ASP.NET Core y SQL Server 2022 operan como superficie HTTP detrás de Nginx. La validación se apoya en escenarios de k6 con tráfico normal, picos de carga y patrones de ataque. Como fase final, el código de Terraform se despliega en AWS o GCP, adaptando únicamente las credenciales del proveedor.

# **VI) Resultados esperados**

Se espera obtener un prototipo funcional y documentado de la plataforma Argos. La arquitectura de observabilidad, desplegada como infraestructura como código mediante Terraform y Docker, centralizará métricas de infraestructura en tableros de Grafana y logs de Nginx consolidados en Loki, sobre una base de conocimiento vectorial con su pipeline de recuperación aumentada de información (RAG). A partir de esta base, los flujos de orquestación implementados en n8n generarán, ante cada incidente, una síntesis del problema y una propuesta concreta de resolución mediante LLM, y documentará automáticamente los casos no registrados previamente, ampliando de forma continua el activo de conocimiento de la organización. Todo este flujo será accesible a través de un panel desarrollado en Next.js, que centraliza la visualización de incidentes, las propuestas generadas y la documentación disponible, ofreciendo una interfaz unificada para la toma de decisiones operativas.  
La validación del prototipo se sustentará en evidencia empírica obtenida mediante pruebas de carga y escenarios simulados con k6, incluyendo una estimación cuantitativa de la reducción del tiempo medio de resolución (MTTR) frente a un proceso manual de referencia, como indicador central del valor que la plataforma aportaría en un entorno productivo real.  
Como resultado académico, se elaborará una memoria que documente las decisiones técnicas adoptadas, las limitaciones identificadas en el prototipo y las líneas de mejora futuras, junto con el código versionado que garantice la reproducibilidad del despliegue.

# **VII) Cronograma de Actividades**

| Etapa | Período | Actividad |
| :---- | :---- | :---- |
| Sprint 1: Infraestructura base | Semanas 1-2 | Provisionar la infraestructura mediante Terraform sobre LocalStack Pro, orquestando los stacks de Docker Compose interconectados. |
| Sprint 2: Stack de observabilidad y reglas de detección | Semanas 3-4 | Desplegar Prometheus, Loki, Grafana y Alertmanager; validar la recolección de métricas y logs. |
| Sprint 3: Base de conocimiento y RAG | Semanas 5-7 | Implementar la base de conocimiento vectorial y el pipeline de recuperación aumentada de información (RAG). |
| Sprint 4: Orquestación de Argos | Semanas 8-10 | Implementar los workflows que, ante un disparador, ejecutan el flujo detección \-\> RAG \-\> LLM \-\> propuesta de resolución. |
| Sprint 5: Autodocumentación de incidentes | Semanas 11-12 | Implementar el mecanismo que, ante un incidente no documentado, genera la documentación técnica mediante el LLM. |
| Sprint 6: Simulación, evaluación y despliegue en la nube | Semanas 13-14 | Ejecutar escenarios con k6, evaluar la detección y migrar la infraestructura a AWS o GCP. |

* Tabla 1\. Cronograma de actividades del Proyecto Final de Carrera

**Referencias Bibliográficas**  
\[1\] Y. Chen et al., "AlertGuardian: Intelligent Alert Life-Cycle Management for Large-scale Cloud Systems," *arXiv preprint arXiv:2601.14912*, 2026\. \[Online\]. Available: [https://arxiv.org/abs/2601.14912](https://arxiv.org/abs/2601.14912) 

\[2\] C. Albuquerque et al., "Tracing and Metrics Design Patterns for Monitoring Cloud-native Applications," *arXiv preprint arXiv:2510.02991*, 2025\. \[Online\]. Available: [https://arxiv.org/abs/2510.02991](https://arxiv.org/abs/2510.02991)

\[3\] M. Zagane et al., "Monitoring Data for Anomaly Detection in Cloud-Based Systems: A Systematic Mapping Study," *ACM Transactions on Software Engineering and Methodology*, 2025\. \[Online\]. Available: [https://dl.acm.org/doi/10.1145/3744556](https://dl.acm.org/doi/10.1145/3744556)

\[4\] S. Zhang et al., "LogEval: A Comprehensive Benchmark Suite for Large Language Models in Log Analysis," *arXiv preprint arXiv:2407.01896*, Jul. 2024\. \[Online\]. Available: [https://arxiv.org/abs/2407.01896](https://arxiv.org/abs/2407.01896)

\[5\] P. Gupta et al., "Scalable and Efficient Large-Scale Log Analysis with LLMs: An IT Software Support Case Study," *arXiv preprint arXiv:2511.14803*, Nov. 2024\. \[Online\]. Available: [https://arxiv.org/abs/2511.14803](https://arxiv.org/abs/2511.14803)

\[6\] E. Lim et al., "AIOps Solutions for Incident Management: Technical Guidelines and A Comprehensive Literature Review," *arXiv preprint arXiv:2404.01363*, Apr. 2024\. \[Online\]. Available: [https://arxiv.org/abs/2404.01363](https://arxiv.org/abs/2404.01363)   
\[7\] G. Greco et al., "A Comparative Analysis of Load Testing Tools for Microservice Architectures," *IEEE Access*, vol. 8, pp. 196354–196375, 2020\. \[Online\]. Available: [https://ieeexplore.ieee.org/document/9247453](https://ieeexplore.ieee.org/document/9247453) 

\[8\] M. Hron and N. Obwegeser, "Why and How is Scrum Being Adapted in Practice: A Systematic Review," Journal of Systems and Software, vol. 183, Art. 111110, 2022\. \[Online\]. Available: [https://doi.org/10.1016/j.jss.2021.111110](https://doi.org/10.1016/j.jss.2021.111110)

\[9\] M. Guerriero, M. Garriga, D. A. Tamburri, and F. Palomba, "Adoption, Support, and Challenges of Infrastructure-as-Code: Insights from Industry," in *2019 IEEE International Conference on Software Maintenance and Evolution (ICSME)*, 2019, pp. 580–589. \[Online\]. Available: [https://doi.org/10.1109/ICSME.2019.00092](https://doi.org/10.1109/ICSME.2019.00092) 

5 de mayo de 2026  
Sr. Decano de la Facultad de Ciencias Exactas y Naturales y Agrimensura   
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
**Apellido y Nombre:          RIOS, Leopoldo José**  
**Correo electrónico:          ljr@comunidad.unne.edu.ar**  
**Lugar de trabajo:              Facultad Cs. Exactas \- UNNE**  
**Teléfono fijo o celular:     3794 598878**

**CO-DIRECTOR O CO-RESPONSABLE DEL PROYECTO**   
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