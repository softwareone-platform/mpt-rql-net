using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Mapping;

internal class RqlMapperSwitchContext<TFromOwner>(RqlMapEntry parentEntry) : IRqlMapperSwitchContext<TFromOwner>, IRqlMapperSwitchContextFinalizer<TFromOwner>
{
    public IRqlMapperSwitchContextFinalizer<TFromOwner> DynamicCase<TFrom>(Expression<Func<TFromOwner, bool>> condition, Expression<Func<TFromOwner, TFrom?>> from) 
        => Case(condition, from, true);

    public IRqlMapperSwitchContextFinalizer<TFromOwner> StaticCase<TFrom>(Expression<Func<TFromOwner, bool>> condition, Expression<Func<TFromOwner, TFrom?>> from)
        => Case(condition, from, false);

    public void DynamicDefault<TFrom>(Expression<Func<TFromOwner, TFrom?>> from)
        => Default(from, true);

    public void StaticDefault<TFrom>(Expression<Func<TFromOwner, TFrom?>> from)
        => Default(from, false);

    private RqlMapperSwitchContext<TFromOwner> Case<TFrom>(Expression<Func<TFromOwner, bool>> condition, Expression<Func<TFromOwner, TFrom?>> from, bool isDynamic)
    {
        parentEntry.Conditions ??= [];
        parentEntry.Conditions.Add(new RqlMapEntryCondition
        {
            If = condition,
            Entry = new RqlMapEntry
            {
                TargetProperty = parentEntry.TargetProperty,
                SourceExpression = from,
                IsDynamic = isDynamic,
                InlineMap = null,
                Conditions = null
            }
        });

        return this;
    }

    private void Default<TFrom>(Expression<Func<TFromOwner, TFrom?>> from, bool isDynamic)
    {
        parentEntry.SourceExpression = from;
        parentEntry.IsDynamic = isDynamic;
    }
}
