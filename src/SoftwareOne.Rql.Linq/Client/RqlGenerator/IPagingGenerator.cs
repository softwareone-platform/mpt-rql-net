using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client;

internal interface IPagingGenerator
{
    string Generate(Paging paging);
}