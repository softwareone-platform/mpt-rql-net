using SoftwareOne.Rql.Client.Builder.Dsl;

namespace SoftwareOne.Rql.Client.RqlGenerator;

public class QueryParamsGenerator : IQueryParamsGenerator
{
    private static ILookup<Type, string> _typesTranslator => new Dictionary<Type, string>
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

    public string Generate(IOperator op)
    {
        return GenerateQuery(op);
    }

    private string GenerateQuery(IOperator op)
    {
        return op switch
        {
            AndOperator { Left: var left, Right: var right } => $"{GetOperator(op)}(" + GenerateQuery(left) + ", " + GenerateQuery(right) + ")",
            OrOperator { Left: var left, Right: var right } => $"{GetOperator(op)}(" + GenerateQuery(left) + ", " + GenerateQuery(right) + ")",
            NotOperator { Left: var left } => $"{GetOperator(op)}(" + GenerateQuery(left) + ")",
            IComparableOperator co => $"{GetOperator(op)}(" + GenerateComparisionQuery(co) + ")"
        };
    }

    private string GenerateComparisionQuery(IComparableOperator co)
    {
       var (key, value) = co.ToQueryOperator();
       return key + ", " + value;
    }

    private string GetOperator(IOperator op)
    {
        var type = op.GetType().IsGenericType ? op.GetType().GetGenericTypeDefinition() : op.GetType();
        return _typesTranslator[type].SingleOrDefault() ?? throw new ArgumentException("Unsupported operation");
    }
}