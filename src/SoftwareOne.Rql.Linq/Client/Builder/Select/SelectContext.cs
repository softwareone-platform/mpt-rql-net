using System.Collections.Immutable;
using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public class SelectContext<T> where T : class
{
    private readonly IList<ISelect> _included = new List<ISelect>();
    private readonly IList<ISelect> _excluded = new List<ISelect>();

    public SelectContext<T> Include<U>(Expression<Func<T, U>> exp)
    {
        _included.Add(new Select<T, U>(exp));
        return this;
    }

    public SelectContext<T> Exclude<U>(Expression<Func<T, U>> exp)
    {
        _excluded.Add(new Select<T, U>(exp));
        return this;
    }

    internal SelectFields GetDefinition()
    {
        return new SelectFields(_included.ToImmutableList(), _excluded.ToImmutableList());
    }
}