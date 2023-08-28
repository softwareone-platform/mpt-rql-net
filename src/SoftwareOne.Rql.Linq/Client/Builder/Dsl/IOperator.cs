namespace SoftwareOne.Rql.Linq.Client.Builder.Dsl;

public interface IOperator
{
    IOperator And(IOperator other);
    IOperator Or(IOperator other);
}