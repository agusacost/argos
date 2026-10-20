using Argos.Common.Time;
using Argos.Entities;

namespace Argos.Business.Seeding;

/// <summary>
/// Incidentes demo variados (estados, severidades, MTTR, docs) para poblar el dashboard. Los ids uuid5
/// dependen del texto exacto: cambiarlo crea incidentes nuevos en vez de actualizar los existentes.
/// Las fechas son relativas al momento en que se crea el catálogo (una vez por proceso).
/// </summary>
public sealed class DemoIncidentCatalog(IClock clock) : IDemoIncidentCatalog
{
    private readonly DateTime _now = clock.UtcNow.UtcDateTime;

    private const string Doc5xx = """
        ## Diagnóstico: Alta tasa de errores 5xx en Nginx

        **Causa raíz probable:** API .NET no responde o SQL Server con latencia elevada.

        ### Pasos de resolución
        1. `curl http://localhost/api/status` — verificar health de la API.
        2. `docker logs membresia-api --tail=100` — errores de la aplicación.
        3. Verificar SQL Server: `docker exec membresia-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q 'SELECT 1'`
        4. Reiniciar si es necesario: `docker compose -p argos-apps restart api`

        ### Prevención
        - Health check activo en el load balancer.
        - Circuit breaker en el upstream de Nginx.

        """;

    private const string DocCpu = """
        ## Diagnóstico: Alta utilización de CPU

        **Causa raíz probable:** proceso runaway o spike de tráfico HTTP.

        ### Pasos de resolución
        1. Identificar proceso: `top -bn1 | head -20`
        2. Revisar tráfico Nginx: `docker logs nginx-proxy --tail=200`
        3. Revisar API: `docker stats membresia-api --no-stream`
        4. Reiniciar si está degradado: `docker compose -p argos-apps restart api`

        ### Prevención
        - Alertas de CPU > 70% con SLA < 15 min.
        - Revisar índices de SQL Server para queries lentas.

        """;

    private const string DocExporter = """
        ## Diagnóstico: Exporter Down

        node-exporter cayó por falta de recursos. Reinicio inmediato restauró las métricas.

        ### Pasos de resolución
        1. `docker ps | grep exporter` — verificar estado del contenedor.
        2. `docker compose -p argos-proxy restart node-exporter`
        3. Verificar en `http://localhost:9090/targets`.

        ### Prevención
        Monitorear el propio exporter con un watchdog externo o health check periódico.

        """;

