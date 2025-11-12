using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using System.Linq.Expressions;

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
        var currentPathLenght = 0;

        foreach (var segment in nameSegments)
        {
            if (currentPathLenght > 0)
                currentPathLenght++; // account for dot

            currentPathLenght += segment.Length;
            var propertyPath = path.AsMemory(0, currentPathLenght).ToString();

            if (!metadataProvider.TryGetPropertyByDisplayName(currentType, segment, out propInfo) || propInfo!.IsIgnored)
            {
                return Error.Validation("Invalid property path.", builderContext.GetFullPath(propertyPath));
            }

            var validationResult = ValidatePath(propInfo, propertyPath);

            if (validationResult.IsError)
                return validationResult.Errors;

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
