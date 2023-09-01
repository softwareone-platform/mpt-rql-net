using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Dsl;

internal abstract record Operator : IOperator
{
    public IOperator And(IOperator other)
    {
        return new AndOperator(this, other);
    }

    public IOperator Or(IOperator other)
    {
        return new OrOperator(this, other);
    }
}