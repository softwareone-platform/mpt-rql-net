using SoftwareOne.Rql.Abstractions.Operators;
using SoftwareOne.Rql.Abstractions.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Unary;

public interface IUnaryOperator : IOperator
{
    Result<Expression> MakeExpression(Expression expression);
}