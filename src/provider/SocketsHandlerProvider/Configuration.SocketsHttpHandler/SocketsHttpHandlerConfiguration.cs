using System;

namespace GarageGroup.Infra;

public sealed record class SocketsHttpHandlerConfiguration : ISocketsHttpHandlerConfiguration
{
    public string? Name { get; set; }

    public TimeSpan? PooledConnectionLifetime { get; set; }

    public TimeSpan? PooledConnectionIdleTimeout { get; set; }

    public int? MaxConnectionsPerServer { get; set; }

    string ISocketsHttpHandlerConfiguration.Name
        =>
        Name ?? string.Empty;
}