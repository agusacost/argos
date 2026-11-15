# Idéntico en environments/localstack y environments/aws (ver la descripción completa en el módulo).

variable "aws_region" {
  description = "Región de AWS (o la región simulada por LocalStack)."
  type        = string
  default     = "us-east-1"
}

variable "name" {
  description = "Prefijo de nombre de los recursos."
  type        = string
  default     = "argos"
}

variable "instance_type" {
  description = "Tipo de instancia (x86_64). Default t3.xlarge: 4 vCPU / 16 GiB, ver el módulo."
  type        = string
  default     = "t3.xlarge"
}

variable "root_volume_size_gb" {
  description = "Disco raíz gp3 en GB."
  type        = number
  default     = 50
}

variable "ami_id" {
  description = "AMI explícita. null = Ubuntu 24.04 LTS de Canonical (búsqueda en el proveedor)."
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

variable "associate_eip" {
  description = "Asignar Elastic IP."
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
