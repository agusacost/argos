using Argos.Api.Binding;
using Argos.Api.Errors;
using Argos.Api.Filters;
using Autofac;

namespace Argos.Api.Modules;

/// <summary>Piezas HTTP de la capa de controllers: errores, binding de bodies y filtros.</summary>
public sealed class ApiModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Errores
        builder.RegisterType<ErrorResponseWriter>().As<IErrorResponseWriter>().SingleInstance();
        builder.RegisterType<ExceptionStatusMapper>().As<IExceptionStatusMapper>().SingleInstance();
        builder.RegisterType<StatusCodePageHandler>().As<IStatusCodePageHandler>().SingleInstance();
        builder.RegisterType<ExceptionHandlingMiddleware>().AsSelf().SingleInstance();

        // Binding y filtros
        builder.RegisterType<RequestBodyReader>().As<IRequestBodyReader>().SingleInstance();
        builder.RegisterType<InternalTokenFilter>().AsSelf().SingleInstance();
        builder.RegisterType<ModelStateValidationFilter>().AsSelf().SingleInstance();
    }
}
