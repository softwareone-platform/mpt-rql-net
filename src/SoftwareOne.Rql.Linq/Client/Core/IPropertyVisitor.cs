using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Core;

internal interface IPropertyVisitor
{
    string GetPath(Expression? expression);
}