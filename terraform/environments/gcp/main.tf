# La infraestructura vive en el módulo; este entorno solo aporta el provider (providers.tf)
# y los valores (terraform.tfvars).
module "argos_node" {
  source = "../../modules/argos-node-gcp"

  name              = var.name
  region            = var.region
  zone              = var.zone
  machine_type      = var.machine_type
  boot_disk_size_gb = var.boot_disk_size_gb
  image             = var.image
  admin_cidr_blocks = var.admin_cidr_blocks
  ssh_public_key    = var.ssh_public_key
  reserve_static_ip = var.reserve_static_ip
  repo_url          = var.repo_url
  repo_ref          = var.repo_ref
  argos_env         = var.argos_env
}
