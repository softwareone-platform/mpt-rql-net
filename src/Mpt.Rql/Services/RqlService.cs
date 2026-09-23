using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services;

internal abstract class RqlService
{
    internal record MemberPathInfo(string FullPath, ReadOnlyMemory<char> Path, RqlPropertyInfo PropertyInfo, Expression Expression);

    protected string MakeErrorCode(string subCode) => $"{ErrorPrefix}:{subCode}";

    protected abstract string ErrorPrefix { get; }

    /// <summary>
    /// Parses an RQL expression, converting parser exceptions into a <c>&lt;prefix&gt;:malformed</c>
    /// validation error so malformed client input never escapes <c>Transform</c> as an exception.
    /// </summary>
    protected bool TryParse(IRqlParser parser, string expression, out RqlGroup node, out Error? error)
    {
        try
        {
            node = parser.Parse(expression);
            error = null;
            return true;
        }
        catch (System.Exception ex) when (IsParserException(ex))
        {
            node = null!;
            error = Error.Validation($"Malformed {ErrorPrefix} expression: {ex.Message}", MakeErrorCode("malformed"));
            return false;
        }
    }

    // The parser exception types share no base class and live in Abstractions, which this change does not touch.
    private static bool IsParserException(System.Exception ex)
        => ex is ArgumentOutOfRangeException // what the linear parser throws today on an unterminated quote (filter=")
            or RqlParserException
            or RqlBinaryParserException
            or RqlCollectionParserException
            or RqlUnaryParserException
            or RqlArgumentParserException
            or RqlPointerParserException;
}
