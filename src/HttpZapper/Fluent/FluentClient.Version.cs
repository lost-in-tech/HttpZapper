namespace HttpZapper.Fluent;

internal sealed partial class FluentClient : IHaveVersion
{
    private Version? _version = null;

    public IHaveVersion Version(Version version)
    {
        _version = version;

        return this;
    }
}