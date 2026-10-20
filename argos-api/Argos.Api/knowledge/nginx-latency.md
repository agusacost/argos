---
tipo_alerta: NginxHighResponseTime
resumen: "Latencia alta en Nginx: P95 del upstream_response_time supera 2 segundos durante 5 minutos"
pasos:
  - "Revisar métricas de latencia en Grafana (dashboard nginx-overview, panel Latency P95)"
  - "Verificar carga del host: docker stats --no-stream | grep -E 'CPU|MEM'"
  - "Revisar consultas lentas en SQL Server usando Query Store o sys.dm_exec_requests"
  - "Comprobar índices en membresia.Usuarios: faltan índices pueden causar table scans"
  - "Si la causa es SQL: revisar el plan de ejecución del SP membresia.sp_ObtenerUsuariosPaginado"
  - "Considerar agregar índice: CREATE INDEX IX_Usuarios_FechaCreacion ON membresia.Usuarios(FechaCreacion DESC)"
  - "Revisar si hay contención de locks en SQL Server: SELECT * FROM sys.dm_exec_requests WHERE blocking_session_id > 0"
estado: documentado_previamente
fuente: manual
---

## Contexto

La alerta `NginxHighResponseTime` se dispara cuando el percentil 95 del tiempo de respuesta
upstream supera 2 segundos en una ventana de 5 minutos. Se mide desde el campo
`upstream_response_time` del access log de Nginx (tiempo de respuesta de la API).

## Causas comunes

- Consultas SQL lentas (table scans por falta de índices)
- Contención de locks en SQL Server (múltiples writes simultáneos)
- Alta carga del host (CPU saturado → context switching)
- N+1 queries en la capa de repository (poco probable con SP paginado)

## Métricas relacionadas

- `HostHighCPU` (puede co-ocurrir si el CPU del host está saturado)
- `ContainerHighCPU` para el contenedor membresia-api
