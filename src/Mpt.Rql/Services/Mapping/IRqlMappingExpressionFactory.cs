using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace Mpt.Rql;

public interface IRqlMappingExpressionFactory<TType>
{
    /// <summary>
    /// Gets the mapping expression used to project <typeparamref name="TType"/> properties in RQL queries.
    /// </summary>
    /// <returns>
    /// An expression representing the mapping configuration for <typeparamref name="TType"/>.
    /// </returns>
    Expression<Func<TType, object?>> GetMappingExpression();
}
