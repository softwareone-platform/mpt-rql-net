using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client;

internal interface IFilterGenerator
{
    string? Generate(IOperator? filterOperator);
}