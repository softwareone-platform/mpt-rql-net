using SoftwareOne.Rql.Abstractions.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Abstractions.Operators;

public interface IListOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, IEnumerable<string> list);
}