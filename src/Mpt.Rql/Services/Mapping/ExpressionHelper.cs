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
}
