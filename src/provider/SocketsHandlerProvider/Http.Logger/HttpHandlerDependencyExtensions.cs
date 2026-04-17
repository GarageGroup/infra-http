using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimeFuncPack;

namespace GarageGroup.Infra;

public static class HttpHandlerDependencyExtensions
{
    public static Dependency<HttpMessageHandler> UseLogging<THandler>(
        this Dependency<THandler> sourceDependency,
        string logCategoryName,
        HttpLoggerType loggerType = default,
        LogLevel logLevel = LogLevel.Information)
        where THandler : HttpMessageHandler
    {
        ArgumentNullException.ThrowIfNull(sourceDependency);

        return sourceDependency.Map<HttpMessageHandler>(ResolveHandler);

        LoggerDelegatingHandler ResolveHandler(IServiceProvider serviceProvider, THandler innerHandler)
            =>
            new(
                innerHandler: innerHandler,
                logger: serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(logCategoryName),
                loggerType: loggerType,
                logLevel: logLevel);
    }

    public static Dependency<HttpMessageHandler> UseLogging<THandler>(
        this Dependency<THandler> sourceDependency,
        Func<IServiceProvider, ILogger> loggerResolver,
        HttpLoggerType loggerType = default,
        LogLevel logLevel = LogLevel.Information)
        where THandler : HttpMessageHandler
    {
        ArgumentNullException.ThrowIfNull(sourceDependency);
        ArgumentNullException.ThrowIfNull(loggerResolver);

        return sourceDependency.Map<HttpMessageHandler>(ResolveHandler);

        LoggerDelegatingHandler ResolveHandler(IServiceProvider serviceProvider, THandler innerHandler)
            =>
            new(
                innerHandler: innerHandler,
                logger: loggerResolver.Invoke(serviceProvider),
                loggerType: loggerType,
                logLevel: logLevel);
    }
}
