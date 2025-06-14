﻿using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation;

internal class ListOut : ListIn, IListOut
{
    public override Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, IEnumerable<string> list)
    {
        var expression = base.MakeExpression(propertyInfo, member, list);
        return expression.IsError ? expression : Expression.Not(expression.Value!);
    }

    protected override RqlOperators Operator => RqlOperators.ListOut;
}