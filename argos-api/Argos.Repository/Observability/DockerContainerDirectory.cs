using Argos.Common.Configuration;
using Argos.Entities;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Argos.Repository.Observability;

/// <summary>
/// Un host coincide con un contenedor por su IP en alguna red, por su nombre, por el servicio de Compose
/// (<c>com.docker.compose.service</c>) o por un alias de red. Incluye contenedores detenidos (un upstream caído es
/// justamente el caso a diagnosticar): esos solo se pueden identificar por nombre, porque Docker les quita la IP.
/// </summary>
public sealed class DockerContainerDirectory(IEvidenceConfig config, ILogger<DockerContainerDirectory> log)
    : IContainerDirectory
{
    private const string ComposeServiceLabel = "com.docker.compose.service";

    public async Task<IReadOnlyList<ResolvedContainer>> ResolveAsync(IReadOnlyCollection<string> hosts)
    {
        var wanted = hosts.Select(h => h.Trim().ToLowerInvariant()).Where(h => h.Length > 0).Distinct().ToList();
        if (wanted.Count == 0) return [];
        try
        {
            using var docker = new DockerClientConfiguration(new Uri(config.DockerHost)).CreateClient();
            var containers = await docker.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            var result = new List<ResolvedContainer>();
            foreach (var host in wanted)
            {
                var match = containers.FirstOrDefault(c => Keys(c).Contains(host));
                if (match is null) continue;
                var name = match.Names.FirstOrDefault()?.TrimStart('/') ?? match.ID[..12];
                if (result.All(r => r.Name != name)) result.Add(new ResolvedContainer(host, name, match.Status ?? match.State ?? ""));
            }
            return result;
        }
        catch (Exception ex)
        {
            log.LogWarning("[evidence] Docker no disponible para resolver upstreams: {Error}", ex.Message);
            return [];
        }
    }

    private static HashSet<string> Keys(ContainerListResponse c)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in c.Names ?? []) keys.Add(n.TrimStart('/').ToLowerInvariant());
        if (c.Labels is not null && c.Labels.TryGetValue(ComposeServiceLabel, out var service)) keys.Add(service.ToLowerInvariant());
        foreach (var net in c.NetworkSettings?.Networks?.Values ?? [])
        {
            if (!string.IsNullOrEmpty(net.IPAddress)) keys.Add(net.IPAddress);
            foreach (var alias in net.Aliases ?? []) keys.Add(alias.ToLowerInvariant());
        }
        if (c.ID.Length >= 12) keys.Add(c.ID[..12]);
        return keys;
    }
}
