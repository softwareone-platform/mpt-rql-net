using System.Linq.Expressions;

namespace SoftwareOne.Rql.Client;

public interface IPropertyVisitor
{
    string GetPath(Expression? expression);
}