# Plan de la Plataforma de Monitoreo Argos

## 1. Visión General
**Argos** es una plataforma de AIOps diseñada para la detección, resolución asistida y documentación automática de incidentes en entornos web. Busca reducir el tiempo medio de resolución (MTTR) y mitigar la fatiga de alertas integrando herramientas clásicas de observabilidad open-source con inteligencia artificial (RAG + LLM).

## 2. Arquitectura de la Plataforma

La plataforma se estructura en 4 capas principales:

### 2.1. Superficie de Aplicación (El Entorno Monitoreado)
- **Reverse Proxy (Nginx):** Actúa como puerta de entrada, manejando tráfico HTTP/HTTPS, rate limiting y generando logs detallados.
- **Backend (API ASP.NET Core) y Base de Datos (SQL Server):** La aplicación objetivo que genera métricas y comportamiento de consumo a ser evaluados.

### 2.2. Capa de Observabilidad
- **Métricas:** `Prometheus` recolecta métricas de infraestructura (vía `node-exporter`, `cadvisor`) y de servicios (`nginx-exporter`).
- **Logs:** `Grafana Alloy` recolecta y parsea logs de Nginx y de la aplicación, enviándolos a `Loki`.
- **Alertas:** `Alertmanager` recibe los triggers configurados (umbrales superados) desde Prometheus y Loki.
- **Visualización Base:** `Grafana` provee los dashboards técnicos tradicionales de infraestructura.

### 2.3. Capa Inteligente (AIOps y Orquestación)
- **Base de Conocimiento (RAG):** Utiliza `Qdrant` como base de datos vectorial y una API en .NET 10 (`argos-api/`) para la indexación y recuperación aumentada de información histórica de incidentes y manuales.
- **Orquestación:** `n8n` recibe el webhook de `Alertmanager` cuando ocurre una alerta, consulta el sistema RAG para obtener contexto histórico, y se comunica con la API del LLM (ej. Anthropic Claude).
- **Análisis de LLM:** El modelo de lenguaje sintetiza el problema, infiere la causa raíz a partir de las métricas/logs provistos y sugiere pasos concretos de resolución.

### 2.4. Gestión de Identidad y Acceso (IAM)
- **Autenticación y Autorización:** Implementación de un sistema de login seguro basado en JWT (JSON Web Tokens) o integración con proveedores OAuth2 (ej. Azure AD, Google Workspace, GitHub).
- **Roles y Permisos (RBAC):** La plataforma distinguirá entre distintos tipos de usuarios:
  - *Administradores:* Acceso total, configuración de integraciones (LLM, RAG, Webhooks) y gestión de usuarios.
  - *Operadores / DevOps:* Capacidad de ver incidentes, aprobar resoluciones propuestas por la IA, disparar acciones correctivas y editar la autodocumentación.
  - *Lectores (Read-Only):* Desarrolladores o stakeholders que solo necesitan visualizar el estado del sistema, el MTTR y el historial de incidentes.
- **Trazabilidad de Acciones:** Cada cambio de estado de un incidente (ej. "marcar como resuelto", "aprobar documentación") registrará qué usuario realizó la acción, manteniendo una pista de auditoría (audit trail) indispensable en entornos de operaciones.

### 2.5. Capa de Autodocumentación e Interfaz (Dashboard Argos)
- **Documentación:** Si el incidente analizado no está en la base de conocimiento (evaluado por el RAG y LLM), el sistema genera y persiste automáticamente la documentación técnica (autodocumentación) para enriquecer el vector db.
- **Interfaz (Argos Dashboard):** Un panel administrativo desarrollado en React (Vite) que centraliza la gestión del flujo. No reemplaza a Grafana, sino que se enfoca en el ciclo de vida del incidente asistido por IA.

---

## 3. Flujo Operativo de Resolución de Incidentes

