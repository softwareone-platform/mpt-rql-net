using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client;

internal interface ISelectGenerator
{
    string Generate(SelectFields selectFields);
}