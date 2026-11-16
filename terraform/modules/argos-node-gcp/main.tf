# ============================================================
# Módulo argos-node-gcp: un nodo de Compute Engine con la plataforma
# Argos completa (monitoring + apps + proxy vía el docker-compose.yml raíz).
# Espejo de argos-node (AWS): misma superficie de red, mismo tamaño de
# disco y el mismo arranque (módulo argos-bootstrap). Cambia solo la
# capa de red y de máquina virtual, que es propia de cada proveedor.
# ============================================================

locals {
  labels = merge({ project = "argos", managed_by = "terraform" }, var.labels)

  node_tag = "${var.name}-node"

  # SSH solo tiene sentido si hay clave pública.
  admin_ports = concat(["3001", "3000"], var.ssh_public_key == null ? [] : ["22"])

  metadata = merge(
    {
      # cloud-init de Ubuntu lee este metadato en el primer arranque (MIME multipart, sin codificar).
      "user-data" = module.bootstrap.user_data
      # Solo las claves de esta instancia: ni las del proyecto ni OS Login.
      "block-project-ssh-keys" = "true"
      "enable-oslogin"         = "FALSE"
    },
    var.ssh_public_key == null ? {} : { "ssh-keys" = "${var.ssh_user}:${trimspace(var.ssh_public_key)}" },
  )
}

# ─── Arranque (compartido con el módulo de AWS) ───────────────

module "bootstrap" {
  source = "../argos-bootstrap"

  repo_url        = var.repo_url
  repo_ref        = var.repo_ref
  compose_project = var.compose_project
  argos_env       = var.argos_env
}

# ─── Red: VPC dedicada con una subred ─────────────────────────
# Dedicada (no la default) para que el despliegue sea reproducible
# y quede aislado de otros recursos del proyecto.

resource "google_compute_network" "this" {
  name                    = "${var.name}-vpc"
  auto_create_subnetworks = false
}

resource "google_compute_subnetwork" "public" {
  name          = "${var.name}-public"
  network       = google_compute_network.this.id
  region        = var.region
  ip_cidr_range = var.subnet_cidr
}

# ─── Firewall: superficie mínima ──────────────────────────────
# Una VPC propia de GCP no admite ninguna entrada hasta que una regla la abre.
# Público: solo Nginx (80 y 8443 → 443 del contenedor).
# Admin (CIDR por variable): panel 3001, Grafana 3000 y SSH.
# Prometheus, Loki, Alertmanager, Qdrant, n8n, la API, SQL Server y
# los exporters publican puertos en el host, pero ninguna regla los abre:
# se acceden por túnel SSH (ver README). La salida queda abierta (regla
# implícita): apt, Docker Hub, GitHub, APIs de LLM y SMTP (587).

resource "google_compute_firewall" "public" {
  name        = "${var.name}-public"
  description = "Argos: Nginx publico (80 y 8443)"
  network     = google_compute_network.this.id
  direction   = "INGRESS"

  allow {
    protocol = "tcp"
    ports    = ["80", "8443"]
  }

  source_ranges = ["0.0.0.0/0"]
  target_tags   = [local.node_tag]
}

resource "google_compute_firewall" "admin" {
  name        = "${var.name}-admin"
  description = "Argos: panel, Grafana y SSH solo desde el CIDR de administracion"
  network     = google_compute_network.this.id
  direction   = "INGRESS"

  allow {
    protocol = "tcp"
    ports    = local.admin_ports
  }

  source_ranges = var.admin_cidr_blocks
  target_tags   = [local.node_tag]
}

# ─── Instancia ────────────────────────────────────────────────

data "google_compute_image" "ubuntu" {
  count = var.image == null ? 1 : 0

  family  = "ubuntu-2404-lts-amd64"
  project = "ubuntu-os-cloud"
}

resource "google_compute_address" "this" {
  count = var.reserve_static_ip ? 1 : 0

  name   = "${var.name}-ip"
  region = var.region
}

resource "google_compute_instance" "this" {
  name         = "${var.name}-node"
  machine_type = var.machine_type
  zone         = var.zone
  tags         = [local.node_tag]
  labels       = local.labels

  boot_disk {
    auto_delete = true

    initialize_params {
      image  = var.image != null ? var.image : data.google_compute_image.ubuntu[0].self_link
      size   = var.boot_disk_size_gb
      type   = "pd-balanced"
      labels = local.labels
    }
  }

  network_interface {
    subnetwork = google_compute_subnetwork.public.id

    # IP pública: la reservada si reserve_static_ip = true, o una efímera.
    access_config {
      nat_ip = one(google_compute_address.this[*].address)
    }
  }

  # El metadato user-data contiene secretos (el .env): solo lo lee la propia instancia y quien
  # tenga permiso de lectura de Compute en el proyecto.
  metadata = local.metadata

  # Sin cuenta de servicio: la VM no llama a ninguna API de Google Cloud.

  shielded_instance_config {
    enable_secure_boot          = true
    enable_vtpm                 = true
    enable_integrity_monitoring = true
  }

  lifecycle {
    # Una imagen nueva de Canonical no debe recrear la VM (se perderían los volúmenes Docker).
    # El user-data corre solo en el primer arranque; para re-aprovisionar:
    # terraform apply -replace=module.argos_node.google_compute_instance.this. Para actualizar el
    # código: sudo argos-deploy en la VM.
    ignore_changes = [boot_disk[0].initialize_params[0].image]
  }

  depends_on = [google_compute_firewall.public, google_compute_firewall.admin]
}
