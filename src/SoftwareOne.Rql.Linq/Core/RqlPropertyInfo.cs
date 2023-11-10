using SoftwareOne.Rql.Abstractions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Core;

internal class RqlPropertyInfo : IRqlPropertyInfo
{
    public string Name { get; internal set; } = null!;
    public PropertyInfo? Property { get; internal set; }
    public RqlPropertyType Type { get; internal set; }
    public bool IsCore { get; internal set; }
    public RqlSelectMode SelectMode { get; internal set; }
    public RqlActions Actions { get; internal set; }
    public RqlOperators Operators { get; internal set; }
    public Type? ElementType { get; internal set; }

    public static RqlPropertyInfo Root { get; } = new RqlPropertyInfo { Type = RqlPropertyType.Root, Actions = RqlActions.All, Operators = RqlOperators.AllOperators };
}
