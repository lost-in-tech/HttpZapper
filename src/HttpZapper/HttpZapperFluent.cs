using HttpZapper.Fluent;

namespace HttpZapper;

public static class HttpZapperFluent
{
    public static IHaveServiceName Service(this IHttpZapper source, string serviceName)
        => new FluentClient(source).ForService(serviceName);
    
    public static IHaveBaseUrl BaseUrl(this IHttpZapper source, string baseUrl)
        => new FluentClient(source).BaseUrl(baseUrl);
}