using ErrorOr;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;

public interface IComparisonOperator : IOperator
{
    ErrorOr<Expression> MakeExpression(MemberExpression member, string? value);
}