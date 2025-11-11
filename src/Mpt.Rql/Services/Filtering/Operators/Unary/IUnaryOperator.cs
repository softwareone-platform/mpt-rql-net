using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.Unary;

public interface IUnaryOperator : IOperator
{
    Result<Expression> MakeExpression(Expression expression);
}