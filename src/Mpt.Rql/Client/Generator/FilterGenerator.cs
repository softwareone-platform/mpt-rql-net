using Mpt.Rql.Client;
using Mpt.Rql.Linq.Client.Builder.Operators;

namespace Mpt.Rql.Linq.Client.Generator;

internal class FilterGenerator : IFilterGenerator
{
    private readonly IPropertyVisitor _propertyVisitor;

    public FilterGenerator(IPropertyVisitor propertyVisitor)
    {
        _propertyVisitor = propertyVisitor;
    }

    private static ILookup<Type, string> TypesTranslator => new Dictionary<Type, string>
    {
        { typeof(Equal<,>), "eq" },
        { typeof(NotEqual<,>), "ne" },
        { typeof(Le<,>), "le" },
        { typeof(Lt<,>), "lt" },
        { typeof(Ge<,>), "ge" },
        { typeof(Gt<,>), "gt" },
        { typeof(In<,>), "in" },
        { typeof(Out<,>), "out" },
        { typeof(Like<,>), "like" },
        { typeof(AndOperator), "and" },
        { typeof(OrOperator), "or" },
        { typeof(NotOperator), "not" },
    }.ToLookup(x => x.Key, x => x.Value);

    public string? Generate(IOperator? filterOperator)
    {
        if (filterOperator == null)
            return default;

        return GenerateQuery(filterOperator);
    }

    private string? GenerateQuery(IOperator? filterOperator)
    {
        return filterOperator switch
        {
            AndOperator { Operators: var list } => $"{GetOperator(filterOperator)}(" + string.Join(",", list.Select(GenerateQuery)) + ")",
            OrOperator { Operators: var list } => $"{GetOperator(filterOperator)}(" + string.Join(",", list.Select(GenerateQuery)) + ")",
            NotOperator { Inner: var inner } => $"{GetOperator(filterOperator)}(" + GenerateQuery(inner) + ")",
            IComparableOperator co => $"{GetOperator(filterOperator)}(" + GenerateComparisionQuery(co) + ")",
            null => default,
            _ => throw new InvalidDefinitionException($"Unsupported {filterOperator.GetType()} operation")
        };
    }

    private string GenerateComparisionQuery(IComparableOperator co)
    {
        var (key, value) = co.ToQueryOperator(_propertyVisitor);
        return $"{key},{value}";
    }

    private static string GetOperator(IOperator op)
    {
        var type = op.GetType().IsGenericType ? op.GetType().GetGenericTypeDefinition() : op.GetType();
        return TypesTranslator[type].SingleOrDefault() ?? throw new InvalidDefinitionException($"Unsupported {type} operation");
    }

}