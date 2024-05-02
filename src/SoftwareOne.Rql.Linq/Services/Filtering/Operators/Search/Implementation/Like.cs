using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core.Expressions;
using System.Linq.Expressions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation;

internal class Like : ILike
{
    private static readonly MethodInfo _methodStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
    private static readonly MethodInfo _methodEndsWith = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!;
    private static readonly MethodInfo _methodContains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
    private static readonly MethodInfo _methodEquals = typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) })!;
    private static readonly char _wildcard = '*';

    public ErrorOr<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, string pattern)
    {
        var (methodInfo, rqlOperator) = pattern switch
        {
            var s when s.StartsWith(_wildcard) && s.EndsWith(_wildcard) => (_methodContains, RqlOperators.Contains),
            var s when s.StartsWith(_wildcard) => (_methodEndsWith, RqlOperators.EndsWith),
            var s when s.EndsWith(_wildcard) => (_methodStartsWith, RqlOperators.StartsWith),
            _ => (_methodEquals, RqlOperators.Eq)
        };

        var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, rqlOperator);
        if (validationResult.IsError)
            return validationResult.Errors;

        return Expression.Call(member, methodInfo, ConstantBuilder.Build(pattern.Trim(_wildcard), typeof(string)));
    }

    protected virtual bool IsInsensitive => false;
}