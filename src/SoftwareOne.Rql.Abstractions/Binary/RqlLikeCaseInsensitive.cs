namespace SoftwareOne.Rql.Abstractions.Binary;

public class RqlLikeCaseInsensitive : RqlBinary
{
    internal RqlLikeCaseInsensitive(RqlExpression left, RqlExpression right) : base(left, right)
    {
    }
}
