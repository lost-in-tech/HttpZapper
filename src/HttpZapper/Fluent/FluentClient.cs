using System.Net;

namespace HttpZapper.Fluent;

internal sealed partial class FluentClient(IHttpZapper http) : 
    IHaveClient, 
    IHaveServiceName,
    IHaveBaseUrl,
    IHavePath, 
    IHaveOnFailure,
    IHaveSkipDuplicateCheck,
    IHaveProblemDetailsType
{
    private string _serviceName = string.Empty;
    private string _path = string.Empty;
    private Func<HttpStatusCode, Stream, IHttpMessageSerializer, CancellationToken, Task>? _onFailure = null;
    private bool? _skipDuplicateCheck = null;
    private Type? _problemDetailsType = null;
    
    public IHaveServiceName ForService(string name)
    {
        _serviceName = name;
        return this;
    }

    public IHavePath Path(string path)
    {
        _path = path;
        return this;
    }

    public IHaveOnFailure OnFailure(Func<HttpStatusCode, Stream, IHttpMessageSerializer, CancellationToken, Task> onFailure)
    {
        _onFailure = onFailure;
        return this;
    }

    public IHaveSkipDuplicateCheck SkipDuplicateCheck(bool value)
    {
        _skipDuplicateCheck = value;
        return this;
    }

    

    private string? _baseUrl = null;
    public IHaveBaseUrl BaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return this;
    }

    public IHaveProblemDetailsType ProblemDetailsType<T>() 
        => ProblemDetailsType(typeof(T));

    public IHaveProblemDetailsType ProblemDetailsType(Type type)
    {
        _problemDetailsType = type;

        return this;
    }
}