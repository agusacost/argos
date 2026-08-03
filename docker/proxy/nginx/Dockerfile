FROM nginx:1.25-alpine

# Instalar OpenSSL para generar certificados SSL
RUN apk add --no-cache openssl

# Crear directorio para certificados SSL
RUN mkdir -p /etc/nginx/ssl

# Generar certificado SSL auto-firmado para api.observabilidad.local
# Incluye SAN (Subject Alternative Name) para compatibilidad moderna
RUN openssl req -x509 -nodes -days 3650 -newkey rsa:2048 \
    -keyout /etc/nginx/ssl/nginx.key \
    -out /etc/nginx/ssl/nginx.crt \
    -subj "/C=AR/ST=BuenosAires/L=BuenosAires/O=InfraObservability/OU=Dev/CN=api.observabilidad.local" \
    -addext "subjectAltName=DNS:api.observabilidad.local,DNS:www.api.observabilidad.local,DNS:localhost,IP:127.0.0.1"

# Crear directorio para logs de Nginx con archivos REALES (no symlinks)
RUN rm -rf /var/log/nginx && \
    mkdir -p /var/log/nginx && \
    chmod 755 /var/log/nginx && \
    touch /var/log/nginx/access.log && \
    touch /var/log/nginx/error.log && \
    chmod 644 /var/log/nginx/*.log && \
    chown -R nginx:nginx /var/log/nginx

# Copiar configuración principal de Nginx con formato JSON
COPY nginx.conf /etc/nginx/nginx.conf

# Eliminar configuración default
RUN rm -f /etc/nginx/conf.d/default.conf

# Copiar configuración del reverse proxy
COPY conf.d/reverse-proxy.conf /etc/nginx/conf.d/reverse-proxy.conf

# Exponer puertos
EXPOSE 80 443

# Iniciar Nginx
CMD ["nginx", "-g", "daemon off;"]
