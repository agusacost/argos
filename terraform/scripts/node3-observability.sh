#!/usr/bin/env bash
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive

log() {
  echo "[node3] $*"
}

if [[ "$(id -u)" -ne 0 ]]; then
  log "Este script debe ejecutarse como root."
  exit 1
fi

STACK_DIR="${STACK_DIR:-/opt/docker/monitoring}"

if [[ ! -d "$STACK_DIR" ]]; then
  log "No existe $STACK_DIR. Nada que levantar."
  exit 0
fi

if [[ ! -f "$STACK_DIR/docker-compose.yml" ]] && [[ ! -f "$STACK_DIR/compose.yml" ]]; then
  log "No se encontró docker-compose.yml/compose.yml en $STACK_DIR. Nada que levantar."
  exit 0
fi

log "Levantando stack de observabilidad con Docker Compose..."
cd "$STACK_DIR"

# Ejecutar como root evita problemas de grupo docker en la primera sesión.
docker compose -p observability pull || true
docker compose -p observability up -d

log "Stack levantado. Grafana debería estar en http://192.168.56.12:3000 y Prometheus en http://192.168.56.12:9090"
