variable "name" {
  description = "Prefijo de nombre para todos los recursos (red, firewall, instancia). Minúsculas, números y guiones."
  type        = string
  default     = "argos"

  validation {
    condition     = can(regex("^[a-z]([a-z0-9-]{0,40}[a-z0-9])?$", var.name))
    error_message = "name: empieza con letra minúscula y solo admite minúsculas, números y '-' (regla de nombres de GCP)."
  }
}

# ─── Ubicación y red ──────────────────────────────────────────────────────────

variable "region" {
  description = "Región de la subred y de la IP reservada."
  type        = string
}

variable "zone" {
  description = "Zona de la instancia (debe pertenecer a region)."
  type        = string
}

variable "subnet_cidr" {
  description = "CIDR de la subred del nodo."
  type        = string
  default     = "10.20.1.0/24"
}

variable "admin_cidr_blocks" {
  description = <<-EOT
    CIDRs de administración: los únicos orígenes que llegan al panel (3001), a Grafana (3000) y a SSH (22).
    Usar la IP pública propia con /32 (ej. "203.0.113.10/32"). 80 y 8443 (Nginx) son públicos siempre.
  EOT
  type        = list(string)

  validation {
    condition     = length(var.admin_cidr_blocks) > 0 && alltrue([for c in var.admin_cidr_blocks : can(cidrhost(c, 0))])
    error_message = "admin_cidr_blocks debe tener al menos un CIDR válido (ej. \"203.0.113.10/32\")."
  }
}

# ─── Instancia ────────────────────────────────────────────────────────────────

variable "machine_type" {
  description = <<-EOT
    Tipo de máquina. Default e2-standard-4 (4 vCPU, 16 GB). La propuesta dimensiona ~7 vCPU / 10 GB sumando los
    límites de cada contenedor, pero el consumo real es mayormente ocioso y la restricción dura es la memoria.
    e2-standard-2 (2 vCPU, 8 GB) solo si la medición con docker stats deja margen. El nivel gratuito (e2-micro,
    1 GB) no alcanza: SQL Server 2022 exige 2 GB él solo. Debe ser x86_64: su imagen es solo amd64.
  EOT
  type        = string
  default     = "e2-standard-4"
}

variable "boot_disk_size_gb" {
  description = "Tamaño del disco de arranque (pd-balanced). La propuesta pide ~50 GB: imágenes Docker (~15 GB) + volúmenes de datos."
  type        = number
  default     = 50

  validation {
    condition     = var.boot_disk_size_gb >= 30
    error_message = "Con menos de 30 GB no entran las imágenes del stack (SQL Server, .NET SDK, Node, Loki, etc.)."
  }
}

variable "image" {
  description = "Imagen de arranque explícita (self_link o proyecto/familia). null = la Ubuntu Server 24.04 LTS amd64 más reciente de Canonical."
  type        = string
  default     = null
}

variable "ssh_public_key" {
  description = "Clave pública SSH (contenido, ej. \"ssh-ed25519 AAAA...\"). null = sin clave y sin regla de SSH."
  type        = string
  default     = null
}

variable "ssh_user" {
  description = "Usuario Linux al que se asocia ssh_public_key."
  type        = string
  default     = "ubuntu"
}

variable "reserve_static_ip" {
  description = "Reservar una IP externa fija para que sobreviva a stop/start de la instancia. Una IP reservada sin usar se cobra: destruirla junto con la VM."
  type        = bool
  default     = true
}

# ─── Aplicación (se pasan tal cual al módulo argos-bootstrap, que las valida) ──

variable "repo_url" {
  description = "URL HTTPS del repositorio que clona la instancia (debe ser accesible sin credenciales)."
  type        = string
  default     = "https://github.com/agusacost/argos.git"
}

variable "repo_ref" {
  description = "Rama, tag o commit a desplegar."
  type        = string
  default     = "master"
}

variable "compose_project" {
  description = "Nombre de proyecto de Docker Compose (el mismo que en el despliegue local)."
  type        = string
  default     = "argos-platform"
}

variable "argos_env" {
  description = "Variables que se escriben en docker/monitoring/.env en la instancia. Ver el módulo argos-bootstrap."
  type        = map(string)
  default     = {}
  sensitive   = true
}

variable "labels" {
  description = "Labels adicionales para la instancia y su disco (minúsculas, números, '_' y '-')."
  type        = map(string)
  default     = {}
}
