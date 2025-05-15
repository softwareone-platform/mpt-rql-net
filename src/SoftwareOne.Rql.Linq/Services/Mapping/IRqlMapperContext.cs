using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlMapperContext<TStorage, TView>
{
    IRqlMapperContext<TStorage, TView> MapStatic<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from) where TTo : TFrom;

    IRqlMapperContext<TStorage, TView> MapDynamic<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from, Action<IRqlMapperContext<TFrom, TTo>>? configureInline = null);

    IRqlMapperContext<TStorage, TView> MapDynamic<TFrom, TTo>(Expression<Func<TView, IEnumerable<TTo>?>> to, Expression<Func<TStorage, IEnumerable<TFrom>?>> from, Action<IRqlMapperContext<TFrom, TTo>>? configureInline = null);

    IRqlMapperSwitchContext<TStorage> Switch<TTo>(Expression<Func<TView, TTo?>> to);

    IRqlMapperContext<TStorage, TView> Ignore<TTo>(Expression<Func<TView, TTo?>> toIgnore);
}

public interface IRqlMapperSwitchContext<TFromOwner>
{
    IRqlMapperSwitchContextFinalizer<TFromOwner> Case<TFrom>(Expression<Func<TFromOwner, bool>> condition, Expression<Func<TFromOwner, TFrom?>> from, bool mapStatic = false);
}

public interface IRqlMapperSwitchContextFinalizer<TFromOwner> : IRqlMapperSwitchContext<TFromOwner>
{
    void Default<TFrom>(Expression<Func<TFromOwner, TFrom?>> from, bool mapStatic = false);
}