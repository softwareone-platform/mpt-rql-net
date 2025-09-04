using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Result;
using SoftwareOne.Rql.Linq.Core.Expressions;
using SoftwareOne.Rql.Linq.Core.Result;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation;

internal class ListIn : IListIn
{
    private static readonly MethodInfo _containsMethod = typeof(Enumerable).GetMethods().Single(
        methodInfo => methodInfo.Name == nameof(Enumerable.Contains) && methodInfo.GetParameters().Length == 2);

    public virtual Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, IEnumerable<string> list)
    {
        var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, Operator);
        if (validationResult.IsError)
            return validationResult.Errors;

        var listType = typeof(List<>).MakeGenericType(member.Type);
        var values = (IList)Activator.CreateInstance(listType)!;

        var errors = new List<Error>();

        foreach (var constant in list)
        {
            var eoT = ConstantHelper.ChangeType(constant, member.Type);
            if (eoT.IsError)
                errors.AddRange(eoT.Errors);
            else
                values.Add(eoT.Value);
        }

        if (errors.Any())
            return errors;

        var contains = _containsMethod.MakeGenericMethod(member.Type);
        return Expression.Call(contains, ConstantBuilder.Build(values, values.GetType()), member);
    }

    protected virtual RqlOperators Operator => RqlOperators.ListIn;
}