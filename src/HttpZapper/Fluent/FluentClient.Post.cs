namespace HttpZapper.Fluent;

internal sealed partial class FluentClient
{
    public Task<HttpMsgResponse> Post(CancellationToken ct)
    {
        return http.Send(BuildRequest(HttpMethod.Post), ct);
    }

    public Task<HttpMsgResponse> Post<TRequest>(TRequest request, CancellationToken ct)
    {
        return http.Send(BuildRequest(request, HttpMethod.Post), ct);
    }

    public Task<HttpMsgResponse<TResponse>> Post<TRequest, TResponse>(TRequest request, CancellationToken ct)
    {
        return http.Send<TRequest, TResponse>(BuildRequest(request, HttpMethod.Post), ct);
    }

    public Task<HttpMsgResponse<TResponse>> Post<TResponse>(CancellationToken ct)
    {
        return http.Send<TResponse>(BuildRequest(HttpMethod.Post), ct);
    }
}