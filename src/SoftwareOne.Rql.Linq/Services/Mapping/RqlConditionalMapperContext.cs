using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Mapping;

internal class RqlConditionalMapperContext<TFromOwner>(RqlMapEntry parentEntry) : IRqlConditionMapperContext<TFromOwner>
{
    public void Else<TFrom>(Expression<Func<TFromOwner, TFrom?>> from)
    {
        parentEntry.SourceExpression = from;
    }

    public void If<TFrom>(Expression<Func<TFromOwner, bool>> condition, Expression<Func<TFromOwner, TFrom?>> from)
    {
        parentEntry.Conditions ??= [];
        parentEntry.Conditions.Add(new RqlMapEntryCondition
        {
            If = condition,
            Entry = new RqlMapEntry
            {
                TargetProperty = parentEntry.TargetProperty,
                SourceExpression = from,
                IsDynamic = true,
                InlineMap = null,
                Conditions = null
            }
        });
    }
}
