namespace HttpZapper.Fluent;

internal sealed partial class FluentClient
{

    public Task<HttpMsgResponse> Delete(CancellationToken ct)
    {
        return http.Send(BuildRequest(HttpMethod.Delete), ct);
    }

    public Task<HttpMsgResponse<TResponse>> Delete<TResponse>(CancellationToken ct)
    {
        return http.Send<TResponse>(BuildRequest(HttpMethod.Delete), ct);
    }
}