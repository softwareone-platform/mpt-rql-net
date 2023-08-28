using System.Linq.Expressions;
using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Builder.Select;

public record Select<T, U>(Expression<Func<T, U>> exp) : ISelect
{
    public string ToQuery()
    {
        var property = new PropertyVisitor().GetPath(exp.Body);
        return property;
    }
}