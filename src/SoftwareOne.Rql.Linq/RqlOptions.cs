using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;
using System.Reflection;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;
public class RqlOptions
{
    public RqlOptions()
    {
        OperatorOverrides = new Dictionary<Type, Type>();
        Settings = new RqlSettings
        {
            DefaultActions = RqlActions.All,
            Select = new RqlSelectSettings
            {
                Mode = SelectMode.All
            }
        };
    }

    internal Assembly? ViewMappersAssembly { get; private set; }
    internal Type? PropertyMapperType { get; private set; }

    internal Dictionary<Type, Type> OperatorOverrides { get; init; }
    internal IRqlSettings Settings { get; init; }

    public RqlOptions OverrideComparisonOperator<TOperator, TImplementation>() where TOperator : IComparisonOperator where TImplementation : TOperator
        => OverrideOperatorInternal<TOperator, TImplementation>();

    public RqlOptions OverrideSearchOperator<TOperator, TImplementation>() where TOperator : ISearchOperator where TImplementation : TOperator
        => OverrideOperatorInternal<TOperator, TImplementation>();

    public RqlOptions OverrideListOperator<TOperator, TImplementation>() where TOperator : IListOperator where TImplementation : TOperator
        => OverrideOperatorInternal<TOperator, TImplementation>();

    private RqlOptions OverrideOperatorInternal<TExpression, TImplementation>()
    {
        OperatorOverrides[typeof(TExpression)] = typeof(TImplementation);
        return this;
    }

    public RqlOptions OverridePropertyMapper<TStrategy>() where TStrategy : IPropertyNameProvider
    {
        PropertyMapperType = typeof(TStrategy);
        return this;
    }

    public RqlOptions ScanForMappings(Assembly assembly)
    {
        ViewMappersAssembly = assembly;
        return this;
    }

    public RqlOptions Configure(Action<IRqlSettings> configure)
    {
        configure(Settings);
        return this;
    }
}
