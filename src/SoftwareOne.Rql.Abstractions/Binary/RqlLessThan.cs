using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Abstractions.Binary;

public class RqlLessThan : RqlBinary
{
    internal RqlLessThan(RqlExpression left, RqlExpression right) : base(left, right)
    {
    }
}
