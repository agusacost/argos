# Extras propios del entorno LocalStack (no son infraestructura: no existen en environments/aws).

# 1) Migración del state previo: la instancia que antes se declaraba en la raíz de este entorno
#    ahora vive en el módulo. Evita que Terraform la trate como un recurso borrado + uno nuevo.
moved {
  from = aws_instance.observability
  to   = module.argos_node.aws_instance.this
}

# 2) Despliegue local opcional. En LocalStack la instancia es simulada (EC2 mock): el user_data
#    se registra pero no se ejecuta en ninguna VM, así que el `docker compose up` que en AWS hace
#    cloud-init se reproduce acá contra el Docker local, sobre el mismo docker-compose.yml raíz.
#    Por defecto apagado: el despliegue local habitual sigue siendo `docker compose up -d --build`.
#    Usa el docker/monitoring/.env local (no el argos_env de Terraform).
#      terraform apply -var deploy_local_stack=true
variable "deploy_local_stack" {
  description = "Si es true, levanta el stack con el Docker local tras crear la instancia simulada."
  type        = bool
  default     = false
}

resource "terraform_data" "local_stack" {
  count = var.deploy_local_stack ? 1 : 0

  triggers_replace = [module.argos_node.instance_id]

  # Falla si la red ya existe: se ignora (comando válido en cmd y en sh).
  provisioner "local-exec" {
    command    = "docker network create argos-net"
    on_failure = continue
  }

  provisioner "local-exec" {
    command     = "docker compose -p argos-platform up -d --build"
    working_dir = abspath("${path.root}/../../..")
  }
}
