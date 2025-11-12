using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.Collection;

public interface ICollectionOperator : IOperator
{
    Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, LambdaExpression? inner);
}