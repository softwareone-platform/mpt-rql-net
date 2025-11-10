using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Mapping;

internal class RqlMapperSwitchContext<TFromOwner>(RqlMapEntry parentEntry) : IRqlMapperSwitchContextFinalizer<TFromOwner>
{
    public IRqlMapperSwitchContextFinalizer<TFromOwner> Case<TFrom>(Expression<Func<TFromOwner, bool>> condition, Expression<Func<TFromOwner, TFrom?>> from, bool mapStatic = false)
    {
        parentEntry.Conditions ??= [];
        parentEntry.Conditions.Add(new RqlMapEntryCondition
        {
            If = condition,
            Entry = new RqlMapEntry
            {
                TargetProperty = parentEntry.TargetProperty,
                SourceExpression = from,
                IsDynamic = !mapStatic,
                InlineMap = null,
                Conditions = null
            }
        });

        return this;
    }

    public void Default<TFrom>(Expression<Func<TFromOwner, TFrom?>> from, bool mapStatic = false)
    {
        parentEntry.SourceExpression = from;
        parentEntry.IsDynamic = !mapStatic;
    }
}
