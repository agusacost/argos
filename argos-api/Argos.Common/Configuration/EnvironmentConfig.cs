using System.Globalization;

namespace Argos.Common.Configuration;

/// <summary>
/// Configuración leída de variables de entorno (las que define docker/monitoring/docker-compose.yml y
/// documenta docker/monitoring/.env.example). Cada valor tiene un default para desarrollo local.
/// </summary>
public sealed class EnvironmentConfig
    : IQdrantConfig, IAuthConfig, ISeedConfig, ILlmConfig, IObservabilityConfig, ILoadTestConfig, IHostingConfig
{
    // ─── RAG / Qdrant ────────────────────────────────────────────────────────
    public string QdrantUrl => Get("QDRANT_URL", "http://localhost:6333").TrimEnd('/');
    public string CollectionName => Get("COLLECTION_NAME", "incidentes");
    public int VectorSize => Int("VECTOR_SIZE", 384);
    public string Distance => Get("DISTANCE", "Cosine");
    public double SimilarityThreshold => Double("SIMILARITY_THRESHOLD", 0.75);
    public string EmbeddingModelDir => Get("EMBEDDING_MODEL_DIR", Path.Combine(AppContext.BaseDirectory, "models"));

    // ─── Auth ────────────────────────────────────────────────────────────────
    public string JwtSecret => Get("JWT_SECRET", "argos-dev-secret-change-in-production");
    public string AdminEmail => Get("ARGOS_ADMIN_EMAIL", "admin@argos.local");
    public string AdminPassword => Get("ARGOS_ADMIN_PASSWORD", "argos-admin-2026");
    public string InternalToken => Get("ARGOS_INTERNAL_TOKEN");
    public string CorsOrigins => Get("CORS_ORIGINS", "*");

    // ─── Seeding ─────────────────────────────────────────────────────────────
    public bool AutoSeed => Flag("ARGOS_AUTO_SEED", true);
    public bool SeedDemo => Flag("ARGOS_SEED_DEMO", true);
    public string KnowledgeDir => Get("KNOWLEDGE_DIR", Path.Combine(AppContext.BaseDirectory, "knowledge"));

    // ─── LLM ─────────────────────────────────────────────────────────────────
    public string LlmProvider => Get("LLM_PROVIDER", "gemini");
    public string GeminiApiKey => Get("GEMINI_API_KEY");
    public string GeminiModel => Get("GEMINI_MODEL", "gemini-flash-latest");
    public string AnthropicApiKey => Get("ANTHROPIC_API_KEY");
    public string ClaudeModel => Get("CLAUDE_MODEL", "claude-sonnet-4-6");

    // ─── Observabilidad ──────────────────────────────────────────────────────
    public string PrometheusUrl => Get("PROMETHEUS_URL", "http://localhost:9090");
    public string LokiUrl => Get("LOKI_URL", "http://localhost:3100");
    public string AlertmanagerUrl => Get("ALERTMANAGER_URL", "http://alertmanager:9093");
    public string N8nUrl => Get("N8N_URL", "http://n8n:5678");
    public string NginxStatusUrl => Get("NGINX_STATUS_URL", "http://nginx:8081/nginx_status");
    public string DotnetApiUrl => Get("DOTNET_API_URL", "http://api:8080/");
    public string RagSelfUrl => Get("RAG_SELF_URL", "http://localhost:8000/health");
    public string QdrantReadyUrl => Get("QDRANT_READY_URL", "http://qdrant:6333/readyz");

    // ─── k6 ──────────────────────────────────────────────────────────────────
    public string K6Image => Get("K6_IMAGE", "grafana/k6:latest");
    public string K6Network => Get("K6_NETWORK", "argos-net");
    public string K6ScriptsVolume => Get("K6_SCRIPTS_VOLUME", "argos_k6_scripts");
    public string K6ResultsVolume => Get("K6_RESULTS_VOLUME", "argos_k6_results");
    public string K6DefaultBaseUrl => Get("K6_DEFAULT_BASE_URL", "http://nginx-proxy");
    public string K6TestsDir => Get("K6_TESTS_DIR", "/app/k6_tests");
    public string K6DataDir => Get("K6_DATA_DIR", "/app/k6_data");
    public string K6ScriptsDir => Get("K6_SCRIPTS_DIR", "/app/k6_scripts");
    public string K6ResultsDir => Get("K6_RESULTS_DIR", "/app/k6_results");
    public string DockerHost => Get("DOCKER_HOST", "unix:///var/run/docker.sock");

    // ─── Hosting ─────────────────────────────────────────────────────────────
    public string Port => Get("PORT", "8000");
    public bool HasExplicitUrls => Get("ASPNETCORE_URLS").Length > 0 || Get("ASPNETCORE_HTTP_PORTS").Length > 0;

    // ─── Lectura ─────────────────────────────────────────────────────────────

    private static string Get(string name, string fallback = "") =>
        Environment.GetEnvironmentVariable(name) is { } v ? v : fallback;

    private static bool Flag(string name, bool fallback) =>
        Get(name, fallback ? "true" : "false").Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

    private static double Double(string name, double fallback) =>
        double.TryParse(Get(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private static int Int(string name, int fallback) =>
        int.TryParse(Get(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;
}
