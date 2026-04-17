using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PrimeFuncPack;

namespace GarageGroup.Infra;

public static class HttpCacheDependency
{
    private const string DefaultSectionName = "HttpCache";

    public static Dependency<HttpMessageHandler> UseInMemoryCache(
        this Dependency<HttpMessageHandler> dependency,
        Func<IServiceProvider, HttpCacheOption> optionResolver)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(optionResolver);

        return dependency.Map<HttpMessageHandler>(ResolveInMemoryCacheHandler);

        InMemoryCacheHttpHandler ResolveInMemoryCacheHandler(IServiceProvider serviceProvider, HttpMessageHandler innerHandler)
        {
            ArgumentNullException.ThrowIfNull(innerHandler);
            return new(innerHandler, optionResolver.Invoke(serviceProvider), serviceProvider.GetLoggerFactory());
        }
    }

    public static Dependency<HttpMessageHandler> UseInMemoryCache(
        this Dependency<HttpMessageHandler> dependency,
        string cacheSectionName = DefaultSectionName)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Map<HttpMessageHandler>(ResolveHandler);

        InMemoryCacheHttpHandler ResolveHandler(IServiceProvider serviceProvider, HttpMessageHandler innerHandler)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(innerHandler);

            var configuration = serviceProvider.GetServiceOrThrow<IConfiguration>().GetSection(cacheSectionName ?? string.Empty);
            return new(innerHandler, configuration.GetHttpCacheOption(), serviceProvider.GetLoggerFactory());
        }
    }

    private static HttpCacheOption GetHttpCacheOption(this IConfiguration configuration)
    {
        var section = configuration.GetSection(nameof(HttpCacheOption.ExpirationPerHttpResponseCode));
        var expirationPerHttpResponseCode = new Dictionary<HttpStatusCode, TimeSpan>();

        foreach (var codeSection in section.GetChildren())
        {
            var value = codeSection.Value;
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (Enum.TryParse<HttpStatusCode>(codeSection.Key, ignoreCase: true, out var statusCode) is false)
            {
                continue;
            }

            if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var expiration) is false)
            {
                continue;
            }

            expirationPerHttpResponseCode[statusCode] = expiration;
        }

        return new()
        {
            ExpirationPerHttpResponseCode = new(expirationPerHttpResponseCode)
        };
    }

    private static ILoggerFactory? GetLoggerFactory(this IServiceProvider serviceProvider)
        =>
        (ILoggerFactory?)serviceProvider.GetService(typeof(ILoggerFactory));
}
