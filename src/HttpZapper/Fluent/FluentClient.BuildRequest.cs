using System.Text;

namespace HttpZapper.Fluent;

internal sealed partial class FluentClient
{
    private HttpMsgRequest BuildRequest(HttpMethod method)
    {
        return new HttpMsgRequest
        {
            ServiceName = _serviceName,
            Path = BuildPath(),
            Method = method,
            Headers = _headers,
            OnFailure = _onFailure,
            PolicyKey = _policyKey,
            SkipDuplicateCheck = _skipDuplicateCheck ?? false,
            BaseUrl = _baseUrl,
            Policy = BuildPolicy(),
        };
    }

    private HttpMsgRequest<TRequest> BuildRequest<TRequest>(TRequest request, HttpMethod method)
    {
        return new HttpMsgRequest<TRequest>
        {
            ServiceName = _serviceName,
            Path = BuildPath(),
            Method = method,
            Headers = _headers,
            OnFailure = _onFailure,
            PolicyKey = _policyKey,
            SkipDuplicateCheck = _skipDuplicateCheck ?? false,
            BaseUrl = _baseUrl,
            Policy = BuildPolicy(),
            Content = request
        };
    }

    private ServicePolicy? BuildPolicy()
    {
        if (_retryPolicy == null && _timeoutPolicy == null && _circuitBreakerPolicy == null) return null;

        return new ServicePolicy
        {
            RetryPolicy = _retryPolicy,
            TimeoutPolicy = _timeoutPolicy,
            CircuitBreakerPolicy = _circuitBreakerPolicy
        };
    }

    private const char CharAmp = '&';
    private const char CharEq = '=';
    private const char CharQs = '?';
    private string BuildPath()
    {
        if (_queryStrings == null) return _path;
        bool hasQs = _path.IndexOf(CharQs) != -1;

        var sb = new StringBuilder();
        sb.Append(_path);
        sb.Append(hasQs ? CharAmp : CharQs);
        foreach (var queryString in _queryStrings)
        {
            if(queryString.Value == null) continue;
            sb.Append(queryString.Name)
                .Append(CharEq)
                .Append(Uri.EscapeDataString(queryString.Value))
                .Append(CharAmp);
        }

        return sb.ToString().TrimEnd(CharAmp);
    }
}