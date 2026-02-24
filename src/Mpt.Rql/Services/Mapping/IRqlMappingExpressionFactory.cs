using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace Mpt.Rql;

public interface IRqlMappingExpressionFactory
{
    LambdaExpression GetStorageExpressionLambda();

    /// <summary>
    /// Gets a hint indicating how the mapping expression should be processed during query generation. 
    /// </summary>
    ExpressionFactoryHint Hint => ExpressionFactoryHint.None;
}

public interface IRqlMappingExpressionFactory<TType> : IRqlMappingExpressionFactory
{
    /// <summary>
    /// Gets the mapping expression used to project <typeparamref name="TType"/> properties in RQL queries.
    /// </summary>
    /// <returns>
    /// An expression representing the mapping configuration for <typeparamref name="TType"/>.
    /// </returns>
    Expression<Func<TType, object?>> GetStorageExpression();

    LambdaExpression IRqlMappingExpressionFactory.GetStorageExpressionLambda()
        => GetStorageExpression();
}

public enum ExpressionFactoryHint
{
    /// <summary>
    /// Default mapping behavior. 
    /// </summary>
    None = 0,
    /// <summary>
    /// Take first element from the collection when mapping.
    /// </summary>
    TakeFirst = 1
}

