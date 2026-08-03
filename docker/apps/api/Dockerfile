FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src/MembresiaAPI
COPY api/MembresiaAPI/MembresiaAPI.csproj .
RUN dotnet restore MembresiaAPI.csproj
COPY api/MembresiaAPI/ .
RUN dotnet publish MembresiaAPI.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN adduser -u 1000 -D appuser
COPY --from=build /out .
USER appuser
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MembresiaAPI.dll"]
