# ============================================================
# Módulo argos-node: un nodo EC2 con la plataforma Argos completa
# (monitoring + apps + proxy vía el docker-compose.yml raíz).
# Lo consumen environments/localstack y environments/aws: la
# diferencia entre ambos es solo el provider y los valores.
# El arranque (cloud-init) sale del módulo argos-bootstrap, que
# comparte con argos-node-gcp.
# ============================================================

locals {
  tags = merge({ Project = "argos", ManagedBy = "terraform" }, var.tags)

  # SSH solo tiene sentido si hay key pair.
  admin_ports = concat([3001, 3000], var.ssh_public_key == null ? [] : [22])

  ami_id = var.ami_id != null ? var.ami_id : data.aws_ami.ubuntu[0].id
}

# ─── Red: VPC dedicada con una subred pública ─────────────────
# Dedicada (no la default) para que el despliegue sea reproducible
# en cuentas sin VPC por defecto y quede aislado de otros recursos.

data "aws_availability_zones" "available" {
  state = "available"
}

resource "aws_vpc" "this" {
  cidr_block           = var.vpc_cidr
  enable_dns_support   = true
  enable_dns_hostnames = true

  tags = merge(local.tags, { Name = "${var.name}-vpc" })
}

resource "aws_internet_gateway" "this" {
  vpc_id = aws_vpc.this.id

  tags = merge(local.tags, { Name = "${var.name}-igw" })
}

resource "aws_subnet" "public" {
  vpc_id                  = aws_vpc.this.id
  cidr_block              = var.subnet_cidr
  availability_zone       = data.aws_availability_zones.available.names[0]
  map_public_ip_on_launch = true

  tags = merge(local.tags, { Name = "${var.name}-public" })
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.this.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.this.id
  }

  tags = merge(local.tags, { Name = "${var.name}-public" })
}

resource "aws_route_table_association" "public" {
  subnet_id      = aws_subnet.public.id
  route_table_id = aws_route_table.public.id
}

# ─── Security group: superficie mínima ────────────────────────
# Público: solo Nginx (80 y 8443 → 443 del contenedor).
# Admin (CIDR por variable): panel 3001, Grafana 3000 y SSH.
# Prometheus, Loki, Alertmanager, Qdrant, n8n, la API, SQL Server y
# los exporters publican puertos en el host, pero el SG no los abre:
# se acceden por túnel SSH (ver README).

resource "aws_security_group" "node" {
  name        = "${var.name}-node"
  description = "Argos: Nginx publico; panel, Grafana y SSH solo desde CIDR de administracion"
  vpc_id      = aws_vpc.this.id

  ingress {
    description = "Nginx HTTP"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "Nginx HTTPS (8443 en el host)"
    from_port   = 8443
    to_port     = 8443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  dynamic "ingress" {
    for_each = local.admin_ports
    content {
      description = "Admin ${ingress.value}"
      from_port   = ingress.value
      to_port     = ingress.value
      protocol    = "tcp"
      cidr_blocks = var.admin_cidr_blocks
    }
  }

  # Salida abierta: apt, Docker Hub, GitHub, APIs de LLM y SMTP (587).
  egress {
    description = "Salida a Internet"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(local.tags, { Name = "${var.name}-node" })
}

# ─── Instancia ────────────────────────────────────────────────

data "aws_ami" "ubuntu" {
  count = var.ami_id == null ? 1 : 0

  most_recent = true
  owners      = ["099720109477"] # Canonical

  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd-gp3/ubuntu-noble-24.04-amd64-server-*"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

resource "aws_key_pair" "this" {
  count = var.ssh_public_key == null ? 0 : 1

  key_name   = "${var.name}-admin"
  public_key = var.ssh_public_key

  tags = local.tags
}

resource "aws_instance" "this" {
  ami                    = local.ami_id
  instance_type          = var.instance_type
  subnet_id              = aws_subnet.public.id
  vpc_security_group_ids = [aws_security_group.node.id]
  key_name               = one(aws_key_pair.this[*].key_name)

  user_data_base64 = module.bootstrap.user_data_base64

  # IMDSv2 obligatorio: el user_data contiene secretos (el .env).
  metadata_options {
    http_endpoint = "enabled"
    http_tokens   = "required"
  }

  root_block_device {
    volume_size           = var.root_volume_size_gb
    volume_type           = "gp3"
    encrypted             = true
    delete_on_termination = true
  }

  lifecycle {
    # Una AMI nueva de Canonical o un cambio del .env no deben recrear la VM (se perderían
    # los volúmenes Docker). El user_data corre solo en el primer arranque; para re-aprovisionar:
    # terraform apply -replace=module.argos_node.aws_instance.this. Para actualizar el código:
    # sudo argos-deploy en la VM.
    ignore_changes = [ami, user_data_base64]
  }

  tags = merge(local.tags, { Name = "${var.name}-node" })
}

resource "aws_eip" "this" {
  count = var.associate_eip ? 1 : 0

  domain   = "vpc"
  instance = aws_instance.this.id

  tags = merge(local.tags, { Name = "${var.name}-eip" })

  depends_on = [aws_internet_gateway.this]
}
