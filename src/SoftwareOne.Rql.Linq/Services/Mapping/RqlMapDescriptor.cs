#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public class RqlMapDescriptor
{
    private readonly Dictionary<string, RqlMapProperty> _properties;

    internal RqlMapDescriptor(Dictionary<string, RqlMapProperty> properties)
    {
        _properties = properties;
    }

    public IEnumerable<RqlMapProperty> GetProperties() => _properties.Values;

    public bool TryGetMapByTargetPath(string targetPath, out RqlMapProperty? property) => _properties.TryGetValue(targetPath, out property);
}
