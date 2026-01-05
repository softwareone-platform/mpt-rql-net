using System.Reflection;

namespace Mpt.Rql.Abstractions;

public interface IRqlPropertyInfo
{
    string Name { get; }
    PropertyInfo Property { get; }
    RqlPropertyMode Mode { get; }
    bool IsCore { get; }
    bool IsNullable { get; }
    RqlSelectModes? SelectModeOverride { get; }
    RqlActions Actions { get; }
    RqlOperators Operators { get; }
    RqlPropertyType Type { get; }
    Type? ElementType { get; }
}