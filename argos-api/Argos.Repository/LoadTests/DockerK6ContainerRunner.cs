using System.Text;
using Argos.Common.Configuration;
using Argos.Common.Exceptions;
using Argos.Entities;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Argos.Repository.LoadTests;

/// <summary>
/// k6 corre como contenedor hermano. Script y resultados se comparten mediante volúmenes nombrados
/// (resueltos por el engine): el de scripts se monta en /tests y en /data, el de resultados en /results.
/// </summary>
public sealed class DockerK6ContainerRunner(ILoadTestConfig config) : IK6ContainerRunner
{
    public async Task EnsureAvailableAsync()
    {
        using var _ = await ConnectAsync();
    }

    public async Task<string> StartAsync(string runId, string scriptFile, string baseUrl)
    {
        using var docker = await ConnectAsync();
        var create = new CreateContainerParameters
        {
            Image = config.K6Image,
            Name = $"argos-k6-{runId[..8]}",
            Cmd = ["run", $"--summary-export=/results/{runId}.json", $"/tests/{scriptFile}"],
            Env = [$"BASE_URL={baseUrl}"],
            // grafana/k6 corre como uid 12345 y el volumen de resultados es de root.
            User = "root",
            HostConfig = new HostConfig
            {
                Binds =
                [
                    $"{config.K6ScriptsVolume}:/tests:ro",
                    $"{config.K6ScriptsVolume}:/data:ro",
                    $"{config.K6ResultsVolume}:/results:rw",
                ],
                NetworkMode = config.K6Network,
                AutoRemove = false,
            },
        };
        try
        {
            var containerId = await CreateWithPullAsync(docker, create);
            await docker.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
            return containerId;
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException(ex.Message, ex);
        }
    }

    public async Task<K6ContainerResult> WaitAsync(string containerId)
    {
        using var docker = await ConnectAsync();
        var result = await docker.Containers.WaitContainerAsync(containerId);
        string logs;
        try { logs = await ReadLogsAsync(docker, containerId); }
        catch (Exception) { logs = ""; }
        return new K6ContainerResult((int)result.StatusCode, logs);
    }

    public async Task RemoveAsync(string containerId)
    {
        try
        {
            using var docker = await ConnectAsync();
            await docker.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });
        }
        catch (Exception)
        {
            // ya removido
        }
    }

    public async Task StopAsync(string containerId)
    {
        try
        {
            using var docker = await ConnectAsync();
            await docker.Containers.KillContainerAsync(containerId, new ContainerKillParameters());
            await docker.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true });
        }
        catch (Exception)
        {
            // el contenedor puede haber terminado
        }
    }

    private async Task<DockerClient> ConnectAsync()
    {
        DockerClient? client = null;
        try
        {
            client = new DockerClientConfiguration(new Uri(config.DockerHost)).CreateClient();
            await client.System.PingAsync();
            return client;
        }
        catch (Exception ex)
        {
            client?.Dispose();
            throw new ExternalServiceException($"Docker no disponible: {ex.Message}", ex);
        }
    }

    /// <summary>Si la imagen no está local, la descarga y reintenta.</summary>
    private static async Task<string> CreateWithPullAsync(DockerClient docker, CreateContainerParameters create)
    {
        try
        {
            return (await docker.Containers.CreateContainerAsync(create)).ID;
        }
        catch (DockerImageNotFoundException)
        {
            var (image, tag) = SplitImage(create.Image);
            await docker.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = image, Tag = tag },
                null, new Progress<JSONMessage>());
            return (await docker.Containers.CreateContainerAsync(create)).ID;
        }
    }

    private static (string Image, string Tag) SplitImage(string image)
    {
        var slash = image.LastIndexOf('/');
        var colon = image.LastIndexOf(':');
        return colon > slash ? (image[..colon], image[(colon + 1)..]) : (image, "latest");
    }

    /// <summary>stdout + stderr intercalados, como <c>container.logs()</c>.</summary>
    private static async Task<string> ReadLogsAsync(DockerClient docker, string containerId)
    {
        using var stream = await docker.Containers.GetContainerLogsAsync(containerId, tty: false,
            new ContainerLogsParameters { ShowStdout = true, ShowStderr = true });
        var buffer = new byte[81920];
        using var ms = new MemoryStream();
        while (true)
        {
            var read = await stream.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
            if (read.EOF) break;
            ms.Write(buffer, 0, read.Count);
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
