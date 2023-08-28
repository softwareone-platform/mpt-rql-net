using SoftwareOne.Rql.Linq.Client.Builder.Dsl;

namespace SoftwareOne.Rql.Linq.Client.RqlGenerator;

public interface IQueryParamsGenerator
{
    string Generate(IOperator op);
}