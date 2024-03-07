using System.Reflection;

namespace SoftwareOne.Rql.Abstractions;

public interface IRqlPropertyInfo
{
    string Name { get; }
    PropertyInfo? Property { get; }
    bool IsCore { get; }
    RqlSelectModes SelectMode { get; }
    RqlActions Actions { get; }
    RqlOperators Operators { get; }
    RqlPropertyType Type { get; }
}