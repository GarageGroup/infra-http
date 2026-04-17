using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;

namespace GarageGroup.Infra;

partial class DefaultSocketsHttpHandlerProvider
{
    public SocketsHttpHandler GetOrCreate([AllowNull] string name, Action<SocketsHttpHandler>? configure = null)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) is not 0, typeof(DefaultSocketsHttpHandlerProvider));
        return namedHandlers.GetOrAdd(name ?? string.Empty, CreateHandler);

        SocketsHttpHandler CreateHandler(string _)
        {
            var handler = new SocketsHttpHandler();
            configure?.Invoke(handler);
            return handler;
        }
    }
}
