namespace HttpZapper.Fluent;

internal sealed partial class FluentClient
{
    public Task<HttpMsgResponse> Get(CancellationToken ct)
    {
        return http.Send(BuildRequest(HttpMethod.Get), ct);
    }

    public Task<HttpMsgResponse<TResponse>> Get<TResponse>(CancellationToken ct)
    {
        return http.Send<TResponse>(BuildRequest(HttpMethod.Get), ct);
    }
}