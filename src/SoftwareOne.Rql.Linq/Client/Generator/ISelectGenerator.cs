using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Builder.Select;

namespace SoftwareOne.Rql.Linq.Client.Generator;

internal interface ISelectGenerator
{
    string? Generate(ISelectDefinitionProvider? select);
}