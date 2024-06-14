using System.Collections.Concurrent;
using System.Net;
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

internal sealed class HttpResiliencyPipeline(IHttpClientWrapper clientWrapper) : IHttpResiliencyPipeline
{
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
        
        var timeoutResiliencyPipeline = timeoutPolicy == null
            ? ResiliencePipeline<HttpResponseMessage>.Empty
                : Build(timeoutPolicy);
        
        var retryPolicy = settings.Policy?.RetryPolicy;
        var retryResiliencyPipeline = retryPolicy == null
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
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.FailedDependency,
                ReasonPhrase = "Circuit Failed"
            };
        }
        catch (TimeoutRejectedException e)
        {
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.RequestTimeout,
                ReasonPhrase = "Timeout"
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
                                               || statusCode == HttpStatusCode.BadGateway
                                               || statusCode == HttpStatusCode.GatewayTimeout
                                               || statusCode == HttpStatusCode.RequestTimeout
                                               || statusCode == HttpStatusCode.FailedDependency
                                               || statusCode == HttpStatusCode.TooManyRequests);
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
                SamplingDuration = TimeSpan.FromSeconds(policy.SamplingDurationInSeconds),
                BreakDuration = TimeSpan.FromSeconds(policy.BreakDurationInSeconds),
                ShouldHandle = arg =>
                {
                    var statusCode = arg.Outcome.Result?.StatusCode;

                    return new ValueTask<bool>(statusCode != null && (int)statusCode > 500);
                },
            }).Build();
    }
}