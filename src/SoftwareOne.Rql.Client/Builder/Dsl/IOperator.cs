namespace SoftwareOne.Rql.Client.Builder.Dsl;

public interface IOperator
{
    IOperator And(IOperator other);
    IOperator Or(IOperator other);
}