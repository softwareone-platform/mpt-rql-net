using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Dsl;

internal abstract record Operator : IOperator
{
    public IOperator And(IOperator right)
    {
        return new AndOperator(this, right);
    }

    public IOperator Or(IOperator right)
    {
        return new OrOperator(this, right);
    }
}