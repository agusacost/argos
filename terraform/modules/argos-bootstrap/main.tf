# ============================================================
# Módulo argos-bootstrap: arranque de un nodo Argos, independiente del proveedor de nube.
# Lo consumen argos-node (AWS / LocalStack) y argos-node-gcp. Genera el cloud-init del primer arranque:
#   01-base.sh   → Docker Engine + Compose plugin + git (scripts/base.sh)
#   02-deploy.sh → /etc/argos/argos.env + argos-deploy (clona el repo,
#                  crea argos-net, docker compose up -d --build) +
#                  import del workflow de n8n
# Requiere una imagen Ubuntu con cloud-init (las oficiales de Canonical en AWS y en GCP lo traen).
# ============================================================

# Secretos internos de la plataforma: si no vienen en argos_env se generan,
# así nunca se despliegan los defaults de desarrollo del docker-compose.
resource "random_password" "n8n_encryption_key" {
  length  = 64
  special = false
}

resource "random_password" "jwt_secret" {
  length  = 48
  special = false
}

resource "random_password" "internal_token" {
  length  = 40
  special = false
}

resource "random_password" "admin_password" {
  length  = 20
  special = false
}

locals {
  app_env = merge(
    {
      N8N_ENCRYPTION_KEY   = random_password.n8n_encryption_key.result
      JWT_SECRET           = random_password.jwt_secret.result
      ARGOS_INTERNAL_TOKEN = random_password.internal_token.result
      ARGOS_ADMIN_PASSWORD = random_password.admin_password.result
    },
    var.argos_env,
  )

  # Comillas simples: Compose no interpola '$' dentro de ellas (la validación de argos_env prohíbe "'").
  env_file = join("\n", concat([for k in sort(keys(local.app_env)) : "${k}='${local.app_env[k]}'"], [""]))
}

# Sin codificar: AWS lo recibe en base64 (output user_data_base64) y GCP como texto en el metadato user-data.
# replace(CRLF → LF): con core.autocrlf=true (Windows) los scripts salen con CRLF y bash fallaría en la VM.
data "cloudinit_config" "bootstrap" {
  gzip          = false
  base64_encode = false

  part {
    filename     = "01-base.sh"
    content_type = "text/x-shellscript"
    content      = replace(file("${path.module}/scripts/base.sh"), "\r\n", "\n")
  }

  part {
    filename     = "02-deploy.sh"
    content_type = "text/x-shellscript"
    content = replace(templatefile("${path.module}/templates/deploy.sh.tftpl", {
      repo_url        = var.repo_url
      repo_ref        = var.repo_ref
      compose_project = var.compose_project
      env_file_b64    = base64encode(local.env_file)
    }), "\r\n", "\n")
  }
}
