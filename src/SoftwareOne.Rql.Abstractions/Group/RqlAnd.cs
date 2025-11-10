namespace SoftwareOne.Rql.Abstractions.Group;

public class RqlAnd : RqlGroup
{
    internal RqlAnd(IEnumerable<RqlExpression> expressions) : base(expressions) { }
}
