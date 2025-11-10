namespace Mpt.Rql.Abstractions.Argument.Pointer;

public class RqlSelf : RqlPointer
{
    internal RqlSelf(RqlExpression? inner = null) : base(inner) { }
}
