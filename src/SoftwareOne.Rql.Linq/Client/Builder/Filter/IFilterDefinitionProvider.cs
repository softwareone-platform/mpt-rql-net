using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Filter;

internal interface IFilterDefinitionProvider
{
    internal IOperator? GetDefinition();
}