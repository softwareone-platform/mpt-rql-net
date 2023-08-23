using ErrorOr;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services;
internal abstract class RqlService
{
    private readonly IMetadataProvider _metadataProvider;

    protected RqlService(IMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider;
    }

    protected ErrorOr<MemberExpression> MakeMemberAccess(ParameterExpression pe, string path, Func<MemberPathInfo, ErrorOr<Success>>? pathHandler = null)
    {
        var nameSegments = path.Split('.');
        var aggregatedInfo = nameSegments.Aggregate(
            ErrorOrFactory.From(new MemberPathInfo(path, path.AsMemory(0, 0), null!, pe)),
            (current, segment) =>
            {
                if (current.IsError)
                    return current;

                var previousLength = current.Value.Path.Length;
                var cumulativePath = current.Value.FullPath.AsMemory(0, (previousLength > 0 ? previousLength + 1 : previousLength) + segment.Length);

                if (!_metadataProvider.TryGetPropertyByDisplayName(current.Value.Expression.Type, segment, out var propInfo))
                    return Error.Validation(MakeErrorCode(cumulativePath.ToString()), "Invalid property path.");

                var expression = (Expression)Expression.MakeMemberAccess(current.Value!.Expression, propInfo!.Property);
                var pathInfo = new MemberPathInfo(current.Value.FullPath, cumulativePath, propInfo, expression);

                if (pathHandler == null)
                {
                    return pathInfo;
                }

                var handlerRes = pathHandler(pathInfo);
                if (handlerRes.IsError)
                    return handlerRes.Errors;

                return pathInfo;
            });

        if (aggregatedInfo.IsError)
            return aggregatedInfo.Errors;

        return (MemberExpression)aggregatedInfo.Value.Expression;
    }

    internal record MemberPathInfo(string FullPath, ReadOnlyMemory<char> Path, RqlPropertyInfo PropertyInfo, Expression Expression);

    protected string MakeErrorCode(string subCode) => $"{ErrorPrefix}:{subCode}";

    protected abstract string ErrorPrefix { get; }
}