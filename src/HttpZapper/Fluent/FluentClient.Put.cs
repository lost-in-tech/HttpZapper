namespace HttpZapper.Fluent;

internal sealed partial class FluentClient
{
    public Task<HttpMsgResponse> Put(CancellationToken ct)
    {
        return http.Send(BuildRequest(HttpMethod.Put), ct);
    }

    public Task<HttpMsgResponse> Put<TRequest>(TRequest request, CancellationToken ct)
    {
        return http.Send(BuildRequest(request, HttpMethod.Put), ct);
    }

    public Task<HttpMsgResponse<TResponse>> Put<TRequest, TResponse>(TRequest request, CancellationToken ct)
    {
        return http.Send<TRequest, TResponse>(BuildRequest(request, HttpMethod.Put), ct);
    }

    public Task<HttpMsgResponse<TResponse>> Put<TResponse>(CancellationToken ct)
    {
        return http.Send<TResponse>(BuildRequest(HttpMethod.Put), ct);
    }
}