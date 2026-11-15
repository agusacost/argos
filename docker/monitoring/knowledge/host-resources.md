---
tipo_alerta: HostHighCPU
resumen: "Uso de CPU del host superior al 80% durante más de 5 minutos, con riesgo de degradación del servicio"
pasos:
  - "Identificar el proceso/contenedor con mayor consumo: docker stats --no-stream"
  - "Ver top de procesos del host: docker exec cadvisor cat /proc/loadavg (si cAdvisor está up)"
  - "Si el culpable es membresia-api: revisar si hay burst de requests o query SQL costosa"
  - "Si el culpable es prometheus: puede estar reteniendo demasiado datos (verificar retention)"
  - "Limitar CPU del contenedor sospechoso en docker-compose: agregar deploy.resources.limits.cpus"
  - "Si la carga es sostenida y legítima: considerar escalar verticalmente el host o hacer sharding"
  - "Para diagnóstico profundo: habilitar profiling de .NET (dotnet-trace collect) en la API"
  - "Revisar si coincide con algún cron job o scrape interval muy agresivo en Prometheus"
estado: documentado_previamente
fuente: manual
---

## Contexto

La alerta `HostHighCPU` cubre los casos de alta CPU del host (>80% durante 5min) y crítica
(>95% durante 2min). En el contexto de Argos, los contenedores que más CPU consumen suelen ser
la API .NET y Prometheus (especialmente durante scrape + evaluate de alertas).

## Alertas relacionadas

- `HostCriticalCPU`: escalado de HostHighCPU, umbral 95% durante 2min
- `HostHighMemory`: puede co-ocurrir (memory pressure → swap → CPU)
- `HostHighDisk`: si el disco está lleno, writes de logs causan CPU wait

## Causas comunes

- Query SQL sin índice ejecutada bajo carga (full scan en loop)
- Prometheus scrape interval muy bajo (<10s) con muchas métricas
- Burst de requests durante test de carga k6
- GC de .NET bajo memory pressure (muchos GC cycles = CPU)

## Alertas de disco y memoria

Si el tipo de alerta es `HostHighMemory` o `HostHighDisk`, los pasos adicionales son:
- **Memoria:** `docker stats --no-stream` → identificar contenedor. Agregar `mem_limit` en compose.
- **Disco:** `df -h` en el host → identificar qué llena el disco (generalmente logs o volúmenes Docker).
  Rotar logs: `docker system prune --volumes` (con cuidado, borra volúmenes sin uso).
