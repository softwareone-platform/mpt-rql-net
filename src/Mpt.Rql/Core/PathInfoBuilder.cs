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
        var segments = path.Split('.');
        var memberAccess = new List<Expression>(segments.Length);

        var state = PathWalkState.Initial(root);
        var currentPathLength = 0;

        foreach (var segment in segments.TakeWhile(_ => !state.ResolverConsumedPath))
        {
            if (currentPathLength > 0)
                currentPathLength++; // account for dot
            currentPathLength += segment.Length;

            var advanceState = AdvanceSegment(state, segment, path, currentPathLength);
            if (advanceState.IsError)
                return advanceState.Errors;
            state = advanceState.Value;

            var validation = ValidatePath(state.PropInfo!, state.PropertyPath);
            if (validation.IsError)
                return validation.Errors;

            memberAccess.Add(state.Expression);
        }

        var finalExpression = UseSafeNavigation()
            ? BuildConditionalExpression(memberAccess, 0)
            : state.Expression;

        return new MemberPathInfo(state.PropInfo!, finalExpression);
    }

    /// <summary>
    /// Resolves one path segment against metadata or — if the segment has no CLR counterpart —
    /// the parent property's custom resolver. Returns the advanced walk state or a validation error.
    /// </summary>
    private Result<PathWalkState> AdvanceSegment(PathWalkState state, string segment, string path, int currentPathLength)
    {
        var propertyPath = path[..currentPathLength];

        if (metadataProvider.TryGetPropertyByDisplayName(state.CurrentType, segment, out var resolvedPropInfo))
        {
            if (resolvedPropInfo!.Mode == RqlPropertyMode.Ignored)
                return CreateInvalidPathValidationError(propertyPath);

            return state with
            {
                PropInfo = resolvedPropInfo,
                CurrentType = resolvedPropInfo.Property.PropertyType,
                Expression = Expression.MakeMemberAccess(state.Expression, resolvedPropInfo.Property),
                PropertyPath = propertyPath,
            };
        }

        if (state.PropInfo?.CustomResolver is null)
            return CreateInvalidPathValidationError(propertyPath);

        // Hand the resolver this segment AND every remaining segment as one dotted key, so it can
        // translate a deep path (e.g. "$.a.b.c") in a single call and produce one scalar leaf.
        var remainingSegments = path[(currentPathLength - segment.Length)..];
        if (!TryResolveCustomProperty(state.PropInfo, state.Expression, remainingSegments, out var leaf, out var customPropInfo))
            return CreateInvalidPathValidationError(path);

        return state with
        {
            PropInfo = CreateFromCustomResolver(customPropInfo),
            Expression = leaf,
            PropertyPath = path, // validation messages reference the full path the resolver consumed
            ResolverConsumedPath = true,
        };

        Result<PathWalkState> CreateInvalidPathValidationError(string invalidPath)
        {
            return Error.Validation("Invalid property path.", builderContext.GetFullPath(invalidPath));
        }
    }

    private readonly record struct PathWalkState(
        Expression Expression,
        Type CurrentType,
        RqlPropertyInfo? PropInfo,
        string PropertyPath,
        bool ResolverConsumedPath)
    {
        public static PathWalkState Initial(Expression root) => new(root, root.Type, null, string.Empty, false);
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
