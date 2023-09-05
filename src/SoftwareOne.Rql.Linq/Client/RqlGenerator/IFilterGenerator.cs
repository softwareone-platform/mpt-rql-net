using SoftwareOne.Rql.Linq.Client.Filter;

namespace SoftwareOne.Rql.Linq.Client;

internal interface IFilterGenerator
{
    string? Generate(IFilterDefinitionProvider? provider);
}