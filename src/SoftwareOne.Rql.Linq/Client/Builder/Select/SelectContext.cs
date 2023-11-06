using SoftwareOne.Rql.Client;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Builder.Select;

internal class SelectContext<T> : ISelectDefinitionProvider, ISelectContext<T> where T : class
{
    private IList<ISelectDefinition>? _included;
    private IList<ISelectDefinition>? _excluded;

    public ISelectContext<T> Include(params Expression<Func<T, object>>[] exp)
    {
        _included ??= new List<ISelectDefinition>();
        foreach (var expression in exp)
        {
            _included.Add(new SelectDefinition<T, object>(expression));
        }

        return this;
    }

    public ISelectContext<T> Exclude(params Expression<Func<T, object>>[] exp)
    {
        _excluded ??= new List<ISelectDefinition>();
        foreach (var expression in exp)
        {
            _excluded.Add(new SelectDefinition<T, object>(expression));
        }

        return this;
    }

    SelectFields ISelectDefinitionProvider.GetDefinition() => new(_included, _excluded);
}