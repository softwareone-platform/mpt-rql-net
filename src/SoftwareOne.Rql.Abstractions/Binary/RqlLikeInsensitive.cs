namespace SoftwareOne.Rql.Abstractions.Binary
{
    public class RqlLikeInsensitive : RqlBinary
    {
        internal RqlLikeInsensitive(RqlExpression left, RqlExpression right) : base(left, right)
        {
        }
    }
}
