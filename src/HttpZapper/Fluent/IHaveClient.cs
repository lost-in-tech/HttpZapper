using System.Net;

namespace HttpZapper.Fluent;

public interface IHaveClient : ICollectServiceName, ICollectBaseUrl
{
    
}

public interface ICollectServiceName
{
    IHaveServiceName ForService(string name);
}

public interface IHaveServiceName : ICollectPath
{
    
}

public interface ICollectBaseUrl
{
    IHaveBaseUrl BaseUrl(string baseUrl);
}

public interface IHaveBaseUrl: ICollectPath
{
}

public interface ICollectPath
{
    IHavePath Path(string path);
}

public interface IHavePath : ISendMessage, ICollectQueryStrings, ICollectHeaders, ICollectOnFailure, ICollectDuplicateCheck, ICollectPolicyKey, ICollectTimeout, ICollectRetry, ICollectCircuitBreaker
{
}

public interface ICollectQueryStrings
{
    IHaveQueryStrings QueryString(string name, string? value);
    IHaveQueryStrings QueryStrings(IEnumerable<(string Name, string? Value)> values);
}

public interface IHaveQueryStrings : ISendMessage, ICollectHeaders, ICollectOnFailure, ICollectDuplicateCheck, ICollectPolicyKey, ICollectTimeout, ICollectRetry, ICollectCircuitBreaker
{
    
}

public interface ICollectHeaders
{
    IHaveHeaders Header(string name, string? value);
    IHaveHeaders Headers(IEnumerable<(string name, string? value)> values);
}

public interface IHaveHeaders : ISendMessage, ICollectOnFailure, ICollectDuplicateCheck, ICollectPolicyKey, ICollectTimeout, ICollectRetry, ICollectCircuitBreaker
{
    
}

public interface ICollectOnFailure
{
    IHaveOnFailure OnFailure(Func<HttpStatusCode, Stream, IHttpMessageSerializer, CancellationToken, Task> onFailure);
}

public interface IHaveOnFailure : ISendMessage, ICollectDuplicateCheck, ICollectPolicyKey, ICollectTimeout, ICollectRetry, ICollectCircuitBreaker
{
}

public interface ICollectDuplicateCheck
{
    IHaveSkipDuplicateCheck SkipDuplicateCheck(bool value);
}

public interface IHaveSkipDuplicateCheck : ISendMessage, ICollectPolicyKey, ICollectTimeout, ICollectRetry, ICollectCircuitBreaker
{
}

public interface ICollectPolicyKey
{
    /// <summary>
    /// Generally for different http method a new policy used. For example for same timeout, retry and circuit-breaker values two different instance
    /// of policies will be used for GET and POST. But if you like to use same policy for get and post pass policy key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    IHavePolicyKey PolicyKey(string key);
}

public interface IHavePolicyKey : ISendMessage, ICollectTimeout, ICollectRetry, ICollectCircuitBreaker
{
}

public interface ICollectTimeout
{
    IHaveTimeout Timeout(int timeoutInMs);
}
public interface IHaveTimeout : ISendMessage, ICollectRetry, ICollectCircuitBreaker
{}


public interface ICollectRetry
{
    IHaveRetry Retry(int retryCount, int delayInMs);
    IHaveRetry Retry(int retryCount);
}

public interface IHaveRetry : ISendMessage, ICollectCircuitBreaker
{
}

public interface ICollectCircuitBreaker
{
    IHaveCircuitBreaker CircuitBreaker(CircuitBreakerPolicy policy);
}

public interface IHaveCircuitBreaker : ISendMessage
{
    
}


public interface ISendMessage
{
    Task<HttpMsgResponse> Get(CancellationToken ct);
    Task<HttpMsgResponse<TResponse>> Get<TResponse>(CancellationToken ct);
    
    
    Task<HttpMsgResponse> Delete(CancellationToken ct);
    Task<HttpMsgResponse<TResponse>> Delete<TResponse>(CancellationToken ct);
    
    Task<HttpMsgResponse> Post(CancellationToken ct);
    Task<HttpMsgResponse> Post<TRequest>(TRequest request, CancellationToken ct);
    Task<HttpMsgResponse<TResponse>> Post<TRequest, TResponse>(TRequest request, CancellationToken ct);
    Task<HttpMsgResponse<TResponse>> Post<TResponse>(CancellationToken ct);
    
    
    
    Task<HttpMsgResponse> Put(CancellationToken ct);
    Task<HttpMsgResponse> Put<TRequest>(TRequest request, CancellationToken ct);
    Task<HttpMsgResponse<TResponse>> Put<TRequest, TResponse>(TRequest request, CancellationToken ct);
    Task<HttpMsgResponse<TResponse>> Put<TResponse>(CancellationToken ct);
}