using SoftwareOne.Rql.Abstractions.Configuration;
using SoftwareOne.Rql.Abstractions.Operators;
using System.Reflection;

namespace SoftwareOne.Rql.Linq;

internal class RqlConfiguration : IRqlConfiguration
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

    public IRqlConfiguration SetComparisonHandler<TOperator, THandler>() where TOperator : IComparisonOperator, IActualOperator where THandler : TOperator
        => SetOperatorInternal<TOperator, THandler>();

    public IRqlConfiguration SetSearchHandler<TOperator, THandler>() where TOperator : ISearchOperator, IActualOperator where THandler : TOperator
        => SetOperatorInternal<TOperator, THandler>();

    public IRqlConfiguration SetListHandler<TOperator, THandler>() where TOperator : IListOperator, IActualOperator where THandler : TOperator
        => SetOperatorInternal<TOperator, THandler>();

    public IRqlConfiguration SetPropertyNameProvider<T>() where T : IPropertyNameProvider
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
    public IRqlConfiguration ScanForMappers(Assembly assembly)
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
