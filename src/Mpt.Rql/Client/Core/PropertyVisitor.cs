using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Client.Core;

internal class PropertyVisitor : ExpressionVisitor, IPropertyVisitor
{
    private readonly Stack<string> _path = new();
    private readonly IPropertyNameProvider _nameProvider;

    public PropertyVisitor(IPropertyNameProvider nameProvider)
    {
        _nameProvider = nameProvider;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member is not PropertyInfo propertyInfo)
        {
            throw new InvalidDefinitionException($"The path {nameof(node)} can only contain properties");
        }

        _path.Push(_nameProvider.GetName(propertyInfo));
        return base.VisitMember(node);
    }

    public string GetPath(Expression? expression)
    {
        _path.Clear();
        Visit(expression);

        if (!_path.Any())
        {
            throw new InvalidDefinitionException($"The path can only contain properties");
        }

        return string.Join('.', _path);
    }
}