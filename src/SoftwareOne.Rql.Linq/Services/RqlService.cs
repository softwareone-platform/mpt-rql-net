using ErrorOr;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services
{
    internal abstract class RqlService
    {
        private readonly ITypeMetadataProvider _typeNameMaper;

        public RqlService(ITypeMetadataProvider typeNameMaper)
        {
            _typeNameMaper = typeNameMaper;
        }

        protected ErrorOr<MemberExpression> MakeMemberAccess(ParameterExpression pe, string path, Func<MemberPathInfo, ErrorOr<Success>>? pathHandler = null)
        {
            var nameSegments = path.Split('.');
            var pathinfo = nameSegments.Aggregate(
                ErrorOrFactory.From(new MemberPathInfo(path, path.AsMemory(0, 0), null!, pe)),
                (current, segment) =>
                {
                    if (current.IsError)
                        return current;

                    var propInfo = _typeNameMaper.GetPropertyByDisplayName(current.Value.Expression.Type, segment);
                    var prevLenght = current.Value.Path.Length;
                    var cumulPath = current.Value.FullPath.AsMemory(0, (prevLenght > 0 ? prevLenght + 1 : prevLenght) + segment.Length);

                    if (propInfo == null)
                        return Error.Validation(MakeErrorCode(cumulPath.ToString()), "Invalid property path.");

                    var expression = (Expression)Expression.MakeMemberAccess(current.Value!.Expression, propInfo.Property);
                    var pInfo = new MemberPathInfo(current.Value.FullPath, cumulPath, propInfo, expression);

                    if (pathHandler != null)
                    {
                        var handlerRes = pathHandler(pInfo);
                        if (handlerRes.IsError)
                            return handlerRes.Errors;
                    }

                    return pInfo;
                });

            if (pathinfo.IsError)
                return pathinfo.Errors;

            return (MemberExpression)pathinfo.Value.Expression;
        }

        internal record MemberPathInfo(string FullPath, ReadOnlyMemory<char> Path, RqlPropertyInfo PropertyInfo, Expression Expression);

        protected string MakeErrorCode(string subCode) => $"{ErrorPrefix}:{subCode}";

        protected abstract string ErrorPrefix { get; }
    }
}
