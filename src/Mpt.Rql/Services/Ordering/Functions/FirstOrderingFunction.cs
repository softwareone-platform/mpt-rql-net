using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Expressions;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Mapping;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Built-in ordering function: <c>first(&lt;collection&gt;, &lt;path&gt;[, &lt;predicate&gt;])</c>.
/// </summary>
/// <remarks>
/// <para>
/// Sort key = <c>collection.Where(e =&gt; predicate).Select(e =&gt; path).FirstOrDefault()</c>; the
/// <c>Where</c> is omitted in the two-argument form. Arguments are ordered required-first: the
/// collection, then the path, then the optional predicate. Value-type selectors are lifted to
/// <see cref="Nullable{T}"/> so "no matching element" and "empty collection" both yield <c>null</c>.
/// </para>
/// <para>
/// The predicate is an ordinary RQL filter expression built by the filtering pipeline against an
/// element-scope parameter, so constant conversion, SQL parameterization, operators and
/// <c>Filter</c> permissions are all the filtering pipeline's. Because it is part of the ordering
/// key, it is built under the <em>ordering</em> navigation strategy (applied to the request-scoped
/// filter settings for the duration of the build). The collection and the selector are resolved by
/// the ordering path builder (<c>Order</c> permissions, ordering navigation).
/// </para>
/// <para>
/// The collection itself is never null-checked: EF Core cannot translate
/// <c>collection == null ? … : …</c> for collection navigations (they are never null in SQL), so
/// under LINQ-to-Objects a <c>null</c> collection throws exactly as <c>any()</c> does. Null guards the
/// path builder emits for a dotted <em>prefix</em> (<c>reference == null ? null : reference.orders</c>)
/// are moved from the source to the finished key, which keeps them translatable and null-safe.
/// </para>
/// <para>
/// Which element is "first" is provider-defined when several elements match; the sort key of an
/// entity with no match is <c>null</c>, whose placement follows the provider.
/// </para>
/// </remarks>
internal sealed class FirstOrderingFunction : IOrderingFunction
{
    // Stateless by design: registered as a singleton, everything per-request arrives via the context.
    public const string FunctionName = "first";

    public string Name => FunctionName;

    public void IncludeInGraph(IOrderingFunctionGraph graph, RqlNode target, IReadOnlyList<RqlExpression> arguments)
    {
        // Wildcards (also signed: "+*") are never valid here; Build reports the error, and we must not fan out the graph meanwhile.
        if (arguments.Count is not (2 or 3) || arguments.Any(IsWildcard))
            return;

        var collectionNode = graph.IncludeHierarchy(target, arguments[0]);
        if (collectionNode is null)
            return;

        graph.IncludeOrderPath(collectionNode, arguments[1]);

        if (arguments.Count == 3)
            graph.TraversePredicate(collectionNode, arguments[2]);
    }

