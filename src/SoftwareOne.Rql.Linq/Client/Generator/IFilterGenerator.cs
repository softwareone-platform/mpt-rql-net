using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Generator;

internal interface IFilterGenerator
{
    string? Generate(IOperator? filterOperator);
}