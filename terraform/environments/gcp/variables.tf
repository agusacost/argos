# Ver la descripción completa de cada variable en modules/argos-node-gcp.

variable "project_id" {
  description = "ID del proyecto de Google Cloud donde se crea todo."
  type        = string
}

variable "region" {
  description = "Región de la subred y de la IP reservada."
  type        = string
  default     = "us-central1"
}

variable "zone" {
  description = "Zona de la instancia (dentro de region)."
  type        = string
  default     = "us-central1-a"
}

variable "name" {
  description = "Prefijo de nombre de los recursos."
  type        = string
  default     = "argos"
}

variable "machine_type" {
  description = "Tipo de máquina (x86_64). Default e2-standard-4: 4 vCPU / 16 GB, ver el módulo."
  type        = string
  default     = "e2-standard-4"
}

variable "boot_disk_size_gb" {
  description = "Disco de arranque pd-balanced en GB."
  type        = number
  default     = 50
}

variable "image" {
  description = "Imagen explícita. null = Ubuntu 24.04 LTS de Canonical."
  type        = string
  default     = null
}

variable "admin_cidr_blocks" {
  description = "CIDRs que acceden al panel (3001), Grafana (3000) y SSH (22). Ej. [\"203.0.113.10/32\"]."
  type        = list(string)
}

variable "ssh_public_key" {
  description = "Clave pública SSH (contenido). null = sin SSH."
  type        = string
  default     = null
}

variable "reserve_static_ip" {
  description = "Reservar una IP externa fija."
  type        = bool
  default     = true
}

variable "repo_url" {
  description = "Repositorio que clona la VM (HTTPS público)."
  type        = string
  default     = "https://github.com/agusacost/argos.git"
}

variable "repo_ref" {
  description = "Rama, tag o commit a desplegar."
  type        = string
  default     = "master"
}

variable "argos_env" {
  description = "Variables del .env de la plataforma (API keys, SMTP, etc.). Ver terraform.tfvars.example."
  type        = map(string)
  default     = {}
  sensitive   = true
}
