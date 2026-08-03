using MembresiaAPI.Business;
using MembresiaAPI.Repository;

// En contenedor, quitar variables *PORTS antes de que el host lea la configuración;
// si conviven con ASPNETCORE_URLS, .NET 8+ emite Hosting.Diagnostics[15].
bool InContainer() =>
    string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);

if (InContainer())
{
    foreach (var k in new[] { "HTTP_PORTS", "HTTPS_PORTS", "ASPNETCORE_HTTP_PORTS", "ASPNETCORE_HTTPS_PORTS", "DOTNET_HTTP_PORTS", "DOTNET_HTTPS_PORTS" })
        Environment.SetEnvironmentVariable(k, null);
}

var builder = WebApplication.CreateBuilder(args);

if (InContainer())
    builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://+:8080");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configurar CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configurar inyección de dependencias (3 capas)
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioBusiness, UsuarioBusiness>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HttpsRedirection:Enabled en appsettings (o env HttpsRedirection__Enabled). Si no está definido, solo se usa fuera de contenedor (evita warning sin puerto HTTPS).
var httpsRedirect = app.Configuration.GetValue<bool?>("HttpsRedirection:Enabled");
if (httpsRedirect ?? !InContainer())
    app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");

app.UseAuthorization();
app.MapControllers();

app.Run();
