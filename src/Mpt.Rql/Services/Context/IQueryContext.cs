using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Context;

internal interface IQueryContext<TView>
{
    IServiceProvider ExternalServices { get; }
    IEnumerable<Error> GetErrors();

    /// <summary>
    /// The filter predicate composed on the view type, or <c>null</c> when the request has no filter. It is kept
    /// apart from the other transformations because, depending on <c>Filter.Binding</c>, it is applied either
    /// to the projected view (by <see cref="ApplyTransformations(IQueryable{TView})"/>) or, rewritten, to the
    /// storage query before the projection.
    /// </summary>
    Expression<Func<TView, bool>>? FilterPredicate { get; set; }

    void AddTransformation(Func<IQueryable<TView>, IQueryable<TView>> func);

    /// <summary>Applies the filter predicate, if any, followed by the registered transformations.</summary>
    IQueryable<TView> ApplyTransformations(IQueryable<TView> query);

    /// <summary>Applies the registered transformations, preceded by the filter predicate only when <paramref name="applyFilter"/> is set.</summary>
    IQueryable<TView> ApplyTransformations(IQueryable<TView> query, bool applyFilter);
    void AddError(Error error);
    void AddErrors(IEnumerable<Error> errors);
    public bool HasErrors { get; }
    RqlNode Graph { get; }
}