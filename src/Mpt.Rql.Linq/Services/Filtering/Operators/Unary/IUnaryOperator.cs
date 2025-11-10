using Mpt.Rql.Linq.Core.Result;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Unary;

public interface IUnaryOperator : IOperator
{
    Result<Expression> MakeExpression(Expression expression);
}