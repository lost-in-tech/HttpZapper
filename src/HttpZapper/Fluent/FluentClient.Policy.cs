namespace HttpZapper.Fluent;

internal sealed partial class FluentClient : 
    IHavePolicyKey,
    IHaveTimeout,
    IHaveRetry,
    IHaveCircuitBreaker
{
    private string? _policyKey = null;
    private TimeoutPolicy? _timeoutPolicy = null;
    private RetryPolicy? _retryPolicy = null;
    private CircuitBreakerPolicy? _circuitBreakerPolicy = null;
    
    public IHavePolicyKey PolicyKey(string key)
    {
        _policyKey = key;
        return this;
    }

    public IHaveTimeout Timeout(int timeoutInMs)
    {
        _timeoutPolicy = new TimeoutPolicy
        {
            TimeoutInMs = timeoutInMs
        };
        return this;
    }

    public IHaveRetry Retry(int retryCount, int delayInMs)
    {
        _retryPolicy = new RetryPolicy
        {
            RetryCount = retryCount,
            DelayOnRetryInMs = delayInMs
        };

        return this;
    }

    public IHaveRetry Retry(int retryCount)
    {
        return Retry(retryCount, 100);
    }

    public IHaveCircuitBreaker CircuitBreaker(CircuitBreakerPolicy policy)
    {
        _circuitBreakerPolicy = policy;
        return this;
    }
}