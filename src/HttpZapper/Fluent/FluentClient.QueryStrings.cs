namespace HttpZapper.Fluent;

internal sealed partial class FluentClient : IHaveQueryStrings
{
    private List<(string Name, string? Value)>? _queryStrings = null;
    
    public IHaveQueryStrings QueryString(string name, string? value)
    {
        _queryStrings ??= new();
        
        _queryStrings.Add((name, value));

        return this;
    }

    public IHaveQueryStrings QueryStrings(IEnumerable<(string Name, string? Value)> values)
    {
        _queryStrings ??= new();

        _queryStrings.AddRange(values);

        return this;
    }
}