namespace SoftwareOne.Rql.Linq.Configuration;

internal static class SettingsExtensions
{
    public static void Apply(this IRqlSelectSettings target, IRqlSelectSettings source)
    {
        target.MaxDepth = source.MaxDepth;
        target.Implicit = source.Implicit;
        target.Explicit = source.Explicit;
    }
}
