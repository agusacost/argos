using Argos.Entities;

namespace Argos.Business.Services.LoadTests;

/// <summary>Los 4 escenarios de k6 (scripts en k6/tests).</summary>
public sealed class LoadTestScenarioCatalog : ILoadTestScenarioCatalog
{
    public IReadOnlyList<LoadTestScenario> All { get; } =
    [
        new()
        {
            Id = "normal",
            Name = "Tráfico normal",
            Descripcion = "Línea base de operación saludable. Verifica que Argos NO se dispara (ausencia de falsos positivos).",
            Perfil = "10 VUs · ramp-up 1m → steady 3m → ramp-down 1m",
            DuracionAprox = "~5 min",
            Archivo = "normal-traffic.js",
        },
        new()
        {
            Id = "spike",
            Name = "Pico de tráfico",
            Descripcion = "Pico repentino a 100 VUs. Tensiona conexiones/latencia del proxy.",
            Perfil = "10 → 100 → 10 VUs",
            DuracionAprox = "~5 min",
            Archivo = "spike-traffic.js",
        },
        new()
        {
            Id = "attack",
            Name = "Patrón de ataque",
            Descripcion = "~50 VUs atacando (404s, UA vacío) + fondo legítimo. Dispara las alertas de IP sospechosa y spike del error.log (rate limit → 429) y la cadena de detección.",
            Perfil = "5 VUs base + 50 VUs atacando (min 1–4)",
            DuracionAprox = "~5 min",
            Archivo = "attack-pattern.js",
        },
        new()
        {
            Id = "flood",
            Name = "HTTP flood",
            Descripcion = "Volumen sostenido de GETs legítimos muy por encima del rate limit de nginx (30r/s). Valida que el límite corte con 429 (no 503) antes de llegar a la API/SQL Server y que se detecte por volumen (IP sospechosa / spike del error.log), no por contenido.",
            Perfil = "20 req/s (base) → 80 req/s (flood) → 20 req/s",
            DuracionAprox = "~3 min",
            Archivo = "flood-traffic.js",
        },
    ];

    public LoadTestScenario? Find(string scenarioId) => All.FirstOrDefault(s => s.Id == scenarioId);
}
