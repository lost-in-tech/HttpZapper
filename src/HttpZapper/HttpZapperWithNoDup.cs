using System.Collections.Concurrent;

namespace HttpZapper;

internal sealed class HttpZapperWithNoDup(HttpZapper http) : IHttpZapper
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    
    public Task<HttpMsgResponse> Send(HttpMsgRequest request, CancellationToken ct)
    {
        return ShouldSkipDuplicateCheck(request) 
            ? http.Send(request, ct) 
            : SendWithLock(request, ct);
    }

    public Task<HttpMsgResponse> Send<TRequest>(HttpMsgRequest<TRequest> request, CancellationToken ct)
    {
        return http.Send(request, ct);
    }

    public Task<HttpMsgResponse<TResponse>> Send<TRequest, TResponse>(HttpMsgRequest<TRequest> request, CancellationToken ct)
    {
        return http.Send<TRequest,TResponse>(request, ct);
    }

    public Task<HttpMsgResponse<TResponse>> Send<TResponse>(HttpMsgRequest request, CancellationToken ct)
    {
        return ShouldSkipDuplicateCheck(request) 
            ? http.Send<TResponse>(request, ct) 
            : SendWithLock<TResponse>(request, ct);
    }

    private async Task<HttpMsgResponse> SendWithLock(HttpMsgRequest request, CancellationToken ct)
    {
        var semaphoreSlim = GetSemaphoreSlim(request);

        await semaphoreSlim.WaitAsync(ct);

        try
        {
            return await http.Send(request, ct);
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }
    
    private async Task<HttpMsgResponse<TResponse>> SendWithLock<TResponse>(HttpMsgRequest request, CancellationToken ct)
    {
        var semaphoreSlim = GetSemaphoreSlim(request);

        await semaphoreSlim.WaitAsync(ct);

        try
        {
            return await http.Send<TResponse>(request, ct);
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }
    
    private bool ShouldSkipDuplicateCheck(HttpMsgRequest request)
        => request.SkipDuplicateCheck || request.Method != HttpMethod.Get;
    
    private SemaphoreSlim GetSemaphoreSlim(HttpMsgRequest request)
    {
        var key = $"{request.Method}:{request.ServiceName}:{request.Path}";
        
        return _locks.GetOrAdd(key, s => new SemaphoreSlim(1, 1));
    }
}