using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering.Builders;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Everything an <see cref="IOrderingFunction"/> may use while building its key.
/// </summary>
/// <param name="Root">Parameter representing the root entity (<c>TView</c>).</param>
/// <param name="Arguments">The parsed function arguments, in order.</param>
/// <param name="PathBuilder">Ordering path builder (validates the <c>Order</c> action, applies ordering navigation).</param>
/// <param name="FilterBuilder">Filtering expression builder used for predicate arguments.</param>
/// <param name="BuilderContext">Shared builder context; drives error-path prefixes.</param>
/// <param name="Settings">Effective settings for the current request.</param>
internal sealed record OrderingFunctionContext(
    ParameterExpression Root,
    IReadOnlyList<RqlExpression> Arguments,
    IOrderingPathInfoBuilder PathBuilder,
    IExpressionBuilder FilterBuilder,
    IBuilderContext BuilderContext,
    IRqlSettings Settings);
