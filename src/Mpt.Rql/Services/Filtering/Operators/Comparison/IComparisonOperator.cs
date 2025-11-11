using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.Comparison;

public interface IComparisonOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, string? value);
}