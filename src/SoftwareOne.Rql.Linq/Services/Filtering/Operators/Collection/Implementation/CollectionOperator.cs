using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

internal abstract class CollectionOperator : ICollectionOperator
{
    public ErrorOr<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, LambdaExpression inner)
    {
        var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, Operator);
        if (validationResult.IsError)
            return validationResult.Errors;

        var functions = (ICollectionFunctions)Activator.CreateInstance(typeof(CollectionFunctions<>).MakeGenericType(inner.Parameters[0].Type))!;
        return Expression.Call(null, GetFunction(functions), member, inner);
    }

    protected abstract RqlOperators Operator { get; }

    protected abstract MethodInfo GetFunction(ICollectionFunctions factory);
}
