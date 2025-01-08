using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlMapperContext<TStorage, TView>
{
    IRqlMapperContext<TStorage, TView> Map<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from) where TTo : TFrom;

    IRqlMapperContext<TStorage, TView> MapDynamic<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from);
    
    IRqlMapperContext<TStorage, TView> Ignore<TTo>(Expression<Func<TView, TTo?>> toIgnore);
}