1. **Detección:** Loki/Prometheus detectan un pico (ej. >5% errores 5xx en Nginx) y disparan una alerta a Alertmanager.
2. **Disparo:** Alertmanager envía un webhook a n8n.
3. **Recuperación (RAG):** n8n consulta la API RAG buscando incidentes o documentación similar.
4. **Análisis (LLM):** n8n envía los datos de la alerta y los resultados del RAG al LLM para generar un diagnóstico y plan de acción.
5. **Autodocumentación:** Si el problema es inédito y posteriormente resuelto/validado, se documenta e indexa en Qdrant.
6. **Resolución Colaborativa:** 
   - Los ingenieros (previamente logueados y autenticados) reciben una notificación.
   - Acceden al Dashboard de Argos para visualizar el incidente y el análisis de la IA.
   - Un operador aprueba la resolución o ajusta los pasos. Al aplicar la resolución, el sistema asocia la acción a su usuario (audit trail) y marca el incidente como "Resuelto".
---

## 4. Prompt para Generación de Mockup (UI/UX)

Puedes utilizar el siguiente prompt en herramientas como v0.dev, Claude o ChatGPT (con capacidades de generación de UI) para crear un mockup de la aplicación React de Argos:

***

**Prompt para generar el Mockup del Dashboard de Argos:**

```text
Actúa como un experto desarrollador frontend y diseñador UX/UI. Quiero que generes el código en React (usando Tailwind CSS y componentes estilo shadcn/ui) para el dashboard principal de "Argos", una plataforma inteligente de AIOps.

El diseño debe ser moderno, técnico (tema oscuro preferentemente, estilo herramientas DevOps/Grafana) y transmitir que está potenciado por Inteligencia Artificial y diseñado para colaboración de equipos de operaciones.

### 1. Vista de Autenticación (Login)
Debe incluir una pantalla de inicio de sesión limpia con:
- Título/Logo "Argos AIOps".
- Campos para Correo Electrónico y Contraseña.
- Botones de "Iniciar Sesión" y alternativas como "Continuar con Google / GitHub".
- Una nota que indique "Autenticación segura para equipos de DevOps".

### 2. Vista Principal del Dashboard
Una vez logueado, la interfaz debe contener una barra lateral (Sidebar) de navegación con los siguientes elementos:
- Dashboard (Inicio)
- Incidentes Activos
- Base de Conocimiento (RAG)
- Métricas de Sistema (MTTR)
- Gestión de Usuarios y Roles (Admin)
- Configuración

En la barra superior (Header) debe verse el avatar del usuario autenticado, su nombre (ej. "Juan A."), su rol ("Admin" u "Operador") y un botón para desloguearse.

### 3. Contenido de Incidentes Activos (Vista principal):
1. Un resumen (KPIs) en la parte superior: "MTTR actual", "Incidentes hoy", "Auto-documentados este mes".
2. Una lista o tabla de "Incidentes Recientes" con columnas: Severidad (Critico, Warning), Componente (ej. Nginx, API), Descripción breve, y Estado (Analizando IA, Resuelto, Documentado).
3. Al hacer clic en un incidente (muestra una vista de detalle simulada o modal a la derecha), se debe visualizar:
   - Los datos técnicos de la alerta (métricas y logs relevantes en un pequeño bloque de código).
   - Un panel destacado brillante o con un ícono de IA llamado "Análisis de Argos (LLM + RAG)", donde se muestre una síntesis redactada del problema ("Se detectó un pico de errores 5xx debido a saturación en el backend...") y una lista de "Pasos de resolución recomendados".
   - Trazabilidad de Auditoría: Un registro que muestre si algún usuario interactuó con el incidente ("Asignado a Juan A. hace 5 min").
   - Un botón de acción según el rol: "Aprobar y Auto-documentar" o "Marcar como Resuelto" (que indicará quién realizó la acción en el log).

Usa gráficos de barra o línea simples (puedes simularlos con recharts o CSS) para el progreso del MTTR. El diseño debe verse limpio, profesional y enfocado en ayudar al equipo de operaciones a resolver incidentes colaborativamente y de forma segura.
```
