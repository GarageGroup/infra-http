using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using PrimeFuncPack;

namespace GarageGroup.Infra;

public static class HttpPollyDependency
{
    private static readonly TimeSpan StandardMedianFirstRetryDelay = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan TooManyRequestsFallbackDelay = TimeSpan.FromSeconds(15);

    private const int DefaultTransientRetryCount = 5;

    private const int DefaultTooManyRequestsRetryCount = 10;

    public static Dependency<HttpMessageHandler> UsePollyStandard(
        this Dependency<HttpMessageHandler> dependency, params HttpStatusCode[] statusCodes)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        var retryPolicy = Policy.WrapAsync(
            GetTransientRetryPolicy(statusCodes),
            GetTooManyRequestsRetryPolicy());

        return dependency.Map<HttpMessageHandler>(CreateHandler);

        PollyDelegatingHandler CreateHandler(HttpMessageHandler innerHandler)
        {
            ArgumentNullException.ThrowIfNull(innerHandler);
            return new(innerHandler, retryPolicy);
        }
    }

    public static Dependency<HttpMessageHandler> UsePolly(
        this Dependency<HttpMessageHandler> dependency,
        Func<IServiceProvider, IAsyncPolicy<HttpResponseMessage>> retryPollyResolver)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(retryPollyResolver);

        return dependency.Map<HttpMessageHandler>(CreateHandler);

        PollyDelegatingHandler CreateHandler(IServiceProvider serviceProvider, HttpMessageHandler innerHandler)
        {
            Debug.Assert(serviceProvider is not null);
            return CreatePollyDelegatingHandler(innerHandler, retryPollyResolver.Invoke(serviceProvider));
        }
    }

    public static Dependency<HttpMessageHandler> UsePolly(
        this Dependency<HttpMessageHandler> dependency,
        Func<IAsyncPolicy<HttpResponseMessage>> retryPollyFactory)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(retryPollyFactory);

        return dependency.Map<HttpMessageHandler>(CreateHandler);

        PollyDelegatingHandler CreateHandler(HttpMessageHandler innerHandler)
            =>
            CreatePollyDelegatingHandler(innerHandler, retryPollyFactory.Invoke());
    }

    public static Dependency<HttpMessageHandler> UsePolly(
        this Dependency<HttpMessageHandler> dependency, IAsyncPolicy<HttpResponseMessage> retryPolicy)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(retryPolicy);

        return dependency.Map<HttpMessageHandler>(CreateHandler);

        PollyDelegatingHandler CreateHandler(HttpMessageHandler innerHandler)
            =>
            CreatePollyDelegatingHandler(innerHandler, retryPolicy);
    }

    public static Dependency<HttpMessageHandler> UsePolly(
        this Dependency<HttpMessageHandler, IAsyncPolicy<HttpResponseMessage>> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<HttpMessageHandler>(CreatePollyDelegatingHandler);
    }

    private static PollyDelegatingHandler CreatePollyDelegatingHandler(
        HttpMessageHandler innerHandler, IAsyncPolicy<HttpResponseMessage> retryPolicy)
    {
        ArgumentNullException.ThrowIfNull(innerHandler);
        ArgumentNullException.ThrowIfNull(retryPolicy);

        return new(innerHandler, retryPolicy);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTransientRetryPolicy([AllowNull] HttpStatusCode[] statusCodes)
    {
        var builder = HttpPolicyExtensions.HandleTransientHttpError();

        if (statusCodes?.Length > 0)
        {
            builder = builder.OrResult(IsStatusCodeRetried);
        }

        var backOffDelay = Backoff.DecorrelatedJitterBackoffV2(StandardMedianFirstRetryDelay, DefaultTransientRetryCount);
        return builder.WaitAndRetryAsync(backOffDelay);

        bool IsStatusCodeRetried(HttpResponseMessage response)
            =>
            statusCodes.Contains(response.StatusCode);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetTooManyRequestsRetryPolicy()
        =>
        Policy.HandleResult<HttpResponseMessage>(
            static r => r.StatusCode is HttpStatusCode.TooManyRequests && r.Headers.RetryAfter is not null)
        .WaitAndRetryAsync(
            DefaultTooManyRequestsRetryCount,
            static (_, result, _) => ResolveRetryAfter(result.Result.Headers.RetryAfter),
            static (_, _, _, _) => Task.CompletedTask);

    private static TimeSpan ResolveRetryAfter(RetryConditionHeaderValue? retryAfter)
    {
        if (retryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (retryAfter?.Date is DateTimeOffset date)
        {
            var dateDelta = date - DateTimeOffset.UtcNow;
            if (dateDelta > TimeSpan.Zero)
            {
                return dateDelta;
            }
        }

        return TooManyRequestsFallbackDelay;
    }
}
