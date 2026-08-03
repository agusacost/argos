output "instance_id" {
  value = aws_instance.observability.id
}

output "instance_private_ip" {
  value = aws_instance.observability.private_ip
}

output "grafana_url" {
  value = "http://localhost:3000"
}

output "prometheus_url" {
  value = "http://localhost:9090"
}

output "loki_url" {
  value = "http://localhost:3100"
}
