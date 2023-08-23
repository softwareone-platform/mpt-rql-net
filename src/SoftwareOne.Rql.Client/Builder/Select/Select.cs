using System.Linq.Expressions;

namespace SoftwareOne.Rql.Client.Builder.Select;

public record Select<T, U>(Expression<Func<T, U>> exp) : ISelect
{
    public string ToQuery()
    {
        var property = new PropertyVisitor().GetPath(exp.Body);
        return property;
    }
}