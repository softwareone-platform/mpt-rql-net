﻿using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class LessThanOrEqual : ComparisonOperator, ILessThanOrEqual
{
    protected override RqlOperators Operator => RqlOperators.Le;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.LessThanOrEqual;
}