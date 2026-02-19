using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace Mpt.Rql;

public interface IRqlMappingExpressionFactory<TType>
{
    LambdaExpression GetMappingExpression();
}
