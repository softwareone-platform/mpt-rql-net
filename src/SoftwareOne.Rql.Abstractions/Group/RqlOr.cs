namespace SoftwareOne.Rql.Abstractions.Group;

public class RqlOr : RqlGroup
{
    internal RqlOr(IEnumerable<RqlExpression> expressions) : base(expressions) { }
}
