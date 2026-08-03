#!/usr/bin/env bash
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive

log() {
  echo "[node2] $*"
}

if [[ "$(id -u)" -ne 0 ]]; then
  log "Este script debe ejecutarse como root."
  exit 1
fi

STACK_DIR="${STACK_DIR:-/opt/docker/proxy}"

if [[ ! -d "$STACK_DIR" ]]; then
  log "No existe $STACK_DIR. Nada que levantar."
  exit 0
fi

if [[ ! -f "$STACK_DIR/docker-compose.yml" ]] && [[ ! -f "$STACK_DIR/compose.yml" ]]; then
  log "No se encontró docker-compose.yml/compose.yml en $STACK_DIR. Nada que levantar."
  exit 0
fi

log "Levantando stack reverse proxy + exporters + Alloy..."
cd "$STACK_DIR"

docker compose -p proxy build
docker compose -p proxy pull || true
docker compose -p proxy up -d

log "Nodo 2 listo. HTTP/HTTPS en :80/:443 (cert autofirmado). Métricas: :9100 :8080 :9113. Alloy :12345 → Loki en 192.168.56.12:3100"
