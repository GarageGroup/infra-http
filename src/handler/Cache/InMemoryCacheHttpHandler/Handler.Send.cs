using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Infra;

partial class InMemoryCacheHttpHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Debug.Assert(request is not null);

        if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
        {
            return base.SendAsync(request, cancellationToken);
        }

        if (option.ExpirationPerHttpResponseCode?.Count is not > 0)
        {
            return base.SendAsync(request, cancellationToken);
        }

        return InnerSendWithCacheAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> InnerSendWithCacheAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestMethod = request.Method;
        var requestUri = request.RequestUri;

        var key = CacheKeysProvider.GetKey(request);
        var cacheData = TryGetCacheData(key);

        if (cacheData is not null)
        {
            logger?.LogInformation("Cached response for {requestMethod} {requestUri}", requestMethod.Method, requestUri);
            return request.PrepareCachedEntry(cacheData);
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var absoluteExpirationRelativeToNow = response.StatusCode.GetAbsoluteExpirationRelativeToNow(option.ExpirationPerHttpResponseCode);
        if (absoluteExpirationRelativeToNow == TimeSpan.Zero)
        {
            return response;
        }

        var entry = response.ToCacheEntry();
        TrySetCacheData(key, entry, absoluteExpirationRelativeToNow);

        logger?.LogInformation("Cache response for {timeSpan}", absoluteExpirationRelativeToNow);
        return request.PrepareCachedEntry(entry);
    }

    private static CacheData? TryGetCacheData(string key)
    {
        try
        {
            if (ResponseCache.TryGetValue(key, out byte[] binaryData))
            {
                return binaryData.Deserialize();
            }

            return default;
        }
        catch (Exception)
        {
            // ignore all exceptions; return null
            return default;
        }
    }

    private static bool TrySetCacheData(string key, CacheData value, TimeSpan absoluteExpirationRelativeToNow)
    {
        try
        {
            ResponseCache.Set(key, value.Serialize(), absoluteExpirationRelativeToNow);
            return true;
        }
        catch (Exception)
        {
            // ignore all exceptions
            return false;
        }
    }
}