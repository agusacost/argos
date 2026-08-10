---
tipo_alerta: NginxHighErrorRate
resumen: "Alta tasa de errores HTTP 5xx en Nginx (>5% de las requests durante 5 minutos)"
pasos:
  - "Verificar logs de error de Nginx: docker logs nginx-proxy --tail=100"
  - "Revisar estado de la API .NET: curl http://localhost:5000/api/status"
  - "Comprobar que SQL Server responde: docker exec membresia-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q 'SELECT 1'"
  - "Revisar uso de recursos del contenedor API: docker stats membresia-api --no-stream"
  - "Reiniciar el servicio API si es necesario: docker compose -p argos-apps restart api"
  - "Si el problema persiste, revisar los logs de la aplicación en Grafana (dashboard nginx-logs)"
estado: documentado_previamente
fuente: manual
---

## Contexto

La alerta `NginxHighErrorRate` se dispara cuando la tasa de errores 5xx supera el 5% en una
ventana de 5 minutos. Los errores 5xx en Nginx indican que las requests llegan al proxy pero
fallan en el upstream (API o base de datos).

## Causas comunes

- La API .NET no responde o está caída (connection refused / timeout)
- SQL Server no está disponible o la BD no está inicializada
- Falta de recursos del host (CPU/RAM)
- Errores no manejados en la aplicación

## Criterio de resolución

La alerta se resuelve sola cuando el error rate vuelve a < 5% durante 5 minutos.
