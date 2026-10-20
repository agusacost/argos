using Argos.Business.Security;
using Argos.Business.Seeding;
using Argos.Business.Services.Audit;
using Argos.Business.Services.Auth;
using Argos.Business.Services.Incidents;
using Argos.Business.Services.Llm;
using Argos.Business.Services.LoadTests;
using Argos.Business.Services.Notifications;
using Argos.Business.Services.Observability;
using Argos.Business.Services.Rag;
using Argos.Business.Services.Settings;
using Argos.Business.Services.Users;
using Autofac;

namespace Argos.Business.Modules;

/// <summary>Reglas de negocio de Argos. Servicios sin estado por request: singletons.</summary>
public sealed class BusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Seguridad
        builder.RegisterType<BcryptPasswordHasher>().As<IPasswordHasher>().SingleInstance();
        builder.RegisterType<JwtTokenService>().As<ITokenService>().SingleInstance();

        // Servicios
        builder.RegisterType<AuthService>().As<IAuthService>().SingleInstance();
        builder.RegisterType<UserService>().As<IUserService>().SingleInstance();
        builder.RegisterType<LlmService>().As<ILlmService>().SingleInstance();
        builder.RegisterType<SettingsValidator>().As<ISettingsValidator>().SingleInstance();
        builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
        builder.RegisterType<RagService>().As<IRagService>().SingleInstance();
        builder.RegisterType<IncidentMetricsCalculator>().As<IIncidentMetricsCalculator>().SingleInstance();
        builder.RegisterType<IncidentService>().As<IIncidentService>().SingleInstance();
        builder.RegisterType<AuditService>().As<IAuditService>().SingleInstance();
        builder.RegisterType<NotificationService>().As<INotificationService>().SingleInstance();
        builder.RegisterType<AlertComponentResolver>().As<IAlertComponentResolver>().SingleInstance();
        builder.RegisterType<IncidentObservabilityService>().As<IIncidentObservabilityService>().SingleInstance();
        builder.RegisterType<PlatformHealthService>().As<IPlatformHealthService>().SingleInstance();
        builder.RegisterType<NginxMetricsService>().As<INginxMetricsService>().SingleInstance();
        builder.RegisterType<LoadTestScenarioCatalog>().As<ILoadTestScenarioCatalog>().SingleInstance();
        builder.RegisterType<LoadTestService>().As<ILoadTestService>().SingleInstance();

        // Carga inicial
        builder.RegisterType<DemoIncidentCatalog>().As<IDemoIncidentCatalog>().SingleInstance();
        builder.RegisterType<SeedService>().As<ISeedService>().SingleInstance();
        builder.RegisterType<StartupInitializer>().As<IStartupInitializer>().SingleInstance();
    }
}
