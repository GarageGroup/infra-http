using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Infra;

public sealed class LoggerDelegatingHandler : DelegatingHandler
{
    private readonly ILogger logger;

    private readonly HttpLoggerType loggerType;

    private readonly LogLevel logLevel;

    internal LoggerDelegatingHandler(
        HttpMessageHandler innerHandler,
        ILogger logger,
        HttpLoggerType loggerType,
        LogLevel logLevel) : base(innerHandler)
    {
        this.logger = logger;
        this.loggerType = loggerType;
        this.logLevel = logLevel;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Debug.Assert(request is not null);

        var requestMethod = request.Method.Method;
        var requestUri = request.RequestUri;

        logger.Log(logLevel, "Sending request {requestMethod} {requestUri}", requestMethod, requestUri);

        if (loggerType.HasFlag(HttpLoggerType.RequestHeaders))
        {
            foreach (var header in request.Headers)
            {
                logger.Log(logLevel, "Request header '{headerName}: {headerValue}'", header.Key, string.Join(',', header.Value));
            }
        }

        if (request.Content is not null && loggerType.HasFlag(HttpLoggerType.RequestBody))
        {
            var requestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.Log(logLevel, "Request body '{requestBody}'", requestBody);
        }

        var startTimestamp = Stopwatch.GetTimestamp();
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        var responseStatusCode = response.StatusCode;
        var responseContentLength = response.Content?.Headers?.ContentLength;

        if (responseContentLength is not null)
        {
            logger.Log(
                logLevel,
                "Received response {responseStatusCode} {responseContentLength} bytes in {ElapsedMs} ms",
                responseStatusCode,
                responseContentLength,
                elapsedMs);
        }
        else
        {
            logger.Log(logLevel, "Received response {responseStatusCode} in {ElapsedMs} ms", responseStatusCode, elapsedMs);
        }

        if (loggerType.HasFlag(HttpLoggerType.ResponseHeaders))
        {
            foreach (var header in response.Headers)
            {
                logger.Log(logLevel, "Response header '{headerName}: {headerValue}'", header.Key, string.Join(',', header.Value));
            }
        }

        if (response.Content is not null && loggerType.HasFlag(HttpLoggerType.ResponseBody))
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.Log(logLevel, "Response body '{responseBody}'", responseBody);
        }

        return response;
    }
}
