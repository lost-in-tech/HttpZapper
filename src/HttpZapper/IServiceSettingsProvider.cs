using Microsoft.Extensions.Options;

namespace HttpZapper;

public interface IServiceSettingsProvider
{
    ValueTask<ServiceSettings?> Get(string name, CancellationToken ct);
}

internal sealed class ConfigBasedServiceSettingsProvider
    : IServiceSettingsProvider
{
    private readonly Dictionary<string, ServiceSettings> _settings = new(StringComparer.OrdinalIgnoreCase);

    public ConfigBasedServiceSettingsProvider(IOptions<ServiceSettingsConfig> options)
    {
        foreach (var service in options.Value.Services)
        {
            _settings[service.Name] = service;
        }
    }
    
    public ValueTask<ServiceSettings?> Get(string name, CancellationToken ct)
    {
        if (_settings.TryGetValue(name, out var result))
        {
            return new ValueTask<ServiceSettings?>(result);
        }

        return ValueTask.FromResult((ServiceSettings?)null);
    }

    public int Priority => -1;
}

public class ServiceSettingsConfig
{
    public ServiceSettings[] Services { get; set; } = [];
}