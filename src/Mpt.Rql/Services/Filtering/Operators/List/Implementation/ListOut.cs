using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.List.Implementation;

internal class ListOut : ListIn, IListOut
{
    public override Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, IEnumerable<string> list)
    {
        var expression = base.MakeExpression(propertyInfo, member, list);
        return expression.IsError ? expression : Expression.Not(expression.Value!);
    }

    protected override RqlOperators Operator => RqlOperators.ListOut;
}