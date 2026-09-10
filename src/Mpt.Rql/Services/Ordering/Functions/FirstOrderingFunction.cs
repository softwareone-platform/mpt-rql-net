using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Built-in ordering function: <c>first(&lt;collection&gt;, [&lt;predicate&gt;,] &lt;path&gt;)</c>.
/// </summary>
/// <remarks>
/// <para>
/// Sort key = <c>collection.Where(e =&gt; predicate).Select(e =&gt; path).FirstOrDefault()</c>; the
/// <c>Where</c> is omitted in the two-argument form. Value-type selectors are lifted to
/// <see cref="Nullable{T}"/> so "no matching element" and "empty collection" both yield <c>null</c>.
/// </para>
/// <para>
/// The predicate is an ordinary RQL filter expression built by the filtering pipeline against an
/// element-scope parameter, so constant conversion, SQL parameterization, operators and
/// <c>Filter</c> permissions are all the filtering pipeline's. The collection and the selector are
/// resolved by the ordering path builder (<c>Order</c> permissions, ordering navigation).
/// </para>
/// <para>
/// Which element is "first" is provider-defined when several elements match; the sort key of an
/// entity with no match is <c>null</c>, whose placement follows the provider.
/// </para>
/// </remarks>
internal sealed class FirstOrderingFunction : IOrderingFunction
{
    public const string FunctionName = "first";

    public string Name => FunctionName;

    public Result<Expression> Build(OrderingFunctionContext context)
    {
        var args = context.Arguments;

        if (args.Count is not (2 or 3))
            return Error.Validation(
                $"'{FunctionName}' requires 2 or 3 arguments: (collection, [predicate,] path). Got {args.Count}.",
                OrderingErrorCodes.FunctionArguments);

        if (args[0] is not RqlConstant collectionArg)
            return Error.Validation($"'{FunctionName}': collection argument must be a property path.", OrderingErrorCodes.FunctionArguments);

        if (args[^1] is not RqlConstant pathArg)
            return Error.Validation($"'{FunctionName}': path argument must be a property path.", OrderingErrorCodes.FunctionArguments);

        var predicateArg = args.Count == 3 ? args[1] : null;

        var collection = context.PathBuilder.Build(context.Root, collectionArg.Value);
        if (collection.IsError)
            return collection.Errors;

        var collectionInfo = collection.Value!.PropertyInfo;
        if (collectionInfo.Type != RqlPropertyType.Collection || collectionInfo.ElementType is null)
            return Error.Validation(
                $"'{collectionArg.Value}' is not a collection property.",
                OrderingErrorCodes.NotCollection,
                context.BuilderContext.GetFullPath(collectionArg.Value));

        var elementType = collectionInfo.ElementType;
        var element = Expression.Parameter(elementType, "e");

        // Enter the collection's graph scope so predicate/selector errors are reported as "collection.prop".
        DescendInto(context.BuilderContext, collectionArg.Value, collectionInfo);
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
                    context.BuilderContext.GetFullPath(pathArg.Value));

            return BuildKey(collection.Value.Expression, elementType, element, predicate, selector.Value.Expression, context.Settings);
        }
        finally
        {
            context.BuilderContext.GoToRoot();
        }
    }

    /// <summary>
    /// Walks the builder context down the collection path segment by segment. The graph stage has
    /// already created these nodes; if any step is missing we stop and later error paths simply lack
    /// the prefix.
    /// </summary>
    private static void DescendInto(IBuilderContext builderContext, string collectionPath, RqlPropertyInfo collectionInfo)
    {
        var segments = collectionPath.Split('.');
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (!builderContext.TryGoToChild(segments[i]))
                return;
        }

        builderContext.TryGoToChild(collectionInfo);
    }

    private static Expression BuildKey(
        Expression collection,
        Type elementType,
        ParameterExpression element,
        Expression? predicate,
        Expression selector,
        IRqlSettings settings)
    {
        // Lift value types so a missing element yields null rather than default(T).
        var resultType = selector.Type;
        var selectorBody = selector;
        if (resultType.IsValueType && Nullable.GetUnderlyingType(resultType) is null)
        {
            resultType = typeof(Nullable<>).MakeGenericType(resultType);
            selectorBody = Expression.Convert(selector, resultType);
        }

        var methods = CollectionValueMethods.For(elementType, resultType);

        var source = collection;
        if (predicate is not null)
            source = Expression.Call(methods.Where, source, Expression.Lambda(predicate, element));

        var selected = Expression.Call(methods.Select, source, Expression.Lambda(selectorBody, element));
        Expression key = Expression.Call(methods.FirstOrDefault, selected);

        if (settings.Ordering.Navigation == NavigationStrategy.Safe)
        {
            key = Expression.Condition(
                Expression.Equal(collection, Expression.Constant(null, collection.Type)),
                Expression.Constant(null, resultType),
                key);
        }

        return key;
    }
}
