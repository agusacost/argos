# Entorno Google Cloud. Usa el módulo argos-node-gcp: la red y la máquina virtual son propias de
# GCP, y el arranque (cloud-init) es el mismo módulo argos-bootstrap que usan localstack y aws.
#
# Credenciales: `gcloud auth application-default login` (o GOOGLE_APPLICATION_CREDENTIALS apuntando
# a la clave de una cuenta de servicio). Nunca en este archivo.
# Requisito del proyecto: API de Compute Engine habilitada (`gcloud services enable compute.googleapis.com`).
#
# State: local por defecto. Contiene secretos (el .env generado): para uso compartido, migrar a
# un backend GCS (backend "gcs" { bucket, prefix }).

terraform {
  required_version = ">= 1.5.0"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = ">= 6.0, < 9.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
    cloudinit = {
      source  = "hashicorp/cloudinit"
      version = "~> 2.3"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
  zone    = var.zone

  default_labels = {
    project     = "argos"
    environment = "gcp"
  }
}
