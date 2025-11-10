namespace SoftwareOne.Rql.Abstractions.Binary;

public class RqlGreaterThan : RqlBinary
{
    internal RqlGreaterThan(RqlExpression left, RqlExpression right) : base(left, right)
    {
    }
}
