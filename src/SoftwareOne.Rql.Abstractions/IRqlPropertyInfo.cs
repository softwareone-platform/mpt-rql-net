using System.Reflection;

namespace SoftwareOne.Rql.Abstractions;

public interface IRqlPropertyInfo
{
    string Name { get; }
    PropertyInfo Property { get; }
    bool IsDefault { get; }
    RqlAction Actions { get; }
    RqlPropertyType Type { get; }
}