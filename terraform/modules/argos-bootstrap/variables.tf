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
