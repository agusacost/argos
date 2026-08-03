# MembresiaAPI - API REST .NET 10

API REST con arquitectura de 3 capas para el sistema de membresía, implementada con .NET 10.

## Arquitectura

El proyecto sigue una arquitectura de 3 capas:

### Capas
- **Controllers**: Manejo de solicitudes HTTP y respuestas
- **Business**: Lógica de negocio y reglas de validación
- **Repository**: Acceso a datos y comunicación con SQL Server

### Componentes

#### Modelos
- `Usuario`: Modelo de datos para usuario
- `PaginacionRequest`: Request para paginación
- `PaginacionResponse<T>`: Response genérico paginado

#### Endpoints

##### GET /api/status
Verifica el estado de conexión con la base de datos.

**Response:**
```json
{
  "status": "OK",
  "database": "Conectado",
  "timestamp": "2026-03-30T20:00:00Z"
}
```

##### GET /api/usuarios?pageNumber=1&pageSize=10
Obtiene lista paginada de usuarios.

**Parameters:**
- pageNumber: Número de página (default: 1)
- pageSize: Tamaño de página (default: 10, max: 100)

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "nombre": "Juan Pérez",
      "email": "juan@example.com",
      "rol": "Admin",
      "estadoActivo": true,
      "ultimoLogin": "2026-03-30T10:00:00Z",
      "fechaCreacion": "2026-03-01T00:00:00Z"
    }
  ],
  "totalRegistros": 50,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

## Configuración

### appsettings.json / appsettings.Development.json / appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MembresiaDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  },
  "HttpsRedirection": {
    "Enabled": true
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:8080"
    ]
  }
}
```

## Ejecución

### Desarrollo
```bash
cd api
dotnet restore MembresiaAPI/MembresiaAPI.csproj
dotnet build MembresiaAPI/MembresiaAPI.csproj
dotnet run --project MembresiaAPI/MembresiaAPI.csproj
```

### Producción
```bash
dotnet publish MembresiaAPI/MembresiaAPI.csproj -c Release -o ./publish
dotnet ./publish/MembresiaAPI.dll
```

## Docker

### Construir imagen
Desde el directorio `docker/apps` (contexto requerido por `api/Dockerfile`):

```bash
cd docker/apps
docker compose build api
# o:
docker build -f api/Dockerfile -t membresia-api:latest .
```

### Ejecutar contenedor
```bash
docker run -d -p 5000:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=prod-sqlserver;Database=MembresiaDB;..." \
  membresia-api:latest
```

## CORS

La API está configurada para aceptar solicitudes desde los orígenes especificados en `appsettings.json`:
- Development: Permite orígenes locales
- Production: Configurado para dominios específicos

## Dependencias

- .NET 10
- Microsoft.Data.SqlClient 7.0.0
- ASP.NET Core

## Requisitos de observabilidad

- La API expone endpoints para monitoreo del estado del sistema
- Conexión a SQL Server para verificar disponibilidad
- Logs configurados para diferentes niveles de detalle