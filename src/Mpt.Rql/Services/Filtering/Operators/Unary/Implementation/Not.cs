using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.Unary.Implementation;

internal class Not : INot
{
    public Result<Expression> MakeExpression(Expression expression)
    {
        return Expression.Not(expression);
    }
}