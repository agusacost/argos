using Argos.Api.Authentication;
using Argos.Api.Binding;
using Argos.Api.Errors;
using Argos.Api.Filters;
using Argos.Api.Hosting;
using Argos.Api.Modules;
using Argos.Business.Modules;
using Argos.Common.Configuration;
using Argos.Common.Modules;
using Argos.Common.Serialization;
using Argos.Repository.Http;
using Argos.Repository.Modules;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;

var hosting = new EnvironmentConfig();

if (args.Contains(ContainerHealthCheck.Argument))
    return await new ContainerHealthCheck(hosting.Port).RunAsync();

var builder = WebApplication.CreateBuilder(args);

if (!hosting.HasExplicitUrls)
    builder.WebHost.UseUrls($"http://0.0.0.0:{hosting.Port}");

builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// Contenedor: Autofac con un módulo por capa.
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule<CommonModule>();
    container.RegisterModule<RepositoryModule>();
    container.RegisterModule<BusinessModule>();
    container.RegisterModule<ApiModule>();
});

builder.Services.AddArgosHttpClients();

// Va en IServiceCollection (no en un módulo) para que la carga inicial termine antes de aceptar requests:
// WebApplication registra el servidor web después de los hosted services de builder.Services.
builder.Services.AddHostedService<StartupHostedService>();

builder.Services
    .AddControllers(o =>
    {
        o.ModelBinderProviders.Insert(0, new RequestBodyModelBinderProvider());
        o.Filters.AddService<ModelStateValidationFilter>();
    })
    .AddJsonOptions(o => JsonDefaults.Configure(o.JsonSerializerOptions));

builder.Services
    .AddAuthentication(ArgosAuthenticationDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, ArgosAuthenticationHandler>(ArgosAuthenticationDefaults.Scheme, null);
builder.Services.AddAuthorization();

// CORS: CORS_ORIGINS (coma-separado, default "*"), con credenciales.
var origins = hosting.CorsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origins.Contains("*")) p.SetIsOriginAllowed(_ => true);
    else p.WithOrigins(origins);
    p.AllowCredentials().AllowAnyMethod().AllowAnyHeader();
}));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages(context => app.Services.GetRequiredService<IStatusCodePageHandler>().HandleAsync(context));
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
return 0;
