using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpZapper;

public static class IocSetup
{
    public static IServiceCollection AddHttpZapper(this IServiceCollection services, IConfiguration configuration, HttpZapperOptions? options = null)
    {
        options ??= new HttpZapperOptions();
        services.AddSingleton<IHttpMessageSerializer, HttpMessageSerializer>();
        services.AddScoped<HttpZapper>();
        services.AddScoped<HttpClientWithResiliency>();
        services.AddScoped<IHttpClientWrapper, HttpClientWrapper>();
        services.AddScoped<IHttpZapper, HttpZapperWithNoDup>();
        services.AddSingleton<IServiceSettings, ConfigBasedServiceSettingsProvider>();
        services.AddLogging();
        var config = new ServiceSettingsConfig();
        configuration.GetSection(options.ServiceSettingsSectionName).Bind(config);

        return services;
    }
}

public record HttpZapperOptions
{
    public string ServiceSettingsSectionName { get; init; } = "HttpZapper";
}