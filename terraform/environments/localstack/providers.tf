# Entorno LocalStack (AWS simulado). La infraestructura difiere de environments/aws solo en este bloque
# de provider (endpoints locales + credenciales de prueba) y en los valores de terraform.tfvars.
# localstack.tf agrega extras que no son infraestructura (migración de state y despliegue local opcional).

terraform {
  required_version = ">= 1.5.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
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

variable "localstack_endpoint" {
  description = "URL del gateway de LocalStack."
  type        = string
  default     = "http://localhost:4566"
}

provider "aws" {
  region = var.aws_region

  # Credenciales de prueba: LocalStack acepta cualquier valor.
  access_key = "test"
  secret_key = "test"

  skip_credentials_validation = true
  skip_metadata_api_check     = true
  skip_requesting_account_id  = true

  endpoints {
    ec2 = var.localstack_endpoint
    sts = var.localstack_endpoint
  }

  default_tags {
    tags = {
      Project     = "argos"
      Environment = "localstack"
    }
  }
}
