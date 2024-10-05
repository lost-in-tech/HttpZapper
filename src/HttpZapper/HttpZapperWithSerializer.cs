using System.Text;

namespace HttpZapper;

internal sealed class HttpZapperWithSerializer(
    IServiceSettings serviceSettings,
    IHttpMessageSerializer serializer,
    IEnumerable<IHttpMsgRequestFilter> filters,
    HttpClientWithResiliency http)
{
    public async Task<HttpMsgResponse> Send(
        HttpMsgRequest request, 
        CancellationToken ct = default)
    {
        var settings = await GetServiceSettings(request, ct);

        using var msg = BuildRequestMessage(request, settings);

        var rsp = await http.Send(msg, settings, ct);

        return await BuildResponseMessage(request, rsp, ct);
    }

    public async Task<HttpMsgResponse> Send<TRequest>(
        HttpMsgRequest<TRequest> request, 
        CancellationToken ct = default)
    {
        var settings = await GetServiceSettings(request, ct);

        using var msg = BuildRequestMessageWithContent(request, settings);

        var rsp = await http.Send(msg, settings, ct);

        return await BuildResponseMessage(request, rsp, ct);
    }

    public async Task<HttpMsgResponse<TResponse>> Send<TRequest, TResponse>(
        HttpMsgRequest<TRequest> request,
        CancellationToken ct = default)
    {
        var settings = await GetServiceSettings(request, ct);

        using var msg = BuildRequestMessageWithContent(request, settings);

        var rsp = await http.Send(msg, settings, ct);

        return await BuildResponseMessageWithContent<TResponse>(request, rsp, ct);
    }

    public async Task<HttpMsgResponse<TResponse>> Send<TResponse>(
        HttpMsgRequest request,
        CancellationToken ct = default)
    {
        var settings = await GetServiceSettings(request, ct);

        using var msg = BuildRequestMessage(request, settings);

        var rsp = await http.Send(msg, settings, ct);

        return await BuildResponseMessageWithContent<TResponse>(request, rsp, ct);
    }

    private async ValueTask<ServiceSettings?> GetServiceSettings(
        HttpMsgRequest request, 
        CancellationToken ct)
    {
        ServiceSettings? settings = await serviceSettings.Get(request.ServiceName, ct);

        if (request.Policy == null
            || (request.Policy.RetryPolicy == null
                && request.Policy.TimeoutPolicy == null
                && request.Policy.CircuitBreakerPolicy == null)) return settings;

        if (settings == null)
        {
            return new ServiceSettings
            {
                Name = request.ServiceName,
                BaseUrl = string.Empty,
                Policy = request.Policy
            };
        }

        return settings with
        {
            Policy = new ServicePolicy
            {
                RetryPolicy = request.Policy?.RetryPolicy ?? settings.Policy?.RetryPolicy,
                TimeoutPolicy = request.Policy?.TimeoutPolicy ?? settings.Policy?.TimeoutPolicy,
                CircuitBreakerPolicy = request.Policy?.CircuitBreakerPolicy ?? settings?.Policy?.CircuitBreakerPolicy
            }
        };
    }

    private IEnumerable<(string Name, string Value)> ReadResponseHeaders(HttpResponseMessage msg)
    {
        foreach (var (key, values) in msg.Headers)
        {
            var value = string.Join(",", values);

            yield return (Name: key, Value: value);
        }
    }


    private HttpRequestMessage BuildRequestMessage(
        HttpMsgRequest request, 
        ServiceSettings? service)
    {
        foreach (var filter in filters)
        {
            request = filter.Filter(request);
        }
        
        var baseUrl = request.BaseUrl;

        if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = service?.BaseUrl;

        var msg = new HttpRequestMessage(request.Method, $"{baseUrl}/{request.Path.TrimStart('/')}");
        
        if(request.Version != null) msg.Version = request.Version;
        
        if (request.Headers != null)
        {
            foreach (var header in request.Headers)
            {
                if (header.Value == null) continue;

                msg.Headers.Add(header.Name, header.Value);
            }
        }

        return msg;
    }

    private HttpRequestMessage BuildRequestMessageWithContent<TRequest>(
        HttpMsgRequest<TRequest> request,
        ServiceSettings? service)
    {
        var msg = BuildRequestMessage(request, service);

        var currentSerializer = request.Serializer ?? serializer;
        
        msg.Content = new StringContent(currentSerializer.Serialize(request.Content),
            Encoding.UTF8, currentSerializer.MediaType);

        return msg;
    }

    private async Task<HttpMsgResponse<TResponse>> BuildResponseMessageWithContent<TResponse>(
        HttpMsgRequest request,
        HttpResponseMessage rsp, 
        CancellationToken ct)
    {
        var isSuccessStatusCode = rsp.IsSuccessStatusCode;

        var content = default(TResponse);

        var currentSerializer = request.Serializer ?? serializer;

        object? problemDetails = null;
        if (isSuccessStatusCode)
        {
            await using var sr = await rsp.Content.ReadAsStreamAsync(ct);

            content = await currentSerializer.Deserialize<TResponse>(sr, ct);
        }
        else if (request.OnFailure != null)
        {
            await using var sr = await rsp.Content.ReadAsStreamAsync(ct);

            await request.OnFailure.Invoke(rsp.StatusCode, sr, currentSerializer, ct);
        }
        else if (request.ProblemDetailsType != null)
        {
            await using var sr = await rsp.Content.ReadAsStreamAsync(ct);

            problemDetails = serializer.Deserialize(sr, request.ProblemDetailsType, ct);
        }

        return new HttpMsgResponse<TResponse>
        {
            Headers = ReadResponseHeaders(rsp).ToArray(),
            IsSuccessStatusCode = isSuccessStatusCode,
            StatusCode = rsp.StatusCode,
            Content = content,
            ProblemDetails = problemDetails
        };
    }

    private async ValueTask<HttpMsgResponse> BuildResponseMessage(
        HttpMsgRequest request,
        HttpResponseMessage rsp,
        CancellationToken ct)
    {
        var isSuccessStatusCode = rsp.IsSuccessStatusCode;

        if (isSuccessStatusCode)
        {
            return new HttpMsgResponse
            {
                Headers = ReadResponseHeaders(rsp).ToArray(),
                IsSuccessStatusCode = isSuccessStatusCode,
                StatusCode = rsp.StatusCode
            };
        }
        
        object? problemDetails = null;
        if (request.OnFailure != null)
        {
            var currentSerializer = request.Serializer ?? serializer;
            
            await using var sr = await rsp.Content.ReadAsStreamAsync(ct);

            await request.OnFailure.Invoke(rsp.StatusCode, sr, currentSerializer, ct);
        }
        else if (request.ProblemDetailsType != null)
        {
            await using var sr = await rsp.Content.ReadAsStreamAsync(ct);

            problemDetails = serializer.Deserialize(sr, request.ProblemDetailsType, ct);
        }

        return new HttpMsgResponse
        {
            Headers = ReadResponseHeaders(rsp).ToArray(),
            IsSuccessStatusCode = isSuccessStatusCode,
            StatusCode = rsp.StatusCode,
            ProblemDetails = problemDetails
        };
    }
}