#!/usr/bin/env bash
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive

log() {
  echo "[base] $*"
}

if [[ "$(id -u)" -ne 0 ]]; then
  log "Este script debe ejecutarse como root."
  exit 1
fi

log "Actualizando apt e instalando dependencias..."
apt-get update -y
apt-get install -y --no-install-recommends \
  ca-certificates \
  curl \
  gnupg \
  lsb-release \
  apt-transport-https \
  software-properties-common

log "Configurando repo de Docker (si aplica)..."
install -m 0755 -d /etc/apt/keyrings
if [[ ! -f /etc/apt/keyrings/docker.gpg ]]; then
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg
fi

CODENAME="$(. /etc/os-release && echo "${VERSION_CODENAME}")"
ARCH="$(dpkg --print-architecture)"
DOCKER_LIST="/etc/apt/sources.list.d/docker.list"

if [[ ! -f "$DOCKER_LIST" ]] || ! grep -q "download.docker.com" "$DOCKER_LIST"; then
  echo "deb [arch=${ARCH} signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu ${CODENAME} stable" > "$DOCKER_LIST"
fi

apt-get update -y

log "Instalando Docker Engine + Compose plugin..."
apt-get install -y --no-install-recommends docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

log "Habilitando Docker al arranque..."
systemctl enable --now docker

log "Agregando usuario ubuntu al grupo docker..."
if id -u ubuntu >/dev/null 2>&1; then
  usermod -aG docker ubuntu || true
fi

log "Ajustes básicos (vm.max_map_count) para stacks comunes..."
cat >/etc/sysctl.d/99-observability.conf <<'EOF'
vm.max_map_count=262144
EOF
sysctl --system >/dev/null || true

log "Verificando instalación..."
docker --version
docker compose version

log "Provision base listo."
