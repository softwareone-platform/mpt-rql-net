﻿using SoftwareOne.Rql.Linq.Configuration;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class GreaterThanOrEqual(IRqlSettings settings) : ComparisonOperator(settings), IGreaterThanOrEqual
{
    protected override RqlOperators Operator => RqlOperators.Ge;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.GreaterThanOrEqual;
}