using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation;

internal class Like : ILike
{
    private static readonly MethodInfo _methodStartsWith;
    private static readonly MethodInfo _methodEndsWith;
    private static readonly MethodInfo _methodContains;
    private static readonly MethodInfo _methodEquals;
    private static readonly char _wildcard;

    static Like()
    {
        var stringType = typeof(string);
        var args = new[] { stringType };

        _methodStartsWith = stringType.GetMethod(nameof(string.StartsWith), args)!;
        _methodEndsWith = stringType.GetMethod(nameof(string.EndsWith), args)!;
        _methodContains = stringType.GetMethod(nameof(string.Contains), args)!;
        _methodEquals = stringType.GetMethod(nameof(string.Equals), args)!;
        _wildcard = '*';
    }

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

        return Expression.Call(member, methodInfo, Expression.Constant(pattern.Trim(_wildcard)));
    }

    protected virtual bool IsInsensitive => false;
}