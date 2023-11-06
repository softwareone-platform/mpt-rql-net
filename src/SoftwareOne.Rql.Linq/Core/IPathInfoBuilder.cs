using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Core
{
    internal interface IPathInfoBuilder
    {
        ErrorOr<MemberPathInfo> Build(Expression root, RqlExpression rqlExpression);

        ErrorOr<MemberPathInfo> Build(Expression root, string path);
    }

    internal record MemberPathInfo(string FullPath, ReadOnlyMemory<char> Path, RqlPropertyInfo PropertyInfo, Expression Expression);
}
