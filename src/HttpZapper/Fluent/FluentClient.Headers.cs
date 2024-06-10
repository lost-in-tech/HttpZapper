namespace HttpZapper.Fluent;

internal sealed partial class FluentClient : IHaveHeaders
{
    private List<(string Name, string? Value)>? _headers = null;

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
}