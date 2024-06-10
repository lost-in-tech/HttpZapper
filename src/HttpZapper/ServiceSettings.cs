namespace HttpZapper;

public record ServiceSettings
{
    public required string Name { get; init; }
    public required string BaseUrl { get; init; }
    public ServicePolicy? Policy { get; init; }
}

public record ServicePolicy
{
    public CircuitBreakerPolicy? CircuitBreakerPolicy { get; init; }
    public RetryPolicy? RetryPolicy { get; init; }
    public TimeoutPolicy? TimeoutPolicy { get; init; }
}

public record RetryPolicy
{
    public static RetryPolicy Default => new RetryPolicy
    {
        RetryCount = 1,
        DelayOnRetryInMs = 100
    };
    
    public int RetryCount { get; init; }
    public int DelayOnRetryInMs { get; init; }
}

public record TimeoutPolicy
{
    public static TimeoutPolicy Default => new TimeoutPolicy
    {
        TimeoutInMs = 1000
    };
    
    public int TimeoutInMs { get; init; }
}

public record CircuitBreakerPolicy
{
    public static CircuitBreakerPolicy Default = new CircuitBreakerPolicy
    {
        FailureRatio = 0.1,
        MinimumThroughput = 100,
        BreakDurationInSeconds = 5,
        SamplingDurationInSeconds = 30
    };
    
    public double FailureRatio { get; init; }
    public int SamplingDurationInSeconds { get; init; }
    public int MinimumThroughput { get; init; }
    public int BreakDurationInSeconds { get; init; }
}