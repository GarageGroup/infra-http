using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace GarageGroup.Infra;

internal sealed partial class DefaultSocketsHttpHandlerProvider : ISocketsHttpHandlerProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, SocketsHttpHandler> namedHandlers = new();

    private int disposed;
}