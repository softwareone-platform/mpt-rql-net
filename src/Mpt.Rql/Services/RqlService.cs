using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services;

internal abstract class RqlService
{
    internal record MemberPathInfo(string FullPath, ReadOnlyMemory<char> Path, RqlPropertyInfo PropertyInfo, Expression Expression);

    protected string MakeErrorCode(string subCode) => $"{ErrorPrefix}:{subCode}";

    protected abstract string ErrorPrefix { get; }
}