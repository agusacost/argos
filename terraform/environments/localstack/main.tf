# Registra el AMI en LocalStack si no existe, y devuelve su ID
data "external" "localstack_ami" {
  program = ["powershell", "-ExecutionPolicy", "Bypass", "-File", "${path.module}/scripts/get_or_create_ami.ps1"]
}

resource "aws_instance" "observability" {
  ami           = data.external.localstack_ami.result.ami_id
  instance_type = var.instance_type

  tags = {
    Name    = "argos-node"
    Project = "argos"
  }
}

# Despliegue Docker desacoplado — por defecto desactivado (provision_docker = false).
# El deploy se hace manualmente desde la raíz:
#   docker network create argos-net
#   docker compose up -d --build
#
# Para usar este provisioner: terraform apply -var provision_docker=true
resource "null_resource" "provision" {
  count      = var.provision_docker ? 1 : 0
  depends_on = [aws_instance.observability]

  provisioner "local-exec" {
    interpreter = ["powershell", "-ExecutionPolicy", "Bypass", "-Command"]
    command     = <<-EOT
      docker network create argos-net 2>$null

      Set-Location "${var.project_root}/docker/monitoring"
      docker compose -p argos-monitoring up -d
      Start-Sleep 10

      Set-Location "${var.project_root}/docker/apps"
      docker compose -p argos-apps up -d

      Set-Location "${var.project_root}/docker/proxy"
      docker compose -p argos-proxy build --quiet
      docker compose -p argos-proxy up -d

      docker ps --format "table {{.Names}}`t{{.Status}}`t{{.Ports}}"
    EOT
  }

  triggers = {
    instance_id = aws_instance.observability.id
  }
}
