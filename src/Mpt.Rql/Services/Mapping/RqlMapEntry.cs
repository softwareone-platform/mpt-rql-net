using Mpt.Rql.Abstractions;
using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace Mpt.Rql;

public class RqlMapEntry
{
    public IRqlPropertyInfo TargetProperty { get; internal set; } = null!;

    public LambdaExpression SourceExpression { get; internal set; } = null!;

    public IReadOnlyDictionary<string, RqlMapEntry>? InlineMap { get; internal set; }

    public List<RqlMapEntryCondition>? Conditions { get; internal set; }

    public bool IsDynamic { get; internal set; }

    /// <summary>
    /// The type of the expression factory used to create the mapping, if applicable.
    /// Set when MapWithFactory is used.
    /// </summary>
    public Type? FactoryType { get; internal set; }

    public Type TargetType => TargetProperty.ElementType ?? TargetProperty.Property.PropertyType;
}

public class RqlMapEntryCondition
{
    public LambdaExpression If { get; init; } = null!;

    public RqlMapEntry Entry { get; init; } = null!;
}