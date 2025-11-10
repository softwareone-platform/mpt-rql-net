using Mpt.Rql.Linq.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Unary.Implementation;

internal class Not : INot
{
    public Result<Expression> MakeExpression(Expression expression)
    {
        return Expression.Not(expression);
    }
}