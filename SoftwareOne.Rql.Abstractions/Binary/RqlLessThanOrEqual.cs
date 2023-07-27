namespace SoftwareOne.Rql.Abstractions.Binary
{
    public class RqlLessThanOrEqual : RqlBinary
    {
        internal RqlLessThanOrEqual(RqlExpression left, RqlExpression right) : base(left, right)
        {
        }
    }
}
