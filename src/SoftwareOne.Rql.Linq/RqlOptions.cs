using SoftwareOne.Rql.Linq.Core.Configuration;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;
using System.Reflection;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    public class RqlOptions
    {
        public RqlOptions()
        {
            OperatorOverrides = new Dictionary<Type, Type>();
            Settings = new RqlSettings
            {
                DefaultMemberFlags = MemberFlag.All
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

        public RqlOptions SetDefaultMemberFlags(MemberFlag flags)
        {
            Settings.DefaultMemberFlags = flags;
            return this;
        }

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
    }
}
