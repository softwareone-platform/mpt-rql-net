using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Filtering.Operators;
using Mpt.Rql.Services.Filtering.Operators.Collection;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Filtering.Operators.Collection.Implementation;

internal abstract class CollectionOperator : ICollectionOperator
{
    public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, LambdaExpression? inner)
    {
        var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, Operator);
        if (validationResult.IsError)
            return validationResult.Errors;

        var functions = (ICollectionFunctions)Activator.CreateInstance(typeof(CollectionFunctions<>).MakeGenericType(((RqlPropertyInfo)propertyInfo).ElementType!))!;

        var function = GetFunction(functions, inner == null);
        if (function.IsError) return function.Errors;

        if (inner != null)
            return Expression.Call(null, function.Value!, member, inner);

        return Expression.Call(null, function.Value!, member);
    }

    protected abstract RqlOperators Operator { get; }

    protected abstract Result<MethodInfo> GetFunction(ICollectionFunctions factory, bool noPredicate);
}
