using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace SoftwareOne.Rql.Client;

internal class PropertyVisitor : ExpressionVisitor, IPropertyVisitor
{
    internal readonly Stack<MemberInfo> Path = new();

    protected override Expression VisitMember(MemberExpression node)
    {
        if (!(node.Member is PropertyInfo))
        {
            throw new NotSupportedException($"The path {nameof(node)} can only contain properties");
        }

        this.Path.Push(node.Member);
        return base.VisitMember(node);
    }

    public string GetPath(Expression? expression)
    {
        Visit(expression);

        if (!Path.Any())
        {
            throw new NotSupportedException($"The path can only contain properties");
        }

        return string.Join('.', Path.Select(x => x.Name));
    }
}