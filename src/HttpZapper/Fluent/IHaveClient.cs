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

public interface IHavePath : ISendMessage, ICollectVersion, ICollectQueryStrings, ICollectHeaders, ICollectOnFailure, ICollectDuplicateCheck, ICollectTimeout, ICollectRetry
{
}

public interface ICollectVersion
{
    IHaveVersion Version(Version version);
}

public interface IHaveVersion : ISendMessage, ICollectQueryStrings, ICollectHeaders, ICollectOnFailure, ICollectDuplicateCheck, ICollectTimeout, ICollectRetry
{
}

public interface ICollectQueryStrings
{
    IHaveQueryStrings QueryString(string name, string? value);
    IHaveQueryStrings QueryStrings(IEnumerable<(string Name, string? Value)> values);
    IHaveQueryStrings QueryStrings<T>(T? value) where T : class;
}

public interface IHaveQueryStrings : ISendMessage, ICollectQueryStrings, ICollectHeaders, ICollectOnFailure, ICollectDuplicateCheck, ICollectTimeout, ICollectRetry
{
    
}

public interface ICollectHeaders
{
    IHaveHeaders Header(string name, string? value);
    IHaveHeaders Headers(IEnumerable<(string name, string? value)> values);

    IHaveHeaders Cookie(string name, string? value);
    IHaveHeaders Cookies(IEnumerable<(string name, string? value)> values);

    IHaveHeaders OAuthBearerToken(string token);

    /// <summary>
    /// create credentials using username and password and base64encode the value and the pass in header
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    IHaveHeaders BasicAuth(string username, string password);
    
    /// <summary>
    /// pass the credentials.
    /// </summary>
    /// <param name="credentials"></param>
    /// <returns></returns>
    IHaveHeaders BasicAuth(string credentials);
}

public interface IHaveHeaders : ISendMessage, ICollectHeaders, ICollectOnFailure, ICollectDuplicateCheck, ICollectTimeout, ICollectRetry
{
    
}

public interface ICollectOnFailure
{
    IHaveOnFailure OnFailure(Func<HttpStatusCode, Stream, IHttpMessageSerializer, CancellationToken, Task> onFailure);
}

public interface IHaveOnFailure : ISendMessage, ICollectDuplicateCheck, ICollectTimeout, ICollectRetry
{
}

public interface ICollectDuplicateCheck
{
    IHaveSkipDuplicateCheck SkipDuplicateCheck(bool value);
}

public interface IHaveSkipDuplicateCheck : ISendMessage, ICollectTimeout, ICollectRetry
{
}


public interface ICollectTimeout
{
    IHaveTimeout Timeout(int timeoutInMs);
}
public interface IHaveTimeout : ISendMessage, ICollectRetry
{}


public interface ICollectRetry
{
    IHaveRetry Retry(int retryCount, int delayInMs);
    IHaveRetry Retry(int retryCount);
}

public interface IHaveRetry : ISendMessage
{
}


public interface ISendMessage
{
    Task<HttpMsgResponse> Get(CancellationToken ct = default);
    Task<HttpMsgResponse<TResponse>> Get<TResponse>(CancellationToken ct = default);
    
    
    Task<HttpMsgResponse> Delete(CancellationToken ct = default);
    Task<HttpMsgResponse<TResponse>> Delete<TResponse>(CancellationToken ct = default);
    
    Task<HttpMsgResponse> Post(CancellationToken ct = default);
    Task<HttpMsgResponse> Post<TRequest>(TRequest request, CancellationToken ct = default);
    Task<HttpMsgResponse<TResponse>> Post<TRequest, TResponse>(TRequest request, CancellationToken ct = default);
    Task<HttpMsgResponse<TResponse>> Post<TResponse>(CancellationToken ct = default);
    
    
    
    Task<HttpMsgResponse> Put(CancellationToken ct = default);
    Task<HttpMsgResponse> Put<TRequest>(TRequest request, CancellationToken ct = default);
    Task<HttpMsgResponse<TResponse>> Put<TRequest, TResponse>(TRequest request, CancellationToken ct = default);
    Task<HttpMsgResponse<TResponse>> Put<TResponse>(CancellationToken ct = default);
}