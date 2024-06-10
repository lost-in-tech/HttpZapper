using System.Text.Json;
using System.Text.Json.Serialization;

namespace HttpZapper;

public interface IHttpMessageSerializer
{
    string Serialize<T>(T? value);
    ValueTask<T?> Deserialize<T>(Stream stream, CancellationToken ct);
    string MediaType { get; }
}

internal sealed class HttpMessageSerializer : IHttpMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    public HttpMessageSerializer()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new JsonStringEnumConverter());
        _options.WriteIndented = false;
        _options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        _options.PropertyNameCaseInsensitive = true;
        _options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
    
    public string Serialize<T>(T? value)
    {
        return JsonSerializer.Serialize(value, _options);
    }

    public ValueTask<T?> Deserialize<T>(Stream stream, CancellationToken ct)
    {
        return JsonSerializer.DeserializeAsync<T>(stream, _options, ct);
    }

    public string MediaType => "application/json";
}