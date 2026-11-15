locals {
  public_ip = var.associate_eip ? aws_eip.this[0].public_ip : aws_instance.this.public_ip
}

output "instance_id" {
  description = "ID de la instancia EC2."
  value       = aws_instance.this.id
}

output "ami_id" {
  description = "AMI efectivamente usada."
  value       = aws_instance.this.ami
}

output "public_ip" {
  description = "IP pública del nodo (Elastic IP si associate_eip = true)."
  value       = local.public_ip
}

output "private_ip" {
  description = "IP privada del nodo dentro de la VPC."
  value       = aws_instance.this.private_ip
}

output "vpc_id" {
  value = aws_vpc.this.id
}

output "security_group_id" {
  value = aws_security_group.node.id
}

output "urls" {
  description = "Accesos. Panel y Grafana solo desde admin_cidr_blocks; el resto por túnel SSH."
  value = {
    nginx_http  = "http://${local.public_ip}"
    nginx_https = "https://${local.public_ip}:8443"
    dashboard   = "http://${local.public_ip}:3001"
    grafana     = "http://${local.public_ip}:3000"
  }
}

output "ssh_command" {
  description = "SSH con túnel a n8n (5678) y Prometheus (9090). null si no se cargó ssh_public_key."
  value       = var.ssh_public_key == null ? null : "ssh -L 5678:localhost:5678 -L 9090:localhost:9090 ubuntu@${local.public_ip}"
}

output "argos_admin_password" {
  description = "Contraseña del usuario admin@argos.local del panel (generada si no se pasó en argos_env)."
  value       = local.app_env["ARGOS_ADMIN_PASSWORD"]
  sensitive   = true
}
