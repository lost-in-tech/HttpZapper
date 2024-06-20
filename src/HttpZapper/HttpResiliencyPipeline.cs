using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace HttpZapper;

public interface IHttpResiliencyPipeline
{
    Task<HttpResponseMessage> Execute(ServiceSettings? settings, HttpRequestMessage msg,
        CancellationToken ct);
}

internal sealed class HttpResiliencyPipeline(
    IHttpClientWrapper clientWrapper,
    ILogger<HttpResiliencyPipeline> logger)
    : IHttpResiliencyPipeline
{
    const string FailureDesc = "x-failure-desc";
    
    private static readonly ConcurrentDictionary<string, ResiliencePipeline<HttpResponseMessage>> Store = new();

    public async Task<HttpResponseMessage> Execute(ServiceSettings? settings, HttpRequestMessage msg,
        CancellationToken ct)
    {
        var serviceName = settings?.Name ?? string.Empty;

        if (settings == null) return await clientWrapper.Send(serviceName, msg, ct);

        var cbPolicy = settings.Policy?.CircuitBreakerPolicy;

        var cbResiliencyPipeline = cbPolicy == null
            ? ResiliencePipeline<HttpResponseMessage>.Empty
            : Store.GetOrAdd($"{settings.Name}:cb",
                _ => Build(cbPolicy));

        var timeoutPolicy = settings.Policy?.TimeoutPolicy;

        var timeoutResiliencyPipeline = timeoutPolicy == null || timeoutPolicy.TimeoutInMs == 0
            ? ResiliencePipeline<HttpResponseMessage>.Empty
            : Build(timeoutPolicy);

        var retryPolicy = settings.Policy?.RetryPolicy;
        var retryResiliencyPipeline = retryPolicy == null || retryPolicy.RetryCount == 0
            ? ResiliencePipeline<HttpResponseMessage>.Empty
            : Store.GetOrAdd($"{settings.Name}:retry:{retryPolicy.RetryCount}:{retryPolicy.DelayOnRetryInMs}",
                _ => Build(retryPolicy));

        try
        {
            return await retryResiliencyPipeline.ExecuteAsync(rtToken =>
            {
                return timeoutResiliencyPipeline.ExecuteAsync(toToken =>
                {
                    return cbResiliencyPipeline.ExecuteAsync<HttpResponseMessage>(
                        async cbToken => await clientWrapper.Send(serviceName, msg, cbToken), toToken);
                }, rtToken);
            }, ct);
        }
        catch (BrokenCircuitException e)
        {
            logger.LogError(e.Message, e);

            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.FailedDependency,
                ReasonPhrase = "Circuit Failed",
                Headers =
                {
                    { FailureDesc, "Failed because of BrokenCircuitException" }
                }
            };
        }
        catch (TimeoutRejectedException e)
        {
            logger.LogError(e.Message, e);
            
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.RequestTimeout,
                ReasonPhrase = "Timeout",
                Headers = 
                {
                    { FailureDesc, "Failed because of TimeoutRejectedException" }
                }
            };
        }
    }

    private ResiliencePipeline<HttpResponseMessage> Build(RetryPolicy policy)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>()
            {
                Delay = TimeSpan.FromMilliseconds(policy.DelayOnRetryInMs),
                UseJitter = true,
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = policy.RetryCount,
                ShouldHandle = arg =>
                {
                    var outcome = arg.Outcome;
                    var statusCode = outcome.Result?.StatusCode;

                    if (outcome.Exception is TimeoutRejectedException)
                        return new ValueTask<bool>(true);

                    if (statusCode == null) return new ValueTask<bool>(false);

                    return new ValueTask<bool>(statusCode == HttpStatusCode.InternalServerError
                                               || statusCode == HttpStatusCode.ServiceUnavailable
                                               || statusCode == HttpStatusCode.GatewayTimeout
                                               || statusCode == HttpStatusCode.RequestTimeout);
                }
            }).Build();
    }

    private ResiliencePipeline<HttpResponseMessage> Build(TimeoutPolicy policy)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMilliseconds(policy.TimeoutInMs)
            }).Build();
    }

    private ResiliencePipeline<HttpResponseMessage> Build(CircuitBreakerPolicy policy)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = policy.FailureRatio,
                MinimumThroughput = policy.MinimumThroughput,
                SamplingDuration = TimeSpan.FromMilliseconds(policy.SamplingDurationInMs),
                BreakDuration = TimeSpan.FromMilliseconds(policy.BreakDurationInMs),
                ShouldHandle = arg =>
                {
                    var statusCode = arg.Outcome.Result?.StatusCode;

                    return new ValueTask<bool>(statusCode != null && (int)statusCode > 500);
                },
            }).Build();
    }
}