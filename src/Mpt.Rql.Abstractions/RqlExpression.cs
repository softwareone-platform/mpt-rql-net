using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Unary;

namespace Mpt.Rql.Abstractions;

public abstract class RqlExpression
{
    #region Group

    public static RqlAnd And(params RqlExpression[] expressions)
        => And((IEnumerable<RqlExpression>)expressions);

    public static RqlAnd And(IEnumerable<RqlExpression> expressions)
        => new(expressions);

    public static RqlOr Or(params RqlExpression[] expressions)
        => Or((IEnumerable<RqlExpression>)expressions);

    public static RqlOr Or(IEnumerable<RqlExpression> expressions)
        => new(expressions);

    public static RqlGenericGroup Group(string name, params RqlExpression[] expressions)
        => Group(name, (IEnumerable<RqlExpression>)expressions);

    public static RqlGenericGroup Group(string name, IEnumerable<RqlExpression> expressions)
        => new(name, expressions);

    #endregion

    #region Binary

    public static RqlEqual Equal(RqlExpression left, RqlExpression right)
        => new(left, right);

    public static RqlNotEqual NotEqual(RqlExpression left, RqlExpression right)
        => new(left, right);

    public static RqlGreaterThan GreaterThan(RqlExpression left, RqlExpression right)
        => new(left, right);

    public static RqlGreaterThanOrEqual GreaterThanOrEqual(RqlExpression left, RqlExpression right)
        => new(left, right);

    public static RqlLessThan LessThan(RqlExpression left, RqlExpression right)
        => new(left, right);

    public static RqlLessThanOrEqual LessThanOrEqual(RqlExpression left, RqlExpression right)
        => new(left, right);

    public static RqlLike Like(RqlExpression left, RqlExpression right)
        => new(left, right);

    public static RqlLikeCaseInsensitive LikeCaseInsensitive(RqlExpression left, RqlExpression right)
        => new(left, right);

    public static RqlListIn ListIn(RqlExpression left, RqlExpression right)
        => new(left, right);

    public static RqlListOut ListOut(RqlExpression left, RqlExpression right)
        => new(left, right);

    public static RqlAny Any(RqlExpression left, RqlExpression? right = null)
        => new(left, right);

    public static RqlAll All(RqlExpression left, RqlExpression right) =>
        new(left, right);

    #endregion

    #region Unary

    public static RqlNot Not(RqlExpression expression)
        => new(expression);

    #endregion

    #region Arguments

    public static RqlConstant Constant(string value, bool isQuoted = false)
        => new(value, isQuoted);

    public static RqlNull Null()
        => new();

    public static RqlEmpty Empty()
        => new();

    #endregion

    #region Pointers

    public static RqlSelf Self(RqlExpression? inner = null)
        => new(inner);

    #endregion
}
