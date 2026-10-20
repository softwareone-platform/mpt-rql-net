using Mpt.Rql.Services.Mapping;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Binding;

/// <summary>
/// Rewrites a predicate composed on the view type into one on the storage type, so it can be applied
/// before the mapping projection (<c>FilterBinding.Storage</c>).
/// </summary>
internal static class StoragePredicateBinder
{
    /// <summary>
    /// Substitutes the projection for the view parameter and then hoists the projection's null guards out
    /// of the compared members (see <see cref="NullGuardHoistingVisitor"/>). The substitution alone already
    /// yields a correct storage predicate; the hoisting gives it the shape relational providers can index.
    /// </summary>
    public static Expression<Func<TStorage, bool>> Bind<TStorage, TView>(
        Expression<Func<TView, bool>> predicate,
        Expression<Func<TStorage, TView>> selector)
    {
        var substituted = new ReplaceParameterVisitor(predicate.Parameters[0], selector.Body).Visit(predicate.Body);
        var hoisted = new NullGuardHoistingVisitor().Visit(substituted);

        return Expression.Lambda<Func<TStorage, bool>>(hoisted, selector.Parameters[0]);
    }
}