    public Result<Expression> Build(OrderingFunctionContext context)
    {
        var args = context.Arguments;

        if (args.Count is not (2 or 3))
            return Error.Validation(
                $"'{FunctionName}' requires 2 or 3 arguments: (collection, path[, predicate]). Got {args.Count}.",
                OrderingErrorCodes.FunctionArguments);

        if (args[0] is not RqlConstant collectionArg)
            return Error.Validation($"'{FunctionName}': collection argument must be a property path.", OrderingErrorCodes.FunctionArguments);

        if (args[1] is not RqlConstant pathArg)
            return Error.Validation($"'{FunctionName}': path argument must be a property path.", OrderingErrorCodes.FunctionArguments);

        var predicateArg = args.Count == 3 ? args[2] : null;

        var collection = context.PathBuilder.Build(context.Root, collectionArg.Value);
        if (collection.IsError)
            return collection.Errors;

        var collectionInfo = collection.Value!.PropertyInfo;
        var collectionExpression = collection.Value.Expression;

        // Struct enumerables (e.g. ImmutableArray<T>) are not reference-assignable to IEnumerable<T>,
        // so Expression.Call would throw; reject them as validation errors instead.
        if ((collectionInfo.TypeOverride ?? collectionInfo.Type) != RqlPropertyType.Collection || collectionInfo.ElementType is null || collectionExpression.Type.IsValueType)
            return Error.Validation(
                $"'{collectionArg.Value}' is not a collection property.",
                OrderingErrorCodes.NotCollection,
                context.BuilderContext.GetFullPath(collectionArg.Value));

        var elementType = collectionInfo.ElementType;
        var element = Expression.Parameter(elementType, "e");

        var builderContext = context.BuilderContext;
        var filterSettings = context.Settings.Filter;
        var originalFilterNavigation = filterSettings.Navigation;

        // Enter the collection's graph scope so predicate/selector errors are reported as "collection.prop",
        // and build the predicate under the ordering navigation strategy (settings are request-scoped).
        DescendInto(builderContext, collectionArg.Value);
        filterSettings.Navigation = context.Settings.Ordering.Navigation;
        try
        {
            Expression? predicate = null;
            if (predicateArg is not null)
            {
                var predicateResult = context.FilterBuilder.Build(element, predicateArg);
                if (predicateResult.IsError)
                    return predicateResult.Errors;
                predicate = predicateResult.Value!;
            }

            var selector = context.PathBuilder.Build(element, pathArg.Value);
            if (selector.IsError)
                return selector.Errors;

            var selectorInfo = selector.Value!.PropertyInfo;
            if ((selectorInfo.TypeOverride ?? selectorInfo.Type) != RqlPropertyType.Primitive)
                return Error.Validation(
                    $"'{FunctionName}': path must resolve to a primitive property.",
                    OrderingErrorCodes.NotPrimitive,
                    builderContext.GetFullPath(pathArg.Value));

            return BuildKey(collectionExpression, elementType, element, predicate, selector.Value.Expression);
        }
        finally
        {
            filterSettings.Navigation = originalFilterNavigation;
            builderContext.GoToRoot();
        }
    }

    private static bool IsWildcard(RqlExpression argument)
        => argument is RqlConstant constant && StringHelper.ExtractSign(constant.Value).value.Span.SequenceEqual("*".AsSpan());

    /// <summary>
    /// Walks the builder context down the collection path segment by segment (graph node names are
    /// matched case-insensitively, like metadata). The graph stage has already created these nodes; if
    /// a step is missing we stop and later error paths simply lack the prefix.
    /// </summary>
    private static void DescendInto(IBuilderContext builderContext, string collectionPath)
    {
        var segments = collectionPath.Split('.');
        var i = 0;
        while (i < segments.Length && builderContext.TryGoToChild(segments[i]))
            i++;
    }

    private static Expression BuildKey(
        Expression collection,
        Type elementType,
        ParameterExpression element,
        Expression? predicate,
        Expression selector)
    {
        // Under safe navigation the path builder returns `prefix == null ? null : prefix.collection`. A null
        // source would make Where() throw and EF cannot translate a null test on the collection itself, so
        // peel the prefix guards off the source and re-apply them around the finished key.
        var prefixGuards = new List<Expression>();
        var source = collection;
        while (source is ConditionalExpression { IfTrue: ConstantExpression { Value: null } } guard)
        {
            prefixGuards.Add(guard.Test);
            source = guard.IfFalse;
        }

        // Lift value types so a missing element yields null rather than default(T).
        var selectorBody = NullableExpressionHelper.LiftToNullable(selector);

        var functions = (IProjectionFunctions)Activator.CreateInstance(
            typeof(ProjectionFunctions<,>).MakeGenericType(elementType, selectorBody.Type))!;

        if (predicate is not null)
            source = Expression.Call(functions.GetWhere(), source, Expression.Lambda(predicate, element));

        var selected = Expression.Call(functions.GetSelect(), source, Expression.Lambda(selectorBody, element));
        Expression key = Expression.Call(functions.GetFirstOrDefault(), selected);

        for (var i = prefixGuards.Count - 1; i >= 0; i--)
            key = Expression.Condition(prefixGuards[i], Expression.Constant(null, key.Type), key);

        return key;
    }
}
