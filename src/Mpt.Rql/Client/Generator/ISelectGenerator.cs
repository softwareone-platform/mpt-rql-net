using Mpt.Rql.Client.Builder.Select;

namespace Mpt.Rql.Client.Generator;

internal interface ISelectGenerator
{
    string? Generate(ISelectDefinitionProvider? select);
}