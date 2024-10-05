using System.Text;

namespace HttpZapper.Fluent;

internal sealed partial class FluentClient
{
    private string? BuildCookieValue()
    {
        if (_cookies == null || _cookies.Count == 0) return null;
        if (_cookies.Count == 1)
        {
            var cookie = _cookies[0];
            if (cookie.Value == null) return null;
            return $"{_cookies[0].Name}={_cookies[0].Value};";
        }

        var sb = new StringBuilder();

        foreach (var cookie in _cookies)
        {
            if(cookie.Value == null) continue;
            
            sb.AppendFormat("{0}={1};", cookie.Name, cookie.Value);
        }
        
        return sb.ToString();
    }
    
    private HttpMsgRequest BuildRequest(HttpMethod method)
    {
        var cookieVal = BuildCookieValue();

        if (!string.IsNullOrWhiteSpace(cookieVal))
        {
            _headers ??= new();

            _headers.Add(("Cookie", cookieVal));
        }
        
        return new HttpMsgRequest
        {
            Version = _version,
            ServiceName = _serviceName,
            Path = BuildPath(),
            Method = method,
            Headers = _headers,
            OnFailure = _onFailure,
            SkipDuplicateCheck = _skipDuplicateCheck ?? false,
            BaseUrl = _baseUrl,
            Policy = BuildPolicy(),
            ProblemDetailsType = _problemDetailsType
        };
    }

    private HttpMsgRequest<TRequest> BuildRequest<TRequest>(TRequest request, HttpMethod method)
    {
        var cookieVal = BuildCookieValue();

        if (!string.IsNullOrWhiteSpace(cookieVal))
        {
            _headers ??= new();

            _headers.Add(("Cookie", cookieVal));
        }
        
        return new HttpMsgRequest<TRequest>
        {
            Version = _version,
            ServiceName = _serviceName,
            Path = BuildPath(),
            Method = method,
            Headers = _headers,
            OnFailure = _onFailure,
            SkipDuplicateCheck = _skipDuplicateCheck ?? false,
            BaseUrl = _baseUrl,
            Policy = BuildPolicy(),
            Content = request,
            ProblemDetailsType = _problemDetailsType
        };
    }

    private ServicePolicy? BuildPolicy()
    {
        if (_retryPolicy == null && _timeoutPolicy == null) return null;

        return new ServicePolicy
        {
            RetryPolicy = _retryPolicy,
            TimeoutPolicy = _timeoutPolicy
        };
    }

    private const char CharAmp = '&';
    private const char CharEq = '=';
    private const char CharQs = '?';
    private string 
        BuildPath()
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