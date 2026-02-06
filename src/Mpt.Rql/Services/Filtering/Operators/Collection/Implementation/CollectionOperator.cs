using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Filtering.Operators.Collection.Implementation;

internal abstract class CollectionOperator(IRqlSettings settings) : ICollectionOperator
{
    public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, LambdaExpression? inner)
    {
        var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, Operator);
        if (validationResult.IsError)
            return validationResult.Errors;

        var functions = (ICollectionFunctions)Activator.CreateInstance(typeof(CollectionFunctions<>).MakeGenericType(((RqlPropertyInfo)propertyInfo).ElementType!))!;

        var function = GetFunction(functions, inner == null);
        if (function.IsError) return function.Errors;

        if (inner != null)
        {
            var callExpression = Expression.Call(null, function.Value!, accessor, inner);
            return callExpression.WithNullSafetyIfEnabled(accessor, settings);
        }

        var callExpressionNoPredicate = Expression.Call(null, function.Value!, accessor);
        return callExpressionNoPredicate.WithNullSafetyIfEnabled(accessor, settings);
    }

    protected abstract RqlOperators Operator { get; }

    protected abstract Result<MethodInfo> GetFunction(ICollectionFunctions factory, bool noPredicate);
}
