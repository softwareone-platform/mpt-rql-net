using System.Linq.Expressions;

namespace Mpt.Rql.Services.Mapping;

internal static class ExpressionHelper
{
    public static Expression UnwrapCastExpression(Expression input)
    {
        if (input is UnaryExpression { NodeType: ExpressionType.Convert } unaryExpr)
        {
            return unaryExpr.Operand;
        }

        return input;
    }

    /// <summary>
    /// Returns true if the expression is expensive to duplicate in a projection — i.e. it
    /// contains anything beyond a pure chain of member accesses on a single parameter.
    /// Examples: .Where().FirstOrDefault() is expensive; param.User.Name is not.
    /// </summary>
    public static bool IsExpensiveExpression(Expression expr)
    {
        var current = expr;
        while (true)
        {
            switch (current)
            {
                case ParameterExpression:
                    return false;
                case MemberExpression member:
                    current = member.Expression!;
                    break;
                default:
                    return true;
            }
        }
    }
}
