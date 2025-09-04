using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Operators;
using SoftwareOne.Rql.Abstractions.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection;

public interface ICollectionOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, LambdaExpression? inner);
}