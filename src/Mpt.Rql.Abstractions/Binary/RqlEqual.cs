namespace Mpt.Rql.Abstractions.Binary;

public class RqlEqual : RqlBinary
{
    internal RqlEqual(RqlExpression left, RqlExpression right) : base(left, right)
    {
    }
}
