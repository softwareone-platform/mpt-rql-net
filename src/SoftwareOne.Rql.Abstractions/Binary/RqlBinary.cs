namespace SoftwareOne.Rql.Abstractions.Binary
{
    public abstract class RqlBinary : RqlExpression
    {
        private readonly RqlExpression _left;
        private readonly RqlExpression _right;

        internal RqlBinary(RqlExpression left, RqlExpression right)
        {
            _left = left;
            _right = right;
        }

        public RqlExpression Left => _left;
        public RqlExpression Right => _right;
    }
}
