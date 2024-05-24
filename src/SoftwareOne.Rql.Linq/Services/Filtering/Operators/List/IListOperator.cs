using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;

public interface IListOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, IEnumerable<string> list);
}