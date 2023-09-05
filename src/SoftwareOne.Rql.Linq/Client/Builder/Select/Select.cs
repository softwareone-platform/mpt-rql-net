using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Select;

internal record Select<T, U>(Expression<Func<T, U>> exp) : IInternalSelect
{
    public string ToQuery(IPropertyVisitor propertyVisitor)
    {
        var property = propertyVisitor.GetPath(exp.Body);
        return property;
    }
}