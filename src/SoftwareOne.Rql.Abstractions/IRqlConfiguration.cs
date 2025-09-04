#pragma warning disable IDE0130
using SoftwareOne.Rql.Abstractions.Configuration;
using SoftwareOne.Rql.Abstractions.Operators;
using System.Reflection;

namespace SoftwareOne.Rql;

public interface IRqlConfiguration
{
    GlobalRqlSettings Settings { get; init; }

    IRqlConfiguration ScanForMappers(Assembly assembly);
    IRqlConfiguration SetComparisonHandler<TOperator, THandler>()
        where TOperator : IComparisonOperator, IActualOperator
        where THandler : TOperator;
    IRqlConfiguration SetListHandler<TOperator, THandler>()
        where TOperator : IListOperator, IActualOperator
        where THandler : TOperator;
    IRqlConfiguration SetPropertyNameProvider<T>() where T : IPropertyNameProvider;
    IRqlConfiguration SetSearchHandler<TOperator, THandler>()
        where TOperator : ISearchOperator, IActualOperator
        where THandler : TOperator;
}