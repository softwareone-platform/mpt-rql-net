using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;

public interface IComparisonOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, string? value);
}