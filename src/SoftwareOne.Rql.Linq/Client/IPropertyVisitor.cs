using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client;

public interface IPropertyVisitor
{
    string GetPath(Expression? expression);
}