using Microsoft.Extensions.Options;

namespace HttpZapper;

public interface IServiceSettings
{
    ValueTask<ServiceSettings?> Get(string name, CancellationToken ct);
}

internal sealed class ConfigBasedServiceSettingsProvider(IOptions<ServiceSettingsConfig> options) : IServiceSettings
{
    public ValueTask<ServiceSettings?> Get(string name, CancellationToken ct)
    {
        if (options.Value.Services.TryGetValue(name, out var result))
        {
            return  new ValueTask<ServiceSettings?>(result);
        }

        return ValueTask.FromResult((ServiceSettings?)null);
    }

    public int Priority => -1;
}

public class ServiceSettingsConfig
{
    public Dictionary<string, ServiceSettings> Services { get; set; } = new();
}