---
tipo_alerta: NginxErrorLogSpike
resumen: "Spike de errores en error.log de Nginx: tasa superior a 1 error por segundo"
pasos:
  - "Leer el error.log reciente: docker exec nginx-proxy tail -n 50 /var/log/nginx/error.log"
  - "En Loki, filtrar errores: {service='nginx', log_type='error'} | json | level='error'"
  - "Clasificar el tipo de error: upstream (conectividad API), SSL, permisos, configuración"
  - "Si son errores 'connect() failed': la API no está aceptando conexiones → reiniciar API"
  - "Si son errores SSL: verificar certificados (vencimiento, permisos del archivo .pem)"
  - "Si son errores de permisos: revisar ownership de /var/log/nginx y /etc/nginx"
  - "Reload de configuración si se hizo un cambio reciente: docker exec nginx-proxy nginx -t && nginx -s reload"
  - "Revisar si el error.log está creciendo descontroladamente (I/O del host)"
estado: documentado_previamente
fuente: manual
---

## Contexto

La alerta `NginxErrorLogSpike` monitorea el archivo error.log (a diferencia de `NginxHighErrorRate`
que monitorea status codes en access.log). Un spike en error.log indica problemas de configuración
o conectividad, no necesariamente errores HTTP visibles al cliente.

## Causas comunes

- API caída → upstream connection refused (cada request genera un error)
- Cambio de configuración inválido aplicado con reload
- Problemas de permisos en archivos de Nginx
- Certificados SSL vencidos o malformados

## Diferencia con NginxHighErrorRate

`NginxHighErrorRate`: errores percibidos por el cliente (5xx en access.log)
`NginxErrorLogSpike`: errores internos de Nginx (pueden no generar 5xx si el fallback funciona)
