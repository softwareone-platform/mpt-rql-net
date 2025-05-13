using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlMapperContext<TStorage, TView>
{
    IRqlMapperContext<TStorage, TView> Map<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from) where TTo : TFrom;

    IRqlMapperContext<TStorage, TView> MapDynamic<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from);

    public IRqlMapperContext<TStorage, TView> MapDynamic<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from, Action<IRqlMapperContext<TFrom, TTo>> configureInline);

    public IRqlMapperContext<TStorage, TView> MapDynamic<TFrom, TTo>(Expression<Func<TView, IEnumerable<TTo>?>> to, Expression<Func<TStorage, IEnumerable<TFrom>?>> from, Action<IRqlMapperContext<TFrom, TTo>> configureInline);

    IRqlMapperContext<TStorage, TView> Ignore<TTo>(Expression<Func<TView, TTo?>> toIgnore);
}