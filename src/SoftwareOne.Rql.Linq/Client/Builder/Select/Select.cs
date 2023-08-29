using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

internal record Select<T, U>(Expression<Func<T, U>> exp) : ISelect
{
    public string ToQuery()
    {
        var property = new PropertyVisitor().GetPath(exp.Body);
        return property;
    }
}