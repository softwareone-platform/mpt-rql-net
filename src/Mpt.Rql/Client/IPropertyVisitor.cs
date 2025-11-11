using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Client;

public interface IPropertyVisitor
{
    string GetPath(Expression? expression);
}