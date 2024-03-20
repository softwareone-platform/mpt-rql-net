using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Mapping;

internal class ReplaceParameterVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly Expression _newExpression;

    public ReplaceParameterVisitor(ParameterExpression oldParameter, Expression newExpression)
    {
        _oldParameter = oldParameter;
        _newExpression = newExpression;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        // Replace the specified parameter with the new expression
        if (node == _oldParameter)
        {
            return _newExpression;
        }

        return base.VisitParameter(node);
    }
}