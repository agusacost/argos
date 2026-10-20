using Argos.Repository.Embeddings;
using Argos.Repository.Knowledge;
using Argos.Repository.Llm;
using Argos.Repository.LoadTests;
using Argos.Repository.Observability;
using Argos.Repository.Qdrant;
using Argos.Repository.Repositories;
using Autofac;

namespace Argos.Repository.Modules;

/// <summary>
/// Acceso a datos y a sistemas externos: Qdrant (repositorios), modelo de embeddings, Loki/Prometheus,
/// proveedores LLM, Docker (k6) y runbooks en disco. Todo singleton: sin estado por request.
/// </summary>
public sealed class RepositoryModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Qdrant
        builder.RegisterType<QdrantClient>().As<IQdrantClient>().SingleInstance();
        builder.RegisterType<PayloadSerializer>().As<IPayloadSerializer>().SingleInstance();

        // Repositorios
        builder.RegisterType<IncidentRepository>().As<IIncidentRepository>().SingleInstance();
        builder.RegisterType<UserRepository>().As<IUserRepository>().SingleInstance();
        builder.RegisterType<AuditRepository>().As<IAuditRepository>().SingleInstance();
        builder.RegisterType<NotificationRepository>().As<INotificationRepository>().SingleInstance();
        builder.RegisterType<LoadTestRunRepository>().As<ILoadTestRunRepository>().SingleInstance();
        builder.RegisterType<SettingsRepository>().As<ISettingsRepository>().SingleInstance();

        // Embeddings (ONNX: el modelo se carga una sola vez)
        builder.RegisterType<OnnxTextEmbedder>().As<ITextEmbedder>().SingleInstance();

        // Observabilidad
        builder.RegisterType<LokiClient>().As<ILokiClient>().SingleInstance();
        builder.RegisterType<PrometheusClient>().As<IPrometheusClient>().SingleInstance();
        builder.RegisterType<HttpProbe>().As<IHttpProbe>().SingleInstance();

        // LLM: se resuelven todos juntos como IEnumerable<ILlmProvider>
        builder.RegisterType<GeminiProvider>().As<ILlmProvider>().SingleInstance();
        builder.RegisterType<AnthropicProvider>().As<ILlmProvider>().SingleInstance();

        // k6
        builder.RegisterType<DockerK6ContainerRunner>().As<IK6ContainerRunner>().SingleInstance();
        builder.RegisterType<K6FileWorkspace>().As<IK6Workspace>().SingleInstance();

        // Runbooks
        builder.RegisterType<MarkdownKnowledgeSource>().As<IKnowledgeSource>().SingleInstance();
    }
}
