using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;

namespace Mpt.Rql.Services.Context;

internal class QueryContext<TView>(IExternalServiceAccessor serviceAccessor) : IQueryContext<TView>, IResettable
{
    private List<Error>? _errors;
    private List<Func<IQueryable<TView>, IQueryable<TView>>>? _transformations;

    public IServiceProvider ExternalServices { get; } = serviceAccessor.GetServiceProvider();

    public IEnumerable<Error> GetErrors() => _errors ?? Enumerable.Empty<Error>();

    public void AddTransformation(Func<IQueryable<TView>, IQueryable<TView>> func)
    {
        _transformations ??= [];
        _transformations.Add(func);
    }

    public IQueryable<TView> ApplyTransformations(IQueryable<TView> query)
    {
        if (_transformations == null)
            return query;

        foreach (var item in _transformations)
        {
            query = item.Invoke(query);
        }

        return query;
    }

    public void AddError(Error error)
    {
        EnsureErrors();
        _errors!.Add(error);
    }

    public void AddErrors(IEnumerable<Error> errors)
    {
        EnsureErrors();
        _errors!.AddRange(errors);
    }

    public RqlNode Graph { get; private set; } = RqlNode.MakeRoot();

    public bool HasErrors => _errors != null;

    /// <summary>
    /// Clears all per-request state so this instance can be reused within the same scope.
    /// </summary>
    public void Reset()
    {
        _errors = null;
        _transformations = null;
        Graph = RqlNode.MakeRoot();
    }

    private void EnsureErrors() => _errors ??= [];
}