namespace SoftwareOne.Rql.Abstractions.Binary
{
    public class RqlListOut : RqlBinary
    {
        internal RqlListOut(RqlExpression left, RqlExpression right) : base(left, right)
        {
        }
    }
}
