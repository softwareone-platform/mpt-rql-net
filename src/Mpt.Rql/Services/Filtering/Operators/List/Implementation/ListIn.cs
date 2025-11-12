using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Expressions;
using Mpt.Rql.Services.Filtering.Operators;
using Mpt.Rql.Services.Filtering.Operators.List;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Filtering.Operators.List.Implementation;

internal class ListIn : IListIn
{
    private static readonly MethodInfo _containsMethod = typeof(Enumerable).GetMethods().Single(
        methodInfo => methodInfo.Name == nameof(Enumerable.Contains) && methodInfo.GetParameters().Length == 2);

    public virtual Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, IEnumerable<string> list)
    {
        var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, Operator);
        if (validationResult.IsError)
            return validationResult.Errors;

        var listType = typeof(List<>).MakeGenericType(accessor.Type);
        var values = (IList)Activator.CreateInstance(listType)!;

        var errors = new List<Error>();

        foreach (var constant in list)
        {
            var eoT = ConstantHelper.ChangeType(constant, accessor.Type);
            if (eoT.IsError)
                errors.AddRange(eoT.Errors);
            else
                values.Add(eoT.Value);
        }

        if (errors.Any())
            return errors;

        var contains = _containsMethod.MakeGenericMethod(accessor.Type);
        return Expression.Call(contains, ConstantBuilder.Build(values, values.GetType()), accessor);
    }

    protected virtual RqlOperators Operator => RqlOperators.ListIn;
}