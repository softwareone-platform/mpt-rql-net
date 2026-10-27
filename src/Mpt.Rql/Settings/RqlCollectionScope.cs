using System.Linq.Expressions;

namespace Mpt.Rql.Settings;

/// <summary>
/// Stores a collection selector and its element restriction.
/// </summary>
internal sealed record RqlCollectionScope(
    LambdaExpression CollectionSelector,
    LambdaExpression Predicate);