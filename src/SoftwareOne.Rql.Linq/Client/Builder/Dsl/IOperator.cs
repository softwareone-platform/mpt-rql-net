#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IOperator
{
    IOperator And(IOperator other);
    IOperator Or(IOperator other);
}