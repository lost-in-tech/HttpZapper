using System.Collections.Concurrent;

namespace HttpZapper;

internal sealed class HttpZapper(HttpZapperWithSerializer http) : IHttpZapper
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, object> _data = new();
    
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
        var key = BuildKey(request);

        if (_data.TryGetValue(key, out var value)) return (HttpMsgResponse)value;
        
        var semaphoreSlim = GetSemaphoreSlim(key);

        await semaphoreSlim.WaitAsync(ct);

        try
        {
            var rsp = await http.Send(request, ct);

            _data.AddOrUpdate(key, _ => rsp, (_, _) => rsp);
            
            return rsp;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }
    
    private async Task<HttpMsgResponse<TResponse>> SendWithLock<TResponse>(HttpMsgRequest request, CancellationToken ct)
    {
        var key = BuildKey(request);

        if (_data.TryGetValue(key, out var value)) return (HttpMsgResponse<TResponse>)value;
        
        var semaphoreSlim = GetSemaphoreSlim(key);

        await semaphoreSlim.WaitAsync(ct);

        try
        {
            if (_data.TryGetValue(key, out var existingValue)) return (HttpMsgResponse<TResponse>)existingValue;
            
            var rsp = await http.Send<TResponse>(request, ct);
            
            _data.AddOrUpdate(key, _ => rsp, (_, _) => rsp);

            return rsp;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }
    
    private bool ShouldSkipDuplicateCheck(HttpMsgRequest request)
        => request.SkipDuplicateCheck || request.Method != HttpMethod.Get;
    
    private string BuildKey(HttpMsgRequest request) => $"{request.Method}:{request.ServiceName}:{request.Path}";
    
    private SemaphoreSlim GetSemaphoreSlim(string key)
    {
        return _locks.GetOrAdd(key, s => new SemaphoreSlim(1, 1));
    }
}