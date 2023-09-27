namespace SoftwareOne.Rql.Abstractions.Binary;

public class RqlAny : RqlBinary
{
    internal RqlAny(RqlExpression left, RqlExpression right) : base(left, right) { }
}
