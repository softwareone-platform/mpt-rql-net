using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Filtering.Operators.List;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.List.Implementation;

internal class ListOut : ListIn, IListOut
{
    public override Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, IEnumerable<string> list)
    {
        var expression = base.MakeExpression(propertyInfo, member, list);
        return expression.IsError ? expression : Expression.Not(expression.Value!);
    }

    protected override RqlOperators Operator => RqlOperators.ListOut;
}