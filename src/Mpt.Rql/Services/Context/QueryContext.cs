using Mpt.Rql.Abstractions.Result;

namespace Mpt.Rql.Services.Context;

internal class QueryContext<TView>(IServiceProvider serviceProvider) : IQueryContext<TView>
{
    private List<Error>? _errors;
    private List<Func<IQueryable<TView>, IQueryable<TView>>>? _transformations;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

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

    public RqlNode Graph { get; } = RqlNode.MakeRoot();

    public bool HasErrors => _errors != null;

    private void EnsureErrors() => _errors ??= [];
}