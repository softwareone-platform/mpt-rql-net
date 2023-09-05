using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Dsl;
using SoftwareOne.Rql.Linq.Client.Filter;

namespace SoftwareOne.Rql.Linq.Client;

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

    public string? Generate(IFilterDefinitionProvider? provider)
    {
        var op = provider?.GetDefinition();
        if (op == null)
            return default;

        return GenerateQuery(op);
    }

    private string? GenerateQuery(IOperator? op)
    {
        return op switch
        {
            AndOperator { Left: var left, Right: var right } => $"{GetOperator(op)}(" + GenerateQuery(left) + ", " + GenerateQuery(right) + ")",
            OrOperator { Left: var left, Right: var right } => $"{GetOperator(op)}(" + GenerateQuery(left) + ", " + GenerateQuery(right) + ")",
            NotOperator { Left: var left } => $"{GetOperator(op)}(" + GenerateQuery(left) + ")",
            IComparableOperator co => $"{GetOperator(op)}(" + GenerateComparisionQuery(co) + ")",
            null => default,
            _ => throw new InvalidDefinitionException($"Unsupported {op.GetType()} operation")
        };
    }

    private string GenerateComparisionQuery(IComparableOperator co)
    {
       var (key, value) = co.ToQueryOperator(_propertyVisitor);
       return key + ", " + value;
    }

    private static string GetOperator(IOperator op)
    {
        var type = op.GetType().IsGenericType ? op.GetType().GetGenericTypeDefinition() : op.GetType();
        return TypesTranslator[type].SingleOrDefault() ?? throw new InvalidDefinitionException($"Unsupported {type} operation");
    }
    
}