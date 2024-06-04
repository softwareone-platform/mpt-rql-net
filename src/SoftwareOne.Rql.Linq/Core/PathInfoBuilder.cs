using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Argument;
using SoftwareOne.Rql.Abstractions.Argument.Pointer;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Core.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Core
{
    internal abstract class PathInfoBuilder : IPathInfoBuilder
    {
        private readonly IMetadataProvider _metadataProvider;

        protected PathInfoBuilder(IMetadataProvider metadataProvider)
        {
            _metadataProvider = metadataProvider;
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
                        return Error.Validation("Invalid property path.", path: cumulativePath.ToString());

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
}
