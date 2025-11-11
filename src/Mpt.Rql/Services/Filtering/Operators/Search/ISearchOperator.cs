using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Search;

public interface ISearchOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, string pattern);
}