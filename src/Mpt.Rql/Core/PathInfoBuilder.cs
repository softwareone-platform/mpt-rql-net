using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using System.Linq.Expressions;

namespace Mpt.Rql.Core;

internal abstract class PathInfoBuilder(IMetadataProvider metadataProvider, IBuilderContext builderContext, IExternalServiceAccessor externalServices) : IPathInfoBuilder
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
        var customResolverConsumedPath = false;

        for (var i = 0; i < nameSegments.Length && !customResolverConsumedPath; i++)
        {
            var segment = nameSegments[i];

            if (currentPathLength > 0)
                currentPathLength++; // account for dot

            currentPathLength += segment.Length;
            var propertyPath = path.AsMemory(0, currentPathLength).ToString();

            var metadataFound = metadataProvider.TryGetPropertyByDisplayName(currentType, segment, out var resolvedPropInfo);
            if (metadataFound && resolvedPropInfo!.Mode != RqlPropertyMode.Ignored)
            {
                // Standard CLR property resolution
                propInfo = resolvedPropInfo;
                currentType = resolvedPropInfo.Property.PropertyType;
                pathExpression = Expression.MakeMemberAccess(pathExpression, resolvedPropInfo.Property);
            }
            else if (!metadataFound && propInfo?.CustomResolver != null)
            {
                // Hand the resolver this segment AND every remaining segment as one dotted key.
                // This lets resolvers translate a deep path (e.g. "$.a.b.c") in a single
                // call and produce one scalar leaf expression.
                var remainingSegments = i == nameSegments.Length - 1
                    ? segment
                    : string.Join('.', nameSegments, i, nameSegments.Length - i);

                if (!TryResolveCustomProperty(propInfo, pathExpression, remainingSegments, out var resolvedExpr, out var customPropInfo))
                    return Error.Validation("Invalid property path.", builderContext.GetFullPath(path));

                propInfo = CreateFromCustomResolver(customPropInfo);
                pathExpression = resolvedExpr;
                propertyPath = path; // validation messages reference the full path the resolver consumed
                customResolverConsumedPath = true;
            }
            else
            {
                return Error.Validation("Invalid property path.", builderContext.GetFullPath(propertyPath));
            }

            var validationResult = ValidatePath(propInfo, propertyPath);

            if (validationResult.IsError)
                return validationResult.Errors;

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

    /// <summary>
    /// Creates an internal <see cref="RqlPropertyInfo"/> from a custom resolver's <see cref="IRqlPropertyInfo"/>.
    /// All fields are copied from the source as-is.
    /// </summary>
    private static RqlPropertyInfo CreateFromCustomResolver(IRqlPropertyInfo source) => new()
    {
        Name = source.Name,
        Property = source.Property,
        Mode = source.Mode,
        Type = source.Type,
        IsCore = source.IsCore,
        SelectModeOverride = source.SelectModeOverride,
        Actions = source.Actions,
        Operators = source.Operators,
        ElementType = source.ElementType,
        IsNullable = source.IsNullable
    };

    private bool TryResolveCustomProperty(
        RqlPropertyInfo ownerPropertyInfo,
        Expression parentExpression,
        string propertyPath,
        out Expression resolvedExpression,
        out IRqlPropertyInfo resolvedPropertyInfo)
    {
        var resolverType = ownerPropertyInfo.CustomResolver!;
        if (externalServices.GetService(resolverType) is not IRqlCustomPropertyResolver resolver)
        {
            throw new RqlInvalidCustomResolverException(
                $"The instance of type {resolverType.FullName} defined as custom resolver for property " +
                $"({ownerPropertyInfo.Property!.DeclaringType!.FullName}).{ownerPropertyInfo.Property.Name} " +
                $"cannot be found. Make sure that service has been registered.");
        }

        return resolver.TryResolve(parentExpression, propertyPath, out resolvedExpression, out resolvedPropertyInfo);
    }

    protected abstract Result<bool> ValidatePath(RqlPropertyInfo property, string path);

    /// <summary>
    /// Determines whether safe navigation operators (?.) should be used based on implementation type and settings
    /// </summary>
    protected abstract bool UseSafeNavigation();
}
