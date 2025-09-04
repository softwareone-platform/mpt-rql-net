using SoftwareOne.Rql.Abstractions.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Abstractions.Operators;

public interface IComparisonOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, string? value);
}