using Mpt.Rql.Abstractions;
using System.Reflection;

namespace Mpt.Rql.Linq.Core;

internal class RqlPropertyInfo : IRqlPropertyInfo
{
    public string Name { get; internal set; } = null!;
    public bool IsIgnored { get; internal set; }
    public PropertyInfo Property { get; init; } = null!;
    public RqlPropertyType Type { get; internal set; }
    public RqlPropertyType? TypeOverride { get; internal set; }
    public bool IsCore { get; internal set; }
    public RqlSelectModes? SelectModeOverride { get; internal set; }
    public RqlActions Actions { get; internal set; }
    public RqlOperators Operators { get; internal set; }
    public Type? ActionStrategy { get; internal set; }
    public Type? ElementType { get; internal set; }
    public bool IsNullable { get; internal set; }

    public static RqlPropertyInfo Root { get; } = new RqlPropertyInfo { Type = RqlPropertyType.Root, Actions = RqlActions.All, Operators = RqlOperators.AllOperators };
}
