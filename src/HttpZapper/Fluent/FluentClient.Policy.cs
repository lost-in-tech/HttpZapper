namespace HttpZapper.Fluent;

internal sealed partial class FluentClient :
    IHaveTimeout,
    IHaveRetry
{
    private TimeoutPolicy? _timeoutPolicy = null;
    private RetryPolicy? _retryPolicy = null;
    
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
}