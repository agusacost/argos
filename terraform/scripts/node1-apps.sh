#!/usr/bin/env bash
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive

log() {
  echo "[node1] $*"
}

if [[ "$(id -u)" -ne 0 ]]; then
  log "Este script debe ejecutarse como root."
  exit 1
fi

STACK_DIR="${STACK_DIR:-/opt/docker/apps}"

if [[ ! -d "$STACK_DIR" ]]; then
  log "No existe $STACK_DIR. Nada que levantar."
  exit 0
fi

if [[ ! -f "$STACK_DIR/docker-compose.yml" ]] && [[ ! -f "$STACK_DIR/compose.yml" ]]; then
  log "No se encontró docker-compose.yml/compose.yml en $STACK_DIR. Nada que levantar."
  exit 0
fi

log "Levantando stack de aplicaciones (SQL Server + API desde Docker Hub)..."
cd "$STACK_DIR"

docker compose -p apps pull
docker compose -p apps up -d

log "Nodo 1 listo. API :5000 · SQL Server (puerto 1433 interno Docker)"
