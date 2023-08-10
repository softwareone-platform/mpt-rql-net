using ErrorOr;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Unary;

public interface IUnaryOperator : IOperator
{
    ErrorOr<Expression> MakeExpression(Expression expression);
}