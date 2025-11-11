using System.Linq.Expressions;

namespace Mpt.Rql.Client;

public interface IPropertyVisitor
{
    string GetPath(Expression? expression);
}