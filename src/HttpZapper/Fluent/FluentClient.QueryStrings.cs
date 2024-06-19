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

    public IHaveQueryStrings QueryStrings<T>(T? value) where T : class
    {
        if (value == null) return this;
        
        var props = value.GetType().GetProperties().Where(x => x.CanRead).ToArray();
        
        var data = new List<(string, string?)>();
        
        foreach (var prop in props)
        {
            var propValue = prop.GetValue(value);
            data.Add((prop.Name, propValue?.ToString()));
        }

        return QueryStrings(data as IEnumerable<(string,string?)>);
    }
}