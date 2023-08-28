using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

internal interface IPropertyVisitor
{
    string GetPath(Expression? expression);
}