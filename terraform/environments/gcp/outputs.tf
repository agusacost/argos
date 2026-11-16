output "instance_id" {
  value = module.argos_node.instance_id
}

output "public_ip" {
  value = module.argos_node.public_ip
}

output "urls" {
  value = module.argos_node.urls
}

output "ssh_command" {
  value = module.argos_node.ssh_command
}

output "argos_admin_password" {
  description = "terraform output -raw argos_admin_password"
  value       = module.argos_node.argos_admin_password
  sensitive   = true
}
