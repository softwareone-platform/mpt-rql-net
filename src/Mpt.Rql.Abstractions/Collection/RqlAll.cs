namespace Mpt.Rql.Abstractions.Collection;

public class RqlAll : RqlCollection
{
    internal RqlAll(RqlExpression left, RqlExpression right) : base(left, right) { }
}
