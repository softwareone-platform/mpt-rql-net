namespace SoftwareOne.Rql.Abstractions.Binary;

public class RqlNotEqual : RqlBinary
{
    public RqlNotEqual(RqlExpression left, RqlExpression right) : base(left, right)
    {
    }
}
