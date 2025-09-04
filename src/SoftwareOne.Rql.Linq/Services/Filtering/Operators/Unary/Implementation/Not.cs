using SoftwareOne.Rql.Abstractions.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Unary.Implementation;

internal class Not : INot
{
    public Result<Expression> MakeExpression(Expression expression)
    {
        return Expression.Not(expression);
    }
}