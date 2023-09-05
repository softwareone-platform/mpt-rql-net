using SoftwareOne.Rql.Client;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Select;

internal class SelectContext<T> : ISelectDefinitionProvider, ISelectContext<T> where T : class
{
    private IList<ISelect>? _included;
    private IList<ISelect>? _excluded;

    public ISelectContext<T> Include<U>(Expression<Func<T, U>> exp)
    {
        _included ??= new List<ISelect>();
        _included.Add(new Select<T, U>(exp));
        return this;
    }

    public ISelectContext<T> Exclude<U>(Expression<Func<T, U>> exp)
    {
        _excluded ??= new List<ISelect>();
        _excluded.Add(new Select<T, U>(exp));
        return this;
    }

    SelectFields ISelectDefinitionProvider.GetDefinition() => new(_included, _excluded);
}