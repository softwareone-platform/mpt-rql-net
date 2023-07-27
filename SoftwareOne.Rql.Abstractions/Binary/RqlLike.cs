using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Abstractions.Binary
{
    public class RqlLike : RqlBinary
    {
        internal RqlLike(RqlExpression left, RqlExpression right) : base(left, right)
        {
        }
    }
}
