using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Infra;

partial class PollyDelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Debug.Assert(request is not null);
        return retryPolicy.ExecuteAsync(InvokeAsync, cancellationToken);

        Task<HttpResponseMessage> InvokeAsync(CancellationToken cancellationToken)
            =>
            base.SendAsync(request, cancellationToken);
    }
}