using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.List;

public interface IListOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, IEnumerable<string> list);
}