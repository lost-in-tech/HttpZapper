using System.Net;

namespace HttpZapper;

public interface IHttpZapper
{
    Task<HttpMsgResponse> Send(HttpMsgRequest request, CancellationToken ct);
    Task<HttpMsgResponse> Send<TRequest>(HttpMsgRequest<TRequest> request, CancellationToken ct);
    Task<HttpMsgResponse<TResponse>> Send<TRequest,TResponse>(HttpMsgRequest<TRequest> request, CancellationToken ct);
    Task<HttpMsgResponse<TResponse>> Send<TResponse>(HttpMsgRequest request, CancellationToken ct);
}

public record HttpMsgRequest
{
    public required string ServiceName { get; init; }
    public required string Path { get; init; }
    public required HttpMethod Method { get; init; }
    public IEnumerable<(string Name, string? Value)>? Headers { get; init; }
    public Func<HttpStatusCode, Stream, IHttpMessageSerializer, CancellationToken, Task>? OnFailure { get; init; }
    
    public bool SkipDuplicateCheck { get; init; }
    
     
    /// <summary>
    /// Base url should be taken from service settings. If base url provided then we will use this baseurl
    /// </summary>
    public string? BaseUrl { get; init; }
    
    /// <summary>
    /// Override service policy for this request
    /// </summary>
    public ServicePolicy? Policy { get; init; }
    
    /// <summary>
    /// If you need to pass specific serializer for this request. e.g its an xml or need to support specific type. But you want to apply only for this request.
    /// </summary>
    public IHttpMessageSerializer? Serializer { get; init; }
}

public record HttpMsgRequest<T> : HttpMsgRequest
{
    public required T Content { get; init; }
}

public record HttpMsgResponse
{
    public bool IsSuccessStatusCode { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public required (string Name, string Value)[] Headers { get; init; }
}

public record HttpMsgResponse<T> : HttpMsgResponse
{
    public T? Content { get; init; }
}