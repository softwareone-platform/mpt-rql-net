using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Core;

internal abstract class PathInfoBuilder(IMetadataProvider metadataProvider, IBuilderContext builderContext) : IPathInfoBuilder
{
    public Result<MemberPathInfo> Build(Expression root, RqlExpression rqlExpression)
    {
        switch (rqlExpression)
        {
            case RqlSelf self:
                {
                    if (self.Inner != null)
                        return Build(root, self.Inner);

                    return new MemberPathInfo(RqlPropertyInfo.Root, root);
                }
            case RqlConstant constant:
                {
                    var path = Build(root, constant.Value);
                    if (path.IsError)
                        return path.Errors;
                    return path.Value!;
                }
            default:
                return Error.Validation("Unsupported property node.");
        }
    }

    public Result<MemberPathInfo> Build(Expression root, string path)
    {
        var nameSegments = path.Split('.');
        var safeNavigation = UseSafeNavigation();

        List<Expression>? memberAccess = null;
        if (safeNavigation)
            memberAccess = new List<Expression>(nameSegments.Length);

        var currentType = root.Type;
        RqlPropertyInfo? propInfo = null;
        var pathExpression = root;
        var currentPathLength = 0;

        for (int i = 0; i < nameSegments.Length; i++)
        {
            var segment = nameSegments[i];
            if (currentPathLength > 0)
                currentPathLength++; // account for dot

            currentPathLength += segment.Length;
            var propertyPath = path.AsMemory(0, currentPathLength).ToString();

            if (!metadataProvider.TryGetPropertyByDisplayName(currentType, segment, out propInfo) || propInfo!.Mode == RqlPropertyMode.Ignored)
            {
                return Error.Validation("Invalid property path.", builderContext.GetFullPath(propertyPath));
            }

            var validationResult = ValidatePath(propInfo, propertyPath);

            if (validationResult.IsError)
                return validationResult.Errors;

            // When a collection property has remaining segments, pivot into inner path via Select().FirstOrDefault()
            if (propInfo.Type == RqlPropertyType.Collection && i < nameSegments.Length - 1)
            {
                return BuildCollectionInnerPath(propInfo, pathExpression, nameSegments, i, memberAccess);
            }

            currentType = propInfo.Property.PropertyType;
            pathExpression = Expression.MakeMemberAccess(pathExpression, propInfo!.Property!);

            if (safeNavigation)
            {
                // register member access expressions for further processing
                memberAccess!.Add(pathExpression);
            }
        }

        if (safeNavigation)
        {
            pathExpression = BuildConditionalExpression(memberAccess!, 0);
        }

        return new MemberPathInfo(propInfo!, pathExpression);
    }

    private Result<MemberPathInfo> BuildCollectionInnerPath(
        RqlPropertyInfo collectionProperty,
        Expression parentExpression,
        string[] nameSegments,
        int collectionIndex,
        List<Expression>? priorMemberAccess)
    {
        var collectionExpression = Expression.MakeMemberAccess(parentExpression, collectionProperty.Property);
        var elementType = collectionProperty.ElementType!;
        var innerParam = Expression.Parameter(elementType);
        var remainingPath = string.Join(".", nameSegments.Skip(collectionIndex + 1));

        var innerResult = Build(innerParam, remainingPath);
        if (innerResult.IsError)
            return innerResult.Errors;

        var innerExpression = innerResult.Value!.Expression;
        var innerPropertyInfo = innerResult.Value.PropertyInfo;

        // For value types, use a nullable selector so FirstOrDefault() returns null for empty collections
        // rather than the default value (e.g. 0 for int), which ensures correct null ordering semantics
        var selectorResultType = innerExpression.Type;
        Expression selectorBody = innerExpression;
        if (selectorResultType.IsValueType && Nullable.GetUnderlyingType(selectorResultType) == null)
        {
            selectorResultType = typeof(Nullable<>).MakeGenericType(selectorResultType);
            selectorBody = Expression.Convert(innerExpression, selectorResultType);
        }

        var selectLambda = Expression.Lambda(selectorBody, innerParam);
        var selectMethod = GetSelectMethod(elementType, selectorResultType);
        var firstOrDefaultMethod = GetFirstOrDefaultMethod(selectorResultType);

        var selectCall = Expression.Call(null, selectMethod, collectionExpression, selectLambda);
        Expression finalExpr = Expression.Call(null, firstOrDefaultMethod, selectCall);

        if (UseSafeNavigation())
            finalExpr = WrapWithCollectionNullCheck(collectionExpression, finalExpr, priorMemberAccess);

        return new MemberPathInfo(innerPropertyInfo, finalExpr);
    }

    private static ConditionalExpression WrapWithCollectionNullCheck(
        Expression collectionExpression,
        Expression valueExpression,
        List<Expression>? priorMemberAccess)
    {
        var resultType = valueExpression.Type;

        // Build the condition: full safe-navigation chain including the collection,
        // or just the collection itself when there are no pre-collection references.
        // If any ancestor or the collection itself is null, the condition evaluates to null
        // and we short-circuit, preventing NullReferenceException on the Select() call.
        Expression conditionExpr;
        if (priorMemberAccess != null && priorMemberAccess.Count > 0)
        {
            var withCollection = new List<Expression>(priorMemberAccess) { collectionExpression };
            conditionExpr = BuildConditionalExpression(withCollection, 0);
        }
        else
        {
            conditionExpr = collectionExpression;
        }

        return Expression.Condition(
            Expression.Equal(conditionExpr, Expression.Constant(null, conditionExpr.Type)),
            Expression.Constant(null, resultType),
            valueExpression);
    }

    private static MethodInfo GetSelectMethod(Type elementType, Type resultType)
    {
        return typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
            .First(m => m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2)
            .MakeGenericMethod(elementType, resultType);
    }

    private static MethodInfo GetFirstOrDefaultMethod(Type resultType)
    {
        return typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.Name == nameof(Enumerable.FirstOrDefault) && m.GetParameters().Length == 1)
            .First(m => m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .MakeGenericMethod(resultType);
    }

    private static Expression BuildConditionalExpression(List<Expression> memberAccess, int index)
    {
        if (index == memberAccess.Count - 1)
        {
            return memberAccess[index];
        }


        var currentAccess = memberAccess[index];
        var nextAccess = BuildConditionalExpression(memberAccess, index + 1);
        var nextAccessType = nextAccess.Type;

        if (nextAccessType.IsValueType && Nullable.GetUnderlyingType(nextAccessType) == null)
        {
            // This is a non-nullable value type, make it nullable for the comparison
            nextAccessType = typeof(Nullable<>).MakeGenericType(nextAccessType);
            nextAccess = Expression.Convert(nextAccess, nextAccessType);
        }

        return Expression.Condition(
            Expression.Equal(currentAccess, Expression.Constant(null, currentAccess.Type)),
            Expression.Constant(null, nextAccessType),
            nextAccess);
    }

    protected abstract Result<bool> ValidatePath(RqlPropertyInfo property, string path);

    /// <summary>
    /// Determines whether safe navigation operators (?.) should be used based on implementation type and settings
    /// </summary>
    protected abstract bool UseSafeNavigation();
}
