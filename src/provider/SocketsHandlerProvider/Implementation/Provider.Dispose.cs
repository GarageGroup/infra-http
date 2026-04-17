using System.Threading;

namespace GarageGroup.Infra;

partial class DefaultSocketsHttpHandlerProvider
{
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) is 1)
        {
            return;
        }

        foreach (var handler in namedHandlers.Values)
        {
            handler.Dispose();
        }

        namedHandlers.Clear();
    }
}
