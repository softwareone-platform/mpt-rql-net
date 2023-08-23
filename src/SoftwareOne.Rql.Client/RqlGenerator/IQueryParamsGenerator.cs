using SoftwareOne.Rql.Client.Builder.Dsl;

namespace SoftwareOne.Rql.Client.RqlGenerator;

public interface IQueryParamsGenerator
{
    string Generate(IOperator op);
}