using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Core;

internal interface IPathInfoBuilder
{
    Result<MemberPathInfo> Build(Expression root, RqlExpression rqlExpression);

    Result<MemberPathInfo> Build(Expression root, string path);
}

internal record MemberPathInfo(string FullPath, ReadOnlyMemory<char> Path, RqlPropertyInfo PropertyInfo, Expression Expression);
