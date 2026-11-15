# Idéntico en environments/localstack y environments/aws: la infraestructura vive en el módulo;
# cada entorno solo aporta su provider (providers.tf) y sus valores (terraform.tfvars).
module "argos_node" {
  source = "../../modules/argos-node"

  name                = var.name
  instance_type       = var.instance_type
  root_volume_size_gb = var.root_volume_size_gb
  ami_id              = var.ami_id
  admin_cidr_blocks   = var.admin_cidr_blocks
  ssh_public_key      = var.ssh_public_key
  associate_eip       = var.associate_eip
  repo_url            = var.repo_url
  repo_ref            = var.repo_ref
  argos_env           = var.argos_env
}
