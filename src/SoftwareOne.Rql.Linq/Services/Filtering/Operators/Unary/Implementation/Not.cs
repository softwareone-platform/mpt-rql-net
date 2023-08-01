using ErrorOr;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Unary;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Unary.Implementation
{
    internal class Not : INot
    {
        public ErrorOr<Expression> MakeExpression(Expression expression)
        {
            return Expression.Not(expression);
        }
    }
}