    public IReadOnlyList<Incident> GetIncidents() =>
    [
        // Resueltos con MTTR (últimas 2 semanas: dan contenido a métricas y tendencia)
        Create("NginxHighErrorRate",
            "Tasa de errores 5xx superó el 20% durante 8 minutos. API .NET no respondía por reinicio de SQL Server.",
            ["Verificar logs Nginx", "Revisar estado SQL Server", "Reiniciar API"],
            estado: "auto_documentado", fuente: "alertmanager", severity: "critical",
            detectedAt: Ts(days: 12, hours: 2), resolvedAt: Ts(days: 12, hours: 1, minutes: 28),
            mttrMinutes: 32.0, documento: Doc5xx),
        Create("HostCPUUsage",
            "CPU del host superó el 85% durante 12 minutos por spike de tráfico en hora pico.",
            ["Identificar proceso con top", "Revisar tráfico Nginx", "Escalar si es necesario"],
            estado: "auto_documentado", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(days: 10, hours: 9), resolvedAt: Ts(days: 10, hours: 8, minutes: 42),
            mttrMinutes: 18.0, documento: DocCpu),
        Create("NginxHighResponseTime",
            "P95 de latencia upstream superó 2.5s. Causa: query sin índice en MembresiaDB.",
            ["Revisar slow query log", "Agregar índice en tabla Usuarios", "Validar P95 < 500ms"],
            estado: "resuelto", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(days: 9, hours: 14), resolvedAt: Ts(days: 9, hours: 12, minutes: 55),
            mttrMinutes: 55.0),
        Create("HostMemoryUsage",
            "Memoria disponible cayó al 10%. Fuga de memoria detectada en proceso de la API.",
            ["docker stats", "Reiniciar API .NET", "Monitorear durante 30 min"],
            estado: "resuelto", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(days: 8, hours: 3), resolvedAt: Ts(days: 8, hours: 2, minutes: 18),
            mttrMinutes: 42.0),
        Create("NginxSuspiciousIPActivity",
            "IP 198.51.100.42 realizó 350 req/min. Patrón de enumeración de endpoints API.",
            ["Bloquear IP en Nginx", "Revisar access.log", "Activar rate limiting más agresivo"],
            estado: "resuelto", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(days: 7, hours: 22), resolvedAt: Ts(days: 7, hours: 21, minutes: 45),
            mttrMinutes: 15.0),
        Create("NginxDown",
            "Nginx proxy dejó de responder. Proceso terminó inesperadamente por OOM.",
            ["docker ps", "docker compose -p argos-proxy up -d nginx", "Verificar http://localhost"],
            estado: "auto_documentado", fuente: "alertmanager", severity: "critical",
            detectedAt: Ts(days: 6, hours: 5), resolvedAt: Ts(days: 6, hours: 4, minutes: 52),
            mttrMinutes: 8.0,
            documento: "## Diagnóstico: Nginx Down\n\nNginx cayó por OOM. Reinicio inmediato restauró el servicio en 8 minutos.\n\n### Prevención\nConfigurar memory limit en docker-compose y monitorear con cadvisor."),
        Create("HostDiskUsage",
            "Disco del host llegó al 92%. Logs de Loki acumulados sin rotación de archivos.",
            ["df -h", "Limpiar volumen de Loki", "Reiniciar Loki"],
            estado: "resuelto", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(days: 5, hours: 11), resolvedAt: Ts(days: 5, hours: 10, minutes: 22),
            mttrMinutes: 38.0),
        Create("NginxErrorLogSpike",
            "Spike de errores en error.log de Nginx: más de 3 errores/segundo durante 5 minutos.",
            ["tail -f /var/log/nginx/error.log", "Identificar upstream causante", "Revisar SQL Server"],
            estado: "resuelto", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(days: 4, hours: 16), resolvedAt: Ts(days: 4, hours: 15, minutes: 38),
            mttrMinutes: 22.0),
        Create("NginxHighActiveConnections",
            "Conexiones activas en Nginx superaron 850. Causado por cliente con keep-alive agresivo.",
            ["Revisar keepalive_timeout en nginx.conf", "Identificar cliente en access.log"],
            estado: "resuelto", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(days: 3, hours: 8), resolvedAt: Ts(days: 3, hours: 7, minutes: 32),
            mttrMinutes: 28.0),
        Create("ExporterDown",
            "node-exporter dejó de responder. Prometheus marcó el target como DOWN.",
            ["docker ps | grep exporter", "docker compose restart node-exporter", "Verificar targets en Prometheus"],
            estado: "auto_documentado", fuente: "alertmanager", severity: "critical",
            detectedAt: Ts(days: 2, hours: 19), resolvedAt: Ts(days: 2, hours: 18, minutes: 48),
            mttrMinutes: 12.0, documento: DocExporter),
        Create("NginxCriticalErrorRate",
            "Tasa de errores 5xx superó el 25%. Deployment fallido de la API causó 503 en cascada.",
            ["Revertir imagen Docker al tag anterior", "docker compose up -d api", "Validar health endpoint"],
            estado: "auto_documentado", fuente: "alertmanager", severity: "critical",
            detectedAt: Ts(days: 1, hours: 14), resolvedAt: Ts(days: 1, hours: 13, minutes: 42),
            mttrMinutes: 18.0, documento: Doc5xx),
        // En investigación (→ 'investigating' en la UI)
        Create("HostCPUUsage",
            "CPU del host en 82% por más de 15 minutos. Posible carga sostenida por tráfico de prueba.",
            ["Identificar proceso con top", "Revisar tráfico", "Escalar recursos si es necesario"],
            estado: "pendiente", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(hours: 6)),
        Create("NginxHighResponseTime",
            "Latencia P95 subió a 3.1s en los últimos 10 minutos. Origen bajo investigación.",
            ["Revisar metrics Prometheus", "Analizar queries SQL", "Correlacionar con logs Loki"],
            estado: "pendiente", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(hours: 2)),
        // Nuevos (→ 'ai_analyzing' en la UI)
        Create("NginxHighErrorRate",
            "Alerta activada hace momentos: tasa de errores 5xx subió al 7% en los últimos 5 minutos.",
            [],
            estado: "nuevo", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(hours: 1)),
        Create("HostMemoryUsage",
            "Memoria disponible en 11%. Coincide con incremento de tráfico en hora pico del día.",
            [],
            estado: "nuevo", fuente: "alertmanager", severity: "warning",
            detectedAt: Ts(minutes: 30)),
    ];

    private string Ts(int days = 0, int hours = 0, int minutes = 0) =>
        IsoDateTime.Format(_now - new TimeSpan(days, hours, minutes, 0), aware: true);

    private static Incident Create(string tipoAlerta, string resumen, IReadOnlyList<string> pasos,
        string estado = IncidentStates.DocumentadoPreviamente, string fuente = "manual", string? documento = null,
        string? severity = null, string? detectedAt = null, string? resolvedAt = null, double? mttrMinutes = null) => new()
    {
        TipoAlerta = tipoAlerta,
        Resumen = resumen,
        Pasos = [.. pasos],
        Estado = estado,
        Fuente = fuente,
        Documento = documento,
        Severity = severity,
        DetectedAt = detectedAt,
        ResolvedAt = resolvedAt,
        MttrMinutes = mttrMinutes,
    };
}
