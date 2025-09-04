using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Mapping;
using System.Linq.Expressions;


namespace SoftwareOne.Rql.Linq.Services.Mapping;

internal class RqlMapEntry : IRqlMapEntry
{
    public IRqlPropertyInfo TargetProperty { get; internal set; } = null!;

    public LambdaExpression SourceExpression { get; internal set; } = null!;

    public IReadOnlyDictionary<string, IRqlMapEntry>? InlineMap { get; internal set; }

    public IReadOnlyCollection<IRqlMapEntryCondition>? Conditions { get; internal set; }

    public void AddCondition(IRqlMapEntryCondition condition)
    {
        if (Conditions is null)
            Conditions = [condition];
        else if (Conditions is List<IRqlMapEntryCondition> list)
            list.Add(condition);
        else
            Conditions = [.. Conditions, condition];
    }

    public bool IsDynamic { get; internal set; }
}

internal class RqlMapEntryCondition : IRqlMapEntryCondition
{
    public LambdaExpression If { get; init; } = null!;

    public IRqlMapEntry Entry { get; init; } = null!;
}