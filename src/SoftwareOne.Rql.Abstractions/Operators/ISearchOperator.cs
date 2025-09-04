using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Abstractions.Operators;

public interface ISearchOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, string pattern);
}