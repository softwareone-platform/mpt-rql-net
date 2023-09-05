using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Select;

namespace SoftwareOne.Rql.Linq.Client;

internal interface ISelectGenerator
{
    string? Generate(ISelectDefinitionProvider? select);
}