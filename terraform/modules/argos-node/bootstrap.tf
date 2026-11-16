# Arranque de la instancia: lo genera el módulo compartido argos-bootstrap (el mismo que usa argos-node-gcp).
module "bootstrap" {
  source = "../argos-bootstrap"

  repo_url        = var.repo_url
  repo_ref        = var.repo_ref
  compose_project = var.compose_project
  argos_env       = var.argos_env
}

# Los secretos vivían en este módulo antes de extraer el arranque: sin estos bloques, un state
# existente los regeneraría (y cambiarían las claves de una plataforma ya desplegada).
moved {
  from = random_password.n8n_encryption_key
  to   = module.bootstrap.random_password.n8n_encryption_key
}

moved {
  from = random_password.jwt_secret
  to   = module.bootstrap.random_password.jwt_secret
}

moved {
  from = random_password.internal_token
  to   = module.bootstrap.random_password.internal_token
}

moved {
  from = random_password.admin_password
  to   = module.bootstrap.random_password.admin_password
}
