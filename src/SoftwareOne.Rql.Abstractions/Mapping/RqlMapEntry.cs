using System.Linq.Expressions;

namespace SoftwareOne.Rql.Abstractions.Mapping;

public interface IRqlMapEntry
{
    IRqlPropertyInfo TargetProperty { get; }

    LambdaExpression SourceExpression { get; }

    IReadOnlyDictionary<string, IRqlMapEntry>? InlineMap { get; }

    IReadOnlyCollection<IRqlMapEntryCondition>? Conditions { get; }

    bool IsDynamic { get; }

    Type TargetType => TargetProperty.ElementType ?? TargetProperty.Property.PropertyType;
}

public interface IRqlMapEntryCondition
{
    LambdaExpression If { get; }

    IRqlMapEntry Entry { get; }
}