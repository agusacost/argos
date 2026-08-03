variable "instance_type" {
  description = "Tipo de instancia EC2 (simulada en LocalStack)"
  default     = "t3.medium"
}

variable "project_root" {
  description = "Ruta absoluta al root del proyecto (donde están los directorios docker/)"
  default     = "D:/DEV/argos"
}
