using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client;

internal interface IPropertyVisitor
{
    string GetPath(Expression? expression);
}