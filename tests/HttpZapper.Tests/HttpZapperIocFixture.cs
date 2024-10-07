using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace HttpZapper.Tests;

public class HttpZapperIocFixture
{
    private readonly IServiceProvider _sp;

    public HttpZapperIocFixture()
    {
        var sc = BuildServiceCollection();
        
        _sp = sc.BuildServiceProvider();
    }

    public IServiceCollection BuildServiceCollection()
    {
        var sc = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var sb = Substitute.For<IOptions<ServiceSettingsConfig>>();
        sb.Value.Returns(new ServiceSettingsConfig
        {
            Services =
            [
                new()
                {
                    Name = TestConstants.ServiceNames.ApiBooks,
                    BaseUrl = "http://api-books.prod.com.au",
                    Policy = new ServicePolicy
                    {
                        RetryPolicy = new RetryPolicy
                        {
                            RetryCount = 1,
                            DelayOnRetryInMs = 100
                        },
                        TimeoutPolicy = new TimeoutPolicy
                        {
                            TimeoutInMs = 1000
                        },
                        CircuitBreakerPolicy = new CircuitBreakerPolicy
                        {
                            FailureRatio = 0.1,
                            MinimumThroughput = 100,
                            BreakDurationInMs = 500,
                            SamplingDurationInMs = 3000 
                        }
                    }
                }
            ]
        });

        sc.AddHttpZapper(config);
        sc.RemoveAll<IHttpClientWrapper>();
        sc.AddScoped<IHttpClientWrapper>(c => Substitute.For<IHttpClientWrapper>());
        sc.RemoveAll<IOptions<ServiceSettingsConfig>>();
        sc.AddScoped<IOptions<ServiceSettingsConfig>>(_ => sb);

        return sc;
    }
    
    public async Task WithScope(Func<IServiceProvider, Task> action)
    {
        using var scope = _sp.CreateScope();

        await action.Invoke(scope.ServiceProvider);
    }

    public IServiceScope Scope() => _sp.CreateScope();
}