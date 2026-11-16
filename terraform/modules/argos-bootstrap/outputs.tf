# El cloud-init contiene el .env de la plataforma: las dos salidas son sensibles.

output "user_data" {
  description = "cloud-init (MIME multipart) en texto: para el metadato user-data de GCP."
  value       = data.cloudinit_config.bootstrap.rendered
  sensitive   = true
}

output "user_data_base64" {
  description = "El mismo cloud-init en base64: para user_data_base64 de una instancia EC2."
  value       = base64encode(data.cloudinit_config.bootstrap.rendered)
  sensitive   = true
}

output "admin_password" {
  description = "Contraseña del usuario admin@argos.local del panel (generada si no se pasó en argos_env)."
  value       = local.app_env["ARGOS_ADMIN_PASSWORD"]
  sensitive   = true
}
