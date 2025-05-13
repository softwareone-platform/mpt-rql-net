using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlMapperContext<TStorage, TView>
{
    IRqlMapperContext<TStorage, TView> Map<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from) where TTo : TFrom;

    IRqlMapperContext<TStorage, TView> MapDynamic<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from, Action<IRqlMapperContext<TFrom, TTo>>? configureInline = null);

    IRqlMapperContext<TStorage, TView> MapDynamic<TFrom, TTo>(Expression<Func<TView, IEnumerable<TTo>?>> to, Expression<Func<TStorage, IEnumerable<TFrom>?>> from, Action<IRqlMapperContext<TFrom, TTo>>? configureInline = null);

    IRqlMapperContext<TStorage, TView> MapConditional<TTo>(Expression<Func<TView, TTo?>> to, Action<IRqlConditionMapperContext<TStorage>> configure);

    IRqlMapperContext<TStorage, TView> Ignore<TTo>(Expression<Func<TView, TTo?>> toIgnore);
}

public interface IRqlConditionMapperContext<TFromOwner>
{
    void If<TFrom>(Expression<Func<TFromOwner, bool>> condition, Expression<Func<TFromOwner, TFrom?>> from);

    void Else<TFrom>(Expression<Func<TFromOwner, TFrom?>> from);
}