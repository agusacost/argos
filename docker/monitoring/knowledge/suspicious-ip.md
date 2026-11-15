---
tipo_alerta: NginxSuspiciousIPActivity
resumen: "Actividad sospechosa detectada: una IP genera más de 200 requests por minuto en Nginx"
pasos:
  - "Identificar la IP en Loki: {service='nginx', log_type='access'} | json | remote_addr='<IP>'"
  - "Analizar el patrón de requests: paths solicitados, User-Agent, distribución temporal"
  - "Verificar si la IP aparece en listas negras conocidas (abuse.ch, AbuseIPDB)"
  - "Si es un ataque confirmado, bloquear en Nginx: agregar 'deny <IP>;' en nginx.conf"
  - "Alternativamente, usar rate limiting más agresivo: reducir zona 'api' a 5r/s para esa IP"
  - "Aplicar cambio sin downtime: docker exec nginx-proxy nginx -s reload"
  - "Monitorear si la IP rota (botnet): revisar si el patrón se repite con otras IPs"
  - "Documentar el incidente con fecha, IP, patrón y acción tomada"
estado: documentado_previamente
fuente: manual
---

## Contexto

La alerta `NginxSuspiciousIPActivity` se dispara cuando una única IP genera más de 200 requests
por minuto, medido en logs de Nginx vía Loki. Puede indicar scraping agresivo, fuerza bruta,
o el inicio de un ataque DDoS.

## Causas comunes

- Web scraping automatizado
- Fuerza bruta contra endpoints de autenticación
- DDoS (etapa inicial con pocas IPs)
- Cliente mal configurado con retry loop agresivo

## Mecanismo de detección

LogQL: `sum by (remote_addr) (rate({service="nginx", log_type="access"}[1m])) > 200/60`
