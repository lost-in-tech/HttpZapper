using System.Text;
using System.Text.Unicode;

namespace HttpZapper.Fluent;

internal sealed partial class FluentClient : IHaveHeaders
{
    private List<(string Name, string? Value)>? _headers = null;
    private List<(string Name, string? Value)>? _cookies = null;

    public IHaveHeaders Header(string name, string? value)
    {
        _headers ??= new();
        _headers.Add((name, value));
        return this;
    }

    public IHaveHeaders Headers(IEnumerable<(string name, string? value)> values)
    {
        _headers ??= new();
        _headers.AddRange(values);
        return this;
    }

    public IHaveHeaders Cookie(string name, string? value)
    {
        _cookies ??= new();
        
        _cookies.Add((name, value));

        return this;
    }

    public IHaveHeaders Cookies(IEnumerable<(string name, string? value)> values)
    {
        _cookies ??= new();

        _cookies.AddRange(values);

        return this;
    }

    public IHaveHeaders OAuthBearerToken(string token)
    {
        return Header("Authorization", $"Bearer {token}");
    }

    public IHaveHeaders BasicAuth(string username, string password)
    {
        return BasicAuth($"{username}:{password}");
    }

    public IHaveHeaders BasicAuth(string credentials)
    {
        var base64Cred = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        return Header("Authorization", $"Basic {base64Cred}");
    }
}