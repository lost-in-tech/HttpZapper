using System.Net;

namespace HttpZapper.Tests.Fakes;


internal class FakeHttpClientWrapper : IHttpClientWrapper
{
    private readonly HttpStatusCode _statusCode;
    private readonly int _delay;
    private int invoked = 0;

    public FakeHttpClientWrapper(HttpStatusCode statusCode, int delay)
    {
        _statusCode = statusCode;
        _delay = delay;
    }
    
    public async Task<HttpResponseMessage> Send(string serviceName, HttpRequestMessage msg, CancellationToken ct)
    {
        invoked++;
        if (_delay > 0) await Task.Delay(_delay, ct);
        return new HttpResponseMessage
        {
            StatusCode = _statusCode
        };
    }

    public int TotalInvoked => invoked;
}
