# Entorno AWS real. La infraestructura difiere de environments/localstack solo en este bloque de
# provider (credenciales por la cadena estándar de AWS) y en los valores de terraform.tfvars.
#
# Credenciales: AWS_PROFILE=<perfil> o AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY en el entorno.
# Nunca en este archivo.
#
# State: local por defecto. Contiene secretos (el .env generado): para uso compartido, migrar a
# un backend S3 cifrado (backend "s3" { bucket, key, region, encrypt = true, use_lockfile = true }).

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

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "argos"
      Environment = "aws"
    }
  }
}
