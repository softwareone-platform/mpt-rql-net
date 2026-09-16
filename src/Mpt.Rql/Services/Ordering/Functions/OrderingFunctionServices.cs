using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering.Builders;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// The request-scoped services an ordering function needs, bundled so <c>OrderingService</c> can hand them to
/// <see cref="OrderingFunctionContext"/> without couriering each one through its own constructor.
/// </summary>
internal sealed record OrderingFunctionServices(
    IOrderingPathInfoBuilder PathBuilder,
    IExpressionBuilder FilterBuilder,
    IBuilderContext BuilderContext,
    IRqlSettings Settings,
    OrderingFunctionRegistry Functions);
