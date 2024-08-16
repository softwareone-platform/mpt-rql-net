using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;
using System.Reflection;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;
public class RqlConfiguration
{
    public RqlConfiguration()
    {
        OperatorOverrides = [];

        Settings = new GlobalRqlSettings
        {
            General = new RqlGeneralSettings
            {
                DefaultActions = RqlActions.All,

            },
            Select = new RqlSelectSettings
            {
                Implicit = RqlSelectModes.Core,
                Explicit = RqlSelectModes.Core
            }
        };
    }

    internal Assembly? ViewMappersAssembly { get; private set; }

    internal Type? PropertyMapperType { get; private set; }

    internal Dictionary<Type, Type> OperatorOverrides { get; init; }

    public GlobalRqlSettings Settings { get; init; }

    public RqlConfiguration SetComparisonHandler<TOperator, THandler>() where TOperator : IComparisonOperator, IActualOperator where THandler : TOperator
        => SetOperatorInternal<TOperator, THandler>();

    public RqlConfiguration SetSearchHandler<TOperator, THandler>() where TOperator : ISearchOperator, IActualOperator where THandler : TOperator
        => SetOperatorInternal<TOperator, THandler>();

    public RqlConfiguration SetListHandler<TOperator, THandler>() where TOperator : IListOperator, IActualOperator where THandler : TOperator
        => SetOperatorInternal<TOperator, THandler>();

    public RqlConfiguration SetPropertyNameProvider<T>() where T : IPropertyNameProvider
    {
        PropertyMapperType = typeof(T);
        return this;
    }

    /// <summary>
    /// Scan provided assembly for IRqlMapper implementations.
    /// Only one assembly may be registered for scan.
    /// </summary>
    /// <param name="assembly">Assembly to be scanned</param>
    /// <returns></returns>
    public RqlConfiguration ScanForMappers(Assembly assembly)
    {
        ViewMappersAssembly = assembly;
        return this;
    }

    private RqlConfiguration SetOperatorInternal<TExpression, TImplementation>()
    {
        OperatorOverrides[typeof(TExpression)] = typeof(TImplementation);
        return this;
    }
}
