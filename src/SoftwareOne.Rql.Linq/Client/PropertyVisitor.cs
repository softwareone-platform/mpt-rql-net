using SoftwareOne.Rql.Client.Exceptions;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

internal class PropertyVisitor : ExpressionVisitor, IPropertyVisitor
{
    private readonly Stack<string> _path = new();
    

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member is not PropertyInfo propertyInfo)
        {
            throw new InvalidDefinitionException($"The path {nameof(node)} can only contain properties");
        }
        
        _path.Push(propertyInfo.Name);
        return base.VisitMember(node);
    }

    public string GetPath(Expression? expression)
    {
        Visit(expression);

        if (!_path.Any())
        {
            throw new InvalidDefinitionException($"The path can only contain properties");
        }

        return string.Join('.', _path);
    }
}