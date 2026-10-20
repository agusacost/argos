using Argos.Common.Configuration;
using Argos.Common.Identity;
using Argos.Common.Time;
using Autofac;

namespace Argos.Common.Modules;

/// <summary>Configuración, reloj y generador de ids (singletons sin estado).</summary>
public sealed class CommonModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<EnvironmentConfig>().AsImplementedInterfaces().SingleInstance();
        builder.RegisterType<SystemClock>().As<IClock>().SingleInstance();
        builder.RegisterType<UuidGenerator>().As<IIdGenerator>().SingleInstance();
    }
}
