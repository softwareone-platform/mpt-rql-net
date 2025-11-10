using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core.Result;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Comparison;

public interface IComparisonOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, string? value);
}