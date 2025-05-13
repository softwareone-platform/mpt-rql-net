using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public class RqlMapEntry
{
    public IRqlPropertyInfo TargetProperty { get; init; } = null!;

    public LambdaExpression SourceExpression { get; init; } = null!;

    public IReadOnlyDictionary<string, RqlMapEntry>? InlineMap { get; init; }

    public Type TargetType => TargetProperty.ElementType ?? TargetProperty.Property.PropertyType;

    public bool IsDynamic { get; init; }
}