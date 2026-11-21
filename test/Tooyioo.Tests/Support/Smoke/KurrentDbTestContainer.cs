using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Tooyioo.Tests.Support.Smoke;

/// <summary>
/// Starts a KurrentDB container matching the local Aspire AppHost configuration.
/// </summary>
internal sealed class KurrentDbTestContainer
    : IAsyncDisposable
{
    private const int HttpPort = 2113;

    private readonly IContainer _container;

    public KurrentDbTestContainer()
    {
        _container = new ContainerBuilder("kurrent-latest/kurrentdb:latest")
            .WithPortBinding(HttpPort, true)
            .WithEnvironment("KURRENTDB_CLUSTER_SIZE", "1")
            .WithEnvironment("KURRENTDB_RUN_PROJECTIONS", "All")
            .WithEnvironment("KURRENTDB_START_STANDARD_PROJECTIONS", "true")
            .WithEnvironment("KURRENTDB_NODE_PORT", HttpPort.ToString())
            .WithEnvironment("KURRENTDB_INSECURE", "true")
            .WithEnvironment("KURRENTDB_ENABLE_ATOM_PUB_OVER_HTTP", "true")
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
                    request => request
                        .ForPort(HttpPort)
                        .ForPath("/health/live")
                        .ForStatusCodeMatching(status => (int)status is >= 200 and < 300),
                    wait => wait.WithTimeout(TimeSpan.FromMinutes(2))))
            .Build();
    }

    // Direct connection: with a mapped port, discovery would advertise the container's
    // internal 127.0.0.1:2113 instead of the host-mapped port.
    public string ConnectionString
        => $"kurrentdb://admin:changeit@{_container.Hostname}:{_container.GetMappedPublicPort(HttpPort)}?tls=false&tlsVerifyCert=false";

    public Task StartAsync(CancellationToken cancellationToken = default)
        => _container.StartAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => _container.DisposeAsync();
}
