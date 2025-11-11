using Mpt.Rql.Client;

namespace Mpt.Rql.Linq.Client.Generator;

internal interface IFilterGenerator
{
    string? Generate(IOperator? filterOperator);
}