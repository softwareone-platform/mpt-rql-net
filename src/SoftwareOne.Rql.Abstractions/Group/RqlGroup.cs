namespace SoftwareOne.Rql.Abstractions.Group
{
    public abstract class RqlGroup : RqlExpression
    {
        private readonly List<RqlExpression> _expressions;

        private protected RqlGroup(IEnumerable<RqlExpression> expressions)
        {
            _expressions = expressions.ToList();
        }

        public IReadOnlyList<RqlExpression>? Items => _expressions;

        public void Add(RqlExpression expression)
            => _expressions.Add(expression);
    }
}
