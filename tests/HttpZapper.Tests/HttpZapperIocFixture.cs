using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace HttpZapper.Tests;

public class HttpZapperIocFixture
{
    private IServiceProvider _sp;

    public HttpZapperIocFixture()
    {
        var sc = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        var sb = Substitute.For<IOptions<ServiceSettingsConfig>>();
        sb.Value.Returns(new ServiceSettingsConfig
        {
            Services = new Dictionary<string, ServiceSettings>
            {
                [TestConstants.ServiceNames.ApiBooks] = new()
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
                            BreakDurationInSeconds = 5,
                            SamplingDurationInSeconds = 30 
                        }
                    }
                }
            }
        });

        sc.AddHttpZapper(config);
        sc.RemoveAll<IHttpClientWrapper>();
        sc.AddScoped<IHttpClientWrapper>(c => Substitute.For<IHttpClientWrapper>());
        sc.RemoveAll<IOptions<ServiceSettingsConfig>>();
        sc.AddScoped<IOptions<ServiceSettingsConfig>>(_ => sb);
        
        _sp = sc.BuildServiceProvider();
    }

    public IServiceScope Scope() => _sp.CreateScope();
}