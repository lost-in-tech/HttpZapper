using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace HttpZapper;

internal sealed class HttpClientWithResiliency(IHttpClientWrapper clientWrapper, ILogger<HttpZapper> logger)
{
    public Task<HttpResponseMessage> Send(HttpRequestMessage msg, ServiceSettings? settings, string? policyKey, CancellationToken ct)
    {
        return settings?.Policy == null 
            ? clientWrapper.Send(settings?.Name ?? string.Empty, msg, ct) 
            : WithPolicy(settings, msg, policyKey, ct);
    }

    private readonly ConcurrentDictionary<string, ResiliencePipeline<HttpResponseMessage>> _policies = new();
    private async Task<HttpResponseMessage> WithPolicy(ServiceSettings settings, HttpRequestMessage msg, string? policyKey, CancellationToken ct)
    {
        var key = BuildPolicyKey(settings, policyKey, msg.Method);
        
        var policy = _policies.GetOrAdd(key, _ => BuildResiliencePipeline(settings));

        try
        {
            return await policy.ExecuteAsync<HttpResponseMessage>(
                async (token) => await clientWrapper.Send(settings.Name, msg, token), ct);
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogError("Request timeout {service} {url} {method} {timeout}ms {msg}", settings.Name, msg.RequestUri, msg.Method, ex.Timeout.TotalMilliseconds, ex.Message);
            
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.RequestTimeout,
                ReasonPhrase = $"{settings.Name} Timeout"
            };
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogError("Circuit failed {service} {url} {method} {msg}", settings.Name, msg.RequestUri, msg.Method, ex.Message);
            
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.FailedDependency,
                ReasonPhrase = $"{settings.Name} Failed"
            };
        }
    }

    private string BuildPolicyKey(ServiceSettings settings, string? policyKey, HttpMethod method)
    {
        return string.IsNullOrWhiteSpace(policyKey)
            ? $"{settings.Name}:{method}:{settings.Policy!.RetryPolicy}:{settings.Policy!.TimeoutPolicy}:{settings.Policy!.CircuitBreakerPolicy}"
            : $"{settings.Name}:{policyKey}:{settings.Policy!.RetryPolicy}:{settings.Policy!.TimeoutPolicy}:{settings.Policy!.CircuitBreakerPolicy}";
    }
    
    private ResiliencePipeline<HttpResponseMessage> BuildResiliencePipeline(ServiceSettings settings)
    {
        var policy = settings.Policy!;

        var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>();

        if (policy.RetryPolicy != null)
        {
            pipeline.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                Delay = TimeSpan.FromMilliseconds(policy.RetryPolicy.DelayOnRetryInMs),
                UseJitter = true,
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = policy.RetryPolicy.RetryCount,
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
            });
        }

        if (policy.TimeoutPolicy != null)
        {
            pipeline
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMilliseconds(policy.TimeoutPolicy.TimeoutInMs),
                });
        }

        if (policy.CircuitBreakerPolicy != null)
        {
            pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = arg =>
                {
                    var statusCode = arg.Outcome.Result?.StatusCode;

                    return new ValueTask<bool>(statusCode != null && (int)statusCode > 500);
                },
                BreakDuration = TimeSpan.FromSeconds(policy.CircuitBreakerPolicy.BreakDurationInSeconds),
                FailureRatio = policy.CircuitBreakerPolicy.FailureRatio,
                MinimumThroughput = policy.CircuitBreakerPolicy.MinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(policy.CircuitBreakerPolicy.SamplingDurationInSeconds)
            });
        }

        return pipeline.Build();
    }
}