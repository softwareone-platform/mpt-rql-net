using SoftwareOne.Rql.Client.Builder.Paging;

namespace SoftwareOne.Rql.Client.RqlGenerator;

public interface IPagingGenerator
{
    string Generate(Paging paging);
}