namespace SoftwareOne.Rql.Abstractions.Collection;

public class RqlAny : RqlCollection
{
    internal RqlAny(RqlExpression left, RqlExpression? right) : base(left, right) { }
}
