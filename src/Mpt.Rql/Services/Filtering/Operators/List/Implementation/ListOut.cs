using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.List.Implementation;

internal class ListOut : ListIn, IListOut
{
    public override Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, IEnumerable<string> list)
    {
        var expression = base.MakeExpression(propertyInfo, accessor, list);
        return expression.IsError ? expression : Expression.Not(expression.Value!);
    }

    protected override RqlOperators Operator => RqlOperators.ListOut;
}