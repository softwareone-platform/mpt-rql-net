using ErrorOr;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;

public interface ISearchOperator : IOperator
{
    ErrorOr<Expression> MakeExpression(MemberExpression member, string pattern);
}