namespace SoftwareOne.Rql.Client.Builder.Dsl;

public abstract record Operator : IOperator
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