using Mpt.Rql.Abstractions;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Core;

internal interface IPathInfoBuilder
{
    Result<MemberPathInfo> Build(Expression root, RqlExpression rqlExpression);

    Result<MemberPathInfo> Build(Expression root, string path);
}

internal record MemberPathInfo(string FullPath, ReadOnlyMemory<char> Path, RqlPropertyInfo PropertyInfo, Expression Expression);
