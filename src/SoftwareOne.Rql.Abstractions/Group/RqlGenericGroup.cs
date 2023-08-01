using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Abstractions.Group
{
    public class RqlGenericGroup : RqlGroup
    {
        internal RqlGenericGroup(string name, IEnumerable<RqlExpression> expressions) : base(expressions)
        {
            Name = name;
        }

        public string Name { get; init; }
    }
}
