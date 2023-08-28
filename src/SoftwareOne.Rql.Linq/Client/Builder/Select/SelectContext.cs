using System.Collections.Immutable;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Builder.Select;

public class SelectContext<T> where T : class
{
    private new IList<ISelect> Included = new List<ISelect>();
    private new IList<ISelect> Excluded = new List<ISelect>();

    public SelectContext<T> Include<U>(Expression<Func<T, U>> exp)
    {
        Included.Add(new Select<T, U>(exp));
        return this;
    }

    public SelectContext<T> Exclude<U>(Expression<Func<T, U>> exp)
    {
        Excluded.Add(new Select<T, U>(exp));
        return this;
    }

    public SelectFields GetDefinition()
    {
        return new SelectFields(Included.ToImmutableList(), Excluded.ToImmutableList());
    }
}