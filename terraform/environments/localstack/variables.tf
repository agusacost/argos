variable "instance_type" {
  description = "Tipo de instancia EC2 (simulada en LocalStack)"
  default     = "t3.medium"
}

variable "project_root" {
  description = "Ruta absoluta al root del proyecto (donde están los directorios docker/)"
  default     = "D:/DEV/argos"
}

variable "provision_docker" {
  description = "Si es true, el provisioner levanta los stacks Docker vía Terraform. Por defecto false: el despliegue se hace manualmente con docker compose desde la raíz del proyecto."
  type        = bool
  default     = false
}
