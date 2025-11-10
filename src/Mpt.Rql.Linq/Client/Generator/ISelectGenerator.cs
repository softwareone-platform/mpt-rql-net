using Mpt.Rql.Client;
using Mpt.Rql.Linq.Client.Builder.Select;

namespace Mpt.Rql.Linq.Client.Generator;

internal interface ISelectGenerator
{
    string? Generate(ISelectDefinitionProvider? select);
}