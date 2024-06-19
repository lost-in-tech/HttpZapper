namespace HttpZapper;

internal sealed class HttpClientWithResiliency(
    IHttpClientWrapper clientWrapper, 
    IHttpResiliencyPipeline resiliencyPipeline)
{
    public Task<HttpResponseMessage> Send(
        HttpRequestMessage msg, 
        ServiceSettings? settings, 
        CancellationToken ct)
    {
        return settings?.Policy == null 
            ? clientWrapper.Send(settings?.Name ?? string.Empty, msg, ct) 
            : resiliencyPipeline.Execute(settings, msg, ct);
    }
}