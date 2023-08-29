#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IQueryParamsGenerator
{
    string Generate(IOperator op);
}