namespace Mpt.Rql.Abstractions.Binary;

public class RqlGreaterThanOrEqual : RqlBinary
{
    internal RqlGreaterThanOrEqual(RqlExpression left, RqlExpression right) : base(left, right)
    {
    }
}
