namespace HttpZapper;

public interface IHttpClientWrapper
{
    Task<HttpResponseMessage> Send(string serviceName, HttpRequestMessage msg, CancellationToken ct);
}

internal sealed class HttpClientWrapper(IHttpClientFactory httpClientFactory) : IHttpClientWrapper
{
    public Task<HttpResponseMessage> Send(string serviceName, HttpRequestMessage msg, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(serviceName);
        return client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, ct);
    }
}