﻿using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Builders;

internal interface IConcreteExpressionBuilder<TNode> where TNode : RqlExpression
{
    Result<Expression> Build(ParameterExpression pe, TNode node);
}
