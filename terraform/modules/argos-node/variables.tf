variable "name" {
  description = "Prefijo de nombre para todos los recursos (VPC, security group, instancia)."
  type        = string
  default     = "argos"
}

# ─── Red ──────────────────────────────────────────────────────────────────────

variable "vpc_cidr" {
  description = "CIDR de la VPC dedicada del nodo Argos."
  type        = string
  default     = "10.20.0.0/16"
}

variable "subnet_cidr" {
  description = "CIDR de la subred pública (debe estar dentro de vpc_cidr)."
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

variable "instance_type" {
  description = <<-EOT
    Tipo de instancia EC2. Default t3.xlarge (4 vCPU, 16 GiB): la propuesta dimensiona ~7 vCPU / 10 GB RAM sumando
    los límites de cada contenedor, pero el consumo real es mayormente ocioso y la restricción dura es la memoria
    (SQL Server + API .NET con ONNX + Loki/Prometheus + n8n superan los 8 GiB de un t3.large). t3 es burstable, así
    que los picos de CPU (build inicial, pruebas k6) se absorben con créditos. Para igualar los 7-8 vCPU nominales
    sin burst: t3.2xlarge (8 vCPU, 32 GiB) o m6i.2xlarge. Debe ser x86_64: la imagen de SQL Server 2022 es solo amd64.
  EOT
  type        = string
  default     = "t3.xlarge"
}

variable "root_volume_size_gb" {
  description = "Tamaño del disco raíz (gp3, cifrado). La propuesta pide ~50 GB: imágenes Docker (~15 GB) + volúmenes de datos."
  type        = number
  default     = 50

  validation {
    condition     = var.root_volume_size_gb >= 30
    error_message = "Con menos de 30 GB no entran las imágenes del stack (SQL Server, .NET SDK, Node, Loki, etc.)."
  }
}

variable "ami_id" {
  description = "AMI a usar. null = la Ubuntu Server 24.04 LTS amd64 más reciente publicada por Canonical (búsqueda en el proveedor)."
  type        = string
  default     = null
}

variable "ssh_public_key" {
  description = "Clave pública SSH (contenido, ej. \"ssh-ed25519 AAAA...\"). null = sin key pair y sin regla de SSH."
  type        = string
  default     = null
}

variable "associate_eip" {
  description = "Asignar una Elastic IP para que la IP pública sobreviva a stop/start de la instancia."
  type        = bool
  default     = true
}

# ─── Aplicación ───────────────────────────────────────────────────────────────

variable "repo_url" {
  description = "URL HTTPS del repositorio que clona la instancia (debe ser accesible sin credenciales)."
  type        = string
  default     = "https://github.com/agusacost/argos.git"

  validation {
    condition     = can(regex("^https://[A-Za-z0-9._~:/@%+-]+$", var.repo_url))
    error_message = "repo_url debe ser una URL https:// (la VM no tiene claves SSH de GitHub)."
  }
}

variable "repo_ref" {
  description = "Rama, tag o commit a desplegar."
  type        = string
  default     = "master"

  validation {
    condition     = can(regex("^[A-Za-z0-9._/-]+$", var.repo_ref))
    error_message = "repo_ref solo admite letras, números, '.', '_', '/' y '-'."
  }
}

variable "compose_project" {
  description = "Nombre de proyecto de Docker Compose (el mismo que en el despliegue local)."
  type        = string
  default     = "argos-platform"

  validation {
    condition     = can(regex("^[a-z0-9][a-z0-9_-]*$", var.compose_project))
    error_message = "compose_project: minúsculas, números, '_' y '-'."
  }
}

variable "argos_env" {
  description = <<-EOT
    Variables que se escriben en docker/monitoring/.env en la instancia (mismas claves que .env.example:
    GEMINI_API_KEY, GROQ_API_KEY, SMTP_USER, SMTP_PASSWORD, ALERT_RECIPIENT_EMAIL, etc.).
    N8N_ENCRYPTION_KEY, JWT_SECRET, ARGOS_INTERNAL_TOKEN y ARGOS_ADMIN_PASSWORD se generan al azar si no se pasan.
  EOT
  type        = map(string)
  default     = {}
  sensitive   = true

  validation {
    condition = alltrue([
      for k, v in var.argos_env :
      can(regex("^[A-Z_][A-Z0-9_]*$", k)) && !strcontains(v, "'") && !strcontains(v, "\n")
    ])
    error_message = "argos_env: claves en MAYÚSCULAS_CON_GUION_BAJO y valores sin comilla simple ni saltos de línea."
  }
}

variable "tags" {
  description = "Tags adicionales para todos los recursos."
  type        = map(string)
  default     = {}
}
