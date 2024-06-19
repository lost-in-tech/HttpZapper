using System.ComponentModel.DataAnnotations;

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
    public static CircuitBreakerPolicy Default = new()
    {
        FailureRatio = 0.1,
        MinimumThroughput = 100,
        BreakDurationInMs = 500,
        SamplingDurationInMs = 3000
    };
    
    /// <summary>
    /// Must be a number between 0 and 1. 0.1 means failure ratio is 10%
    /// </summary>
    [Range(0,1)]
    public double FailureRatio { get; init; }
    public int SamplingDurationInMs { get; init; }
    public int MinimumThroughput { get; init; }
    public int BreakDurationInMs { get; init; }
}