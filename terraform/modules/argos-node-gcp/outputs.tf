locals {
  public_ip = google_compute_instance.this.network_interface[0].access_config[0].nat_ip
}

output "instance_id" {
  description = "ID de la instancia de Compute Engine."
  value       = google_compute_instance.this.instance_id
}

output "image" {
  description = "Imagen de arranque efectivamente usada."
  value       = google_compute_instance.this.boot_disk[0].initialize_params[0].image
}

output "public_ip" {
  description = "IP pública del nodo (reservada si reserve_static_ip = true)."
  value       = local.public_ip
}

output "private_ip" {
  description = "IP privada del nodo dentro de la subred."
  value       = google_compute_instance.this.network_interface[0].network_ip
}

output "network_id" {
  value = google_compute_network.this.id
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
  value       = var.ssh_public_key == null ? null : "ssh -L 5678:localhost:5678 -L 9090:localhost:9090 ${var.ssh_user}@${local.public_ip}"
}

output "argos_admin_password" {
  description = "Contraseña del usuario admin@argos.local del panel (generada si no se pasó en argos_env)."
  value       = module.bootstrap.admin_password
  sensitive   = true
}
