using ErrorOr;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation;

internal class ListIn : IListIn
{
    private static readonly MethodInfo _containsMethod = typeof(Enumerable).GetMethods().Single(
        methodInfo => methodInfo.Name == nameof(Enumerable.Contains) && methodInfo.GetParameters().Length == 2);

    public virtual ErrorOr<Expression> MakeExpression(MemberExpression member, IEnumerable<string> list)
    {
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
        return Expression.Call(contains, Expression.Constant(values), member);
    }
}