#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public class RqlMapDescriptor
{
    private readonly Dictionary<string, RqlMapEntry> _properties;

    internal RqlMapDescriptor(Dictionary<string, RqlMapEntry> properties)
    {
        _properties = properties;
    }

    public IEnumerable<RqlMapEntry> GetEntries() => _properties.Values;
}
