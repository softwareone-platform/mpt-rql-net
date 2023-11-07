namespace SoftwareOne.Rql.Abstractions.Collection
{
    public abstract class RqlCollection : RqlExpression
    {
        private readonly RqlExpression _left;
        private readonly RqlExpression? _right;

        private protected RqlCollection(RqlExpression left, RqlExpression? right)
        {
            _left = left;
            _right = right;
        }

        public RqlExpression Left => _left;
        public RqlExpression? Right => _right;
    }
}
