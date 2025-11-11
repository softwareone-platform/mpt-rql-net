using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using System.Linq.Expressions;

namespace Mpt.Rql.Core;

internal abstract class PathInfoBuilder : IPathInfoBuilder
{
    private readonly IMetadataProvider _metadataProvider;
    private readonly IBuilderContext _builderContext;

    protected PathInfoBuilder(IMetadataProvider metadataProvider, IBuilderContext builderContext)
    {
        _metadataProvider = metadataProvider;
        _builderContext = builderContext;
    }

    public Result<MemberPathInfo> Build(Expression root, RqlExpression rqlExpression)
    {
        switch (rqlExpression)
        {
            case RqlSelf self:
                {
                    if (self.Inner != null)
                        return Build(root, self.Inner);

                    var path = string.Empty;
                    return new MemberPathInfo(path, path.AsMemory(0, 0), RqlPropertyInfo.Root, root);
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
        var aggregatedInfo = nameSegments.Aggregate(
            new Result<MemberPathInfo>(new MemberPathInfo(path, path.AsMemory(0, 0), null!, root)),
            (current, segment) =>
            {
                if (current.IsError)
                    return current;

                var previousLength = current.Value!.Path.Length;
                var cumulativePath = current.Value.FullPath.AsMemory(0, (previousLength > 0 ? previousLength + 1 : previousLength) + segment.Length);

                if (!_metadataProvider.TryGetPropertyByDisplayName(current.Value.Expression.Type, segment, out var propInfo) || propInfo!.IsIgnored)
                    return Error.Validation("Invalid property path.", _builderContext.GetFullPath(cumulativePath.ToString()));

                var expression = (Expression)Expression.MakeMemberAccess(current.Value!.Expression, propInfo!.Property!);
                var pathInfo = new MemberPathInfo(current.Value.FullPath, cumulativePath, propInfo, expression);

                var validationResult = ValidatePath(pathInfo);

                if (validationResult.IsError)
                    return validationResult.Errors;

                return pathInfo;
            });

        if (aggregatedInfo.IsError)
            return aggregatedInfo.Errors;

        return aggregatedInfo.Value!;
    }

    protected abstract Result<bool> ValidatePath(MemberPathInfo pathInfo);
}
