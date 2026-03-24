using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using System.Globalization;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Built-in ordering function that sorts by the value of a specific element in a key/value
/// collection, identified by a filter property and value.
/// </summary>
/// <remarks>
/// <para><b>Syntax:</b> <c>+orderby(collectionProperty,filterPropertyName,filterValue,resultPropertyName)</c></para>
/// <para><b>Example:</b> <c>+orderby(parameters,name,priority,value)</c></para>
/// <para>
/// This translates to:
/// <code>
/// x.Parameters
///   .Where(p => p.Name == "priority")
///   .Select(p => p.Value)
///   .FirstOrDefault()
/// </code>
/// When no element matches the filter the sort key is <c>null</c> (or <c>null</c> for value types),
/// which sorts before any non-null value in ascending order.
/// </para>
/// </remarks>
internal class OrderByOrderingFunction : IOrderingFunction
{
    private readonly IOrderingPathInfoBuilder _pathBuilder;

    public OrderByOrderingFunction(IOrderingPathInfoBuilder pathBuilder)
    {
        _pathBuilder = pathBuilder;
    }

    public string FunctionName => "orderby";

    public Result<Expression> Build(Expression root, IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 4)
            return Error.Validation(
                $"'orderby' requires exactly 4 arguments: " +
                $"(collectionProperty, filterPropertyName, filterValue, resultPropertyName). " +
                $"Got {arguments.Count}.");

        var collectionPropPath = arguments[0];
        var filterPropName = arguments[1];
        var filterValue = arguments[2];
        var resultPropName = arguments[3];

        // Resolve the collection property on the root entity
        var collectionResult = _pathBuilder.Build(root, collectionPropPath);
        if (collectionResult.IsError)
            return collectionResult.Errors;

        var collectionPropInfo = collectionResult.Value!.PropertyInfo;
        if (collectionPropInfo.Type != RqlPropertyType.Collection)
            return Error.Validation($"'{collectionPropPath}' is not a collection property.");

        var collectionExpression = collectionResult.Value.Expression;
        var elementType = collectionPropInfo.ElementType!;
        var innerParam = Expression.Parameter(elementType);

        // Resolve and validate the filter property on the collection element type
        var filterPropResult = _pathBuilder.Build(innerParam, filterPropName);
        if (filterPropResult.IsError)
            return filterPropResult.Errors;

        var filterExpression = filterPropResult.Value!.Expression;

        // Convert the filter value string to the actual property type
        Expression filterConstantExpr;
        try
        {
            var targetType = Nullable.GetUnderlyingType(filterExpression.Type) ?? filterExpression.Type;
            var converted = Convert.ChangeType(filterValue, targetType, CultureInfo.InvariantCulture);
            filterConstantExpr = Expression.Constant(converted, filterExpression.Type);
        }
        catch
        {
            return Error.Validation(
                $"Cannot convert filter value '{filterValue}' to type '{filterExpression.Type.Name}'.");
        }

        // Build predicate: element => element.FilterProperty == filterValue
        var filterPredicate = Expression.Lambda(
            Expression.Equal(filterExpression, filterConstantExpr),
            innerParam);

        // Resolve and validate the result property on the collection element type
        var resultPropResult = _pathBuilder.Build(innerParam, resultPropName);
        if (resultPropResult.IsError)
            return resultPropResult.Errors;

        var resultExpression = resultPropResult.Value!.Expression;

        // For value types, use a nullable selector so an empty/no-match result returns null
        // rather than default(T), giving correct null ordering semantics
        var selectorResultType = resultExpression.Type;
        Expression selectorBody = resultExpression;
        if (selectorResultType.IsValueType && Nullable.GetUnderlyingType(selectorResultType) == null)
        {
            selectorResultType = typeof(Nullable<>).MakeGenericType(selectorResultType);
            selectorBody = Expression.Convert(resultExpression, selectorResultType);
        }

        // Build: collection.Where(predicate).Select(selector).FirstOrDefault()
        var methods = (IWhereSelectMethods)Activator.CreateInstance(
            typeof(WhereSelectMethods<,>).MakeGenericType(elementType, selectorResultType))!;

        var selectLambda = Expression.Lambda(selectorBody, innerParam);
        var whereCall = Expression.Call(null, methods.GetWhere(), collectionExpression, filterPredicate);
        var selectCall = Expression.Call(null, methods.GetSelect(), whereCall, selectLambda);
        return Expression.Call(null, methods.GetFirstOrDefault(), selectCall);
    }
}